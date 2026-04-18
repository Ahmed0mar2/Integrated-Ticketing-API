using GP.Domain.Entities;
using GP.Infrastructure.Data;
using GP.Infrastructure.Data.SeedData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Calendar = GP.Domain.Entities.Calendar;

namespace GP.Infrastructure.Services
{
    public class EnrTripSeeder
    {
        private readonly ApplicationDbContext _context;

        public EnrTripSeeder(ApplicationDbContext context) => _context = context;

        public async Task SeedTrainsAsync(string stopsFilePath, string pricesFilePath)
        {
            Console.WriteLine("Loading ENR dependencies...");
            var agency = await _context.Agencies.FirstOrDefaultAsync(a => a.AgencyName == "Egyptian National Railways");
            if (agency == null) return;
            var defaultCalendar = await GetOrCreateDefaultCalendarAsync();

            var stationMappings = await _context.StopAgencyMappings
                .Where(m => m.AgencyId == agency.AgencyId)
                .ToDictionaryAsync(m => m.ExternalStationId, m => m.StopId);

            // ==========================================
            // PHASE 1: BUILD THE TRAINS (From train_stops.json)
            // ==========================================
            Console.WriteLine("Phase 1: Building Train Blueprints...");
            string stopsJson = await File.ReadAllTextAsync(stopsFilePath);
            var schedules = JsonSerializer.Deserialize<Dictionary<string, EnrScheduleDto>>(stopsJson);

            int builtTrains = 0;
            int pendingTrips = 0;

            var existingTripKeys = await _context.Trips
                .AsNoTracking()
                .Where(t => t.AgencyId == agency.AgencyId)
                .Select(t => new { t.TripCode, t.OriginStationId, t.DestinationStationId, t.DepartureTime })
                .ToListAsync();

            var existingTrips = new HashSet<string>(
                existingTripKeys.Select(t => $"{t.TripCode ?? string.Empty}_{t.OriginStationId}_{t.DestinationStationId}_{t.DepartureTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}"),
                StringComparer.OrdinalIgnoreCase);

            if (schedules != null)
            {
                foreach (var kvp in schedules)
                {
                    var schedule = kvp.Value;

                    var firstStop = schedule.Stops.OrderBy(s => s.StopOrder).First();
                    var lastStop = schedule.Stops.OrderBy(s => s.StopOrder).Last();

                    if (!stationMappings.TryGetValue(firstStop.StationSlug, out int originId) ||
                        !stationMappings.TryGetValue(lastStop.StationSlug, out int destId))
                        continue;

                    var parsedDeparture = ParseTime(firstStop.Departure) ?? default;
                    var tripKey = $"{schedule.TrainNumber}_{originId}_{destId}_{parsedDeparture.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";
                    if (existingTrips.Contains(tripKey))
                        continue;

                    var trip = new Trip
                    {
                        AgencyId = agency.AgencyId,
                        TripCode = schedule.TrainNumber,
                        OriginStationId = originId,
                        DestinationStationId = destId,
                        DepartureTime = parsedDeparture,
                        TotalDurationMinutes = CalculateDurationMinutes(firstStop.Departure, lastStop.Arrival),
                        ServiceId = defaultCalendar.ServiceId
                    };

                    foreach (var stop in schedule.Stops)
                    {
                        if (stationMappings.TryGetValue(stop.StationSlug, out int stopId))
                        {
                            trip.TripStopTimes.Add(new TripStopTime
                            {
                                StationId = stopId,
                                StopSequence = stop.StopOrder,
                                ArrivalTime = ParseTime(stop.Arrival),
                                DepartureTime = ParseTime(stop.Departure)
                            });
                        }
                    }

                    _context.Trips.Add(trip);
                    existingTrips.Add(tripKey);
                    builtTrains++;
                    pendingTrips++;

                    if (pendingTrips >= 500)
                    {
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                        pendingTrips = 0;
                    }
                }

                if (pendingTrips > 0)
                {
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                }

                Console.WriteLine($"✅ Built {builtTrains} Train Blueprints.");
            }

            // ==========================================
            // PHASE 2: APPLY THE FARES (From trips_with_prices.json)
            // ==========================================
            Console.WriteLine("Phase 2: Building Pricing Matrix...");
            string pricesJson = await File.ReadAllTextAsync(pricesFilePath);

            using JsonDocument doc = JsonDocument.Parse(pricesJson);
            int addedFares = 0;
            int pendingFares = 0;

            foreach (JsonElement fareElement in doc.RootElement.EnumerateArray())
            {
                string trainNumber = fareElement.GetProperty("train_number").GetString()!;
                string fromSlug = fareElement.GetProperty("from_slug").GetString()!;
                string toSlug = fareElement.GetProperty("to_slug").GetString()!;
                string trainTypeAr = fareElement.GetProperty("train_type_ar").GetString()!;

                if (!stationMappings.TryGetValue(fromSlug, out int fromId) ||
                    !stationMappings.TryGetValue(toSlug, out int toId))
                    continue;

                var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripCode == trainNumber && t.AgencyId == agency.AgencyId);
                if (trip == null) continue;

                var pricesObj = fareElement.GetProperty("prices");
                foreach (JsonProperty classPrice in pricesObj.EnumerateObject())
                {
                    string className = classPrice.Name;

                    if (!decimal.TryParse(classPrice.Value.GetString(), out decimal priceValue)) continue;

                    string fullClassName = $"{trainTypeAr} - {className}";
                    int estimatedCapacity = 150;

                    var coachClass = await GetOrCreateCoachClassAsync(fullClassName, estimatedCapacity);

                    bool fareExists = await _context.TripFares.AnyAsync(f =>
                        f.TripId == trip.TripId &&
                        f.OriginStationId == fromId &&
                        f.DestinationStationId == toId &&
                        f.CoachClassId == coachClass.CoachClassId);

                    if (!fareExists)
                    {
                        _context.TripFares.Add(new TripFare
                        {
                            TripId = trip.TripId,
                            OriginStationId = fromId,
                            DestinationStationId = toId,
                            CoachClassId = coachClass.CoachClassId,
                            Price = priceValue
                        });
                        addedFares++;
                        pendingFares++;

                        if (pendingFares >= 500)
                        {
                            await _context.SaveChangesAsync();
                            _context.ChangeTracker.Clear();
                            pendingFares = 0;
                        }
                    }
                }
            }

