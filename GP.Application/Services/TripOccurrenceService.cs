using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GP.Application.Services
{
    public class TripOccurrenceService : ITripOccurrenceService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TripOccurrenceService> _logger;

        public TripOccurrenceService(IServiceProvider serviceProvider, ILogger<TripOccurrenceService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task GenerateOccurrencesAsync(int targetDaysAhead = 60, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Generator: Building Trip Occurrences for the next {Days} days.", targetDaysAhead);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var targetEndDate = today.AddDays(targetDaysAhead);

            List<Trip> trips;
            List<(int TripId, int CoachClassId, int DefaultCapacity)> tripClassesRaw;
            HashSet<long> existingSet;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                trips = await dbContext.Trips
                    .Include(t => t.Calendar)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var tripClassesAnon = await dbContext.TripFares
                    .Include(f => f.CoachClass)
                    .Select(f => new { f.TripId, f.CoachClassId, DefaultCapacity = f.CoachClass.DefaultCapacity })
                    .Distinct()
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                tripClassesRaw = tripClassesAnon.Select(x => (x.TripId, x.CoachClassId, x.DefaultCapacity)).ToList();

                var existingDates = await dbContext.TripOccurrences
                    .Where(o => o.OccurrenceDate >= today && o.OccurrenceDate <= targetEndDate)
                    .Select(o => new { o.TripId, o.OccurrenceDate })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                existingSet = new HashSet<long>(existingDates.Select(e => PackKey(e.TripId, DateToInt(e.OccurrenceDate))));
            }

            var classesByTrip = tripClassesRaw
                .GroupBy(tc => tc.TripId)
                .ToDictionary(g => g.Key, g => g.Select(x => (x.CoachClassId, x.DefaultCapacity)).ToList());

            int batchCount = 0;
            int totalAdded = 0;

            var occurrencesBuffer = new List<TripOccurrence>(capacity: 512);
            var inventoriesBuffer = new List<TripOccurrenceClassInventory>(capacity: 2048);

            foreach (var trip in trips)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (trip.Calendar == null) continue; 

                for (var date = today; date <= targetEndDate; date = date.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Skip if the Trip's calendar says it doesn't run on this day of the week
                    if (!RunsOnDayOfWeek(trip.Calendar, date.DayOfWeek)) continue;

                    // Pack composite key as long to avoid string allocations
                    int dateInt = DateToInt(date);
                    long key = PackKey(trip.TripId, dateInt);
                    if (existingSet.Contains(key)) continue;

                    // A. Create the physical Bus/Train for this Date
                    DateTime departureDateTime = date.ToDateTime(trip.DepartureTime);
                    DateTime arrivalDateTime = departureDateTime;
                    if (trip.TotalDurationMinutes.HasValue)
                    {
                        arrivalDateTime = departureDateTime.AddMinutes(trip.TotalDurationMinutes.Value);
                    }

                    var newOccurrence = new TripOccurrence
                    {
                        TripId = trip.TripId,
                        OccurrenceDate = date,
                        DepartureDateTime = departureDateTime,
                        ArrivalDateTime = arrivalDateTime,
                        IsActive = true
                    };

                    occurrencesBuffer.Add(newOccurrence);

                    // B. Create the Seat Inventories based on the Trip's pricing matrix
                    if (classesByTrip.TryGetValue(trip.TripId, out var availableClasses))
                    {
                        foreach (var coachClass in availableClasses)
                        {
                            inventoriesBuffer.Add(new TripOccurrenceClassInventory
                            {
                                TripOccurrence = newOccurrence, 
                                CoachClassId = coachClass.CoachClassId,
                                RemainingSeats = coachClass.DefaultCapacity,
                                TotalSeats = coachClass.DefaultCapacity
                            });
                        }
                    }

                    // Mark key as used to avoid duplicates in the same run
                    existingSet.Add(key);

                    batchCount++;
                    totalAdded++;

                    if (batchCount >= 500)
                    {
                        await FlushBuffersAsync(occurrencesBuffer, inventoriesBuffer, cancellationToken);
                        occurrencesBuffer.Clear();
                        inventoriesBuffer.Clear();
                        batchCount = 0;
                    }
                }
            }

            if (batchCount > 0 || occurrencesBuffer.Count > 0)
            {
                await FlushBuffersAsync(occurrencesBuffer, inventoriesBuffer, cancellationToken);
                occurrencesBuffer.Clear();
                inventoriesBuffer.Clear();
            }

            _logger.LogInformation("✅ Generator finished! Created {Count} new occurrences.", totalAdded);
        }

        private static int DateToInt(DateOnly d) => d.Year * 10000 + d.Month * 100 + d.Day;
        private static long PackKey(int tripId, int dateInt) => ((long)tripId << 32) | (uint)dateInt;

        private async Task FlushBuffersAsync(
            List<TripOccurrence> occurrences,
            List<TripOccurrenceClassInventory> inventories,
            CancellationToken cancellationToken)
        {
            if ((occurrences == null || occurrences.Count == 0) && (inventories == null || inventories.Count == 0)) return;

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // For bulk adds, disable AutoDetectChanges to improve performance
                dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

                // Use AddRange to minimize per-entity overhead
                if (occurrences?.Count > 0)
                    dbContext.TripOccurrences.AddRange(occurrences);

                if (inventories?.Count > 0)
                    dbContext.TripOccurrenceClassInventories.AddRange(inventories); 

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Handle duplicate-key / unique constraint violations gracefully
                if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
                {
                    _logger.LogWarning(ex, "Duplicate key encountered while inserting occurrences - some rows already exist. Continuing.");
                }
                else
                {
                    _logger.LogError(ex, "Error while saving occurrence batch. Rethrowing.");
                    throw;
                }
            }
            finally
            {
               
                dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            
        }

        private bool RunsOnDayOfWeek(Calendar calendar, DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => calendar.Monday,
                DayOfWeek.Tuesday => calendar.Tuesday,
                DayOfWeek.Wednesday => calendar.Wednesday,
                DayOfWeek.Thursday => calendar.Thursday,
                DayOfWeek.Friday => calendar.Friday,
                DayOfWeek.Saturday => calendar.Saturday,
                DayOfWeek.Sunday => calendar.Sunday,
                _ => false
            };
        }
    }
}
