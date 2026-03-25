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
    public class HorusTripSeeder
    {
        private readonly ApplicationDbContext _context;

        public HorusTripSeeder(ApplicationDbContext context) => _context = context;

        public async Task SeedTripsAsync(string jsonFilePath)
        {
            Console.WriteLine("Loading Horus dependencies...");
            var agency = await _context.Agencies.FirstOrDefaultAsync(a => a.AgencyName == "Horus");
            if (agency == null) return;

            var defaultCalendar = await GetOrCreateDefaultCalendarAsync();

            var stationMappings = await _context.StopAgencyMappings
                .Where(m => m.AgencyId == agency.AgencyId)
                .ToDictionaryAsync(m => m.ExternalStationId, m => m.StopId);

            string jsonString = await File.ReadAllTextAsync(jsonFilePath);
            var trips = JsonSerializer.Deserialize<List<HorusTripDto>>(jsonString);
            if (trips == null) return;

            // =====================================================================
            // NEW: PRE-FLIGHT COACH CLASS CHECK (Solves the Tracking Crash)
            // =====================================================================
            Console.WriteLine("Pre-flight: Resolving Coach Classes...");

            // 1. Get unique classes from the JSON
            var uniqueClasses = trips
                .Select(t => new { Name = $"Horus - {t.BusType}", Capacity = t.BusCapacity })
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .ToList();

            // 2. Fetch existing classes from DB
            var classCache = await _context.Set<CoachClass>()
                .AsNoTracking() // <--- ADD THIS LINE
                .Where(c => c.Name.StartsWith("Horus - "))
                .ToDictionaryAsync(c => c.Name, c => c.CoachClassId);

            // 3. Add any missing classes safely BEFORE touching the Trips
            foreach (var cls in uniqueClasses)
            {
                if (!classCache.ContainsKey(cls.Name))
                {
                    var newClass = new CoachClass { Name = cls.Name, DefaultCapacity = cls.Capacity };
                    _context.Set<CoachClass>().Add(newClass);
                    await _context.SaveChangesAsync(); // 100% safe here!
                    classCache[cls.Name] = newClass.CoachClassId;
                }
            }
            // =====================================================================
            _context.ChangeTracker.Clear();

            Console.WriteLine($"Found {trips.Count} Horus trips. Importing...");
            int addedTrips = 0;

            foreach (var dto in trips)
            {
                if (await _context.Trips.AnyAsync(t => t.TripCode == dto.TripId && t.AgencyId == agency.AgencyId)) continue;

                int destinationStopId = 0;
                if (dto.ToStationId.HasValue && stationMappings.TryGetValue(dto.ToStationId.Value.ToString(), out int mappedDestId))
                {
                    destinationStopId = mappedDestId;
                }
                else if (!string.IsNullOrEmpty(dto.ToEn) && stationMappings.TryGetValue(dto.ToEn.ToLower(), out int mappedSlugId))
                {
                    destinationStopId = mappedSlugId;
                }

                if (destinationStopId == 0) continue;

                var firstStop = dto.StationsFrom.FirstOrDefault();
                if (firstStop?.StationId == null || !stationMappings.TryGetValue(firstStop.StationId.Value.ToString(), out int originStopId))
                    continue;

                // Safely grab the pre-loaded ID from our dictionary
                int coachClassId = classCache[$"Horus - {dto.BusType}"];

                var newTrip = new Trip
                {
                    AgencyId = agency.AgencyId,
                    TripCode = dto.TripId,
                    OriginStationId = originStopId,
                    DestinationStationId = destinationStopId,
                    DepartureTime = ParseTime(firstStop.DepartureTime),
                    TotalDurationMinutes = null,
                    ServiceId = defaultCalendar.ServiceId
                };

                int seq = 1;
                decimal price = decimal.Parse(dto.PriceEgp);

                foreach (var stop in dto.StationsFrom)
                {
                    if (stop.StationId.HasValue && stationMappings.TryGetValue(stop.StationId.Value.ToString(), out int stopId))
                    {
                        newTrip.TripStopTimes.Add(new TripStopTime
                        {
                            StationId = stopId,
                            StopSequence = seq++,
                            DepartureTime = ParseTime(stop.DepartureTime)
                        });

                        newTrip.TripFares.Add(new TripFare
                        {
                            OriginStationId = stopId,
                            DestinationStationId = destinationStopId,
                            CoachClassId = coachClassId, // Clean integer mapping
                            Price = price
                        });
                    }
                }


                newTrip.TripStopTimes.Add(new TripStopTime
                {
                    StationId = destinationStopId,
                    StopSequence = seq,
                    ArrivalTime = null,
                    DepartureTime = null
                });

                _context.Trips.Add(newTrip);
                addedTrips++;
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Successfully imported {addedTrips} Horus trips!");
        }

        private async Task<Calendar> GetOrCreateDefaultCalendarAsync()
        {
            var calendar = await _context.Set<Calendar>().OrderBy(c => c.ServiceId).FirstOrDefaultAsync();

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

        private TimeOnly ParseTime(string timeString)
        {
            if (DateTime.TryParseExact(timeString.Trim(), ["h:mm tt", "hh:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return TimeOnly.FromDateTime(dt);
            return new TimeOnly(0, 0);
        }
    }
}