            if (pendingFares > 0)
            {
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
            Console.WriteLine($"✅ Successfully added {addedFares} fare rules to the Train matrix!");

            // ==========================================
            // PHASE 3: (Cleanup)
            // ==========================================
            Console.WriteLine("Phase 3: Cleaning up ghost trips (trains with no valid prices)...");

            var ghostTrips = await _context.Trips
                .Include(t => t.TripStopTimes)
                .Where(t => t.AgencyId == agency.AgencyId && !t.TripFares.Any())
                .ToListAsync();

            if (ghostTrips.Any())
            {
                _context.Trips.RemoveRange(ghostTrips);
                await _context.SaveChangesAsync();
                Console.WriteLine($"🗑️ Cleaned up {ghostTrips.Count} ghost trips that had missing pricing data!");
            }
            else
            {
                Console.WriteLine("✅ No ghost trips found. All imported trains have valid prices.");
            }
        }

        // --- Helper Methods ---

        private async Task<Calendar> GetOrCreateDefaultCalendarAsync()
        {
            var calendar = await _context.Set<Calendar>().FirstOrDefaultAsync();
            if (calendar == null)
            {
                Console.WriteLine("Creating default 'Runs Every Day' Calendar...");
                calendar = new Calendar
                {
                    Monday = true,
                    Tuesday = true,
                    Wednesday = true,
                    Thursday = true,
                    Friday = true,
                    Saturday = true,
                    Sunday = true,
                    StartDate = new DateOnly(DateTime.UtcNow.Year, 1, 1),
                    EndDate = new DateOnly(DateTime.UtcNow.Year + 2, 12, 31)
                };
                _context.Set<Calendar>().Add(calendar);
                await _context.SaveChangesAsync();
            }
            return calendar;
        }

        private async Task<CoachClass> GetOrCreateCoachClassAsync(string name, int capacity)
        {
            var cc = await _context.Set<CoachClass>().FirstOrDefaultAsync(c => c.Name == name);
            if (cc == null)
            {
                cc = new CoachClass { Name = name, DefaultCapacity = capacity };
                _context.Set<CoachClass>().Add(cc);
                await _context.SaveChangesAsync();
            }
            return cc;
        }

        private TimeOnly? ParseTime(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString)) return null;
            if (DateTime.TryParseExact(timeString.Trim(), ["h:mm tt", "hh:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return TimeOnly.FromDateTime(dt);
            return null;
        }

        private int? CalculateDurationMinutes(string? departureStr, string? arrivalStr)
        {
            if (string.IsNullOrWhiteSpace(departureStr) || string.IsNullOrWhiteSpace(arrivalStr))
                return null;

            if (DateTime.TryParseExact(departureStr.Trim(), ["h:mm tt", "hh:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dep) &&
                DateTime.TryParseExact(arrivalStr.Trim(), ["h:mm tt", "hh:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime arr))
            {
                if (arr < dep) arr = arr.AddDays(1);
                return (int)(arr - dep).TotalMinutes;
            }
            return null;
        }
    }
}
