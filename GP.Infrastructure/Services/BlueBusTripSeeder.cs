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
    public class BlueBusTripSeeder
    {
        private readonly ApplicationDbContext _context;

        public BlueBusTripSeeder(ApplicationDbContext context) => _context = context;

        public async Task SeedTripsAsync(string jsonFilePath)
        {
            Console.WriteLine("Loading Blue Bus dependencies...");

            var agency = await _context.Agencies.FirstOrDefaultAsync(a => a.AgencyName == "Blue Bus");
            if (agency == null) return;
            var defaultCalendar = await GetOrCreateDefaultCalendarAsync();

            // Load all Blue Bus mapped station IDs into memory
            var stationMappings = await _context.StopAgencyMappings
                .Where(m => m.AgencyId == agency.AgencyId)
                .ToDictionaryAsync(m => m.ExternalStationId, m => m.StopId);

            string jsonString = await File.ReadAllTextAsync(jsonFilePath);
            var trips = JsonSerializer.Deserialize<List<BlueBusTripDto>>(jsonString);

            if (trips == null) return;
            Console.WriteLine($"Found {trips.Count} Blue Bus trips. Importing...");

            int addedTrips = 0;

            foreach (var dto in trips)
            {
                // Prevent duplicate imports
                if (await _context.Trips.AnyAsync(t => t.TripCode == dto.TripId && t.AgencyId == agency.AgencyId))
                    continue;

                // 1. Resolve Master Origin and Destination
                var firstOriginStop = dto.StationsFrom.FirstOrDefault();
                var lastDestStop = dto.StationsTo.LastOrDefault();

                if (firstOriginStop == null || lastDestStop == null) continue;

                if (!stationMappings.TryGetValue(firstOriginStop.Station, out int originId) ||
                    !stationMappings.TryGetValue(lastDestStop.Station, out int destId))
                {
                    Console.WriteLine($"⚠️ Warning: Could not find mapping for trip {dto.TripId}. Skipping.");
                    continue;
                }

                // 2. Parse Blueprint details
                int durationMinutes = ParseDurationToMinutes(dto.Duration);
                var coachClass = await GetOrCreateCoachClassAsync($"Blue Bus - {dto.BusType}", dto.BusCapacity);

                var newTrip = new Trip
                {
                    AgencyId = agency.AgencyId,
                    TripCode = dto.TripId,
                    OriginStationId = originId,
                    DestinationStationId = destId,
                    DepartureTime = ParseTime(firstOriginStop.DepartureTime!),
                    TotalDurationMinutes = durationMinutes > 0 ? durationMinutes : null,
                    ServiceId = defaultCalendar.ServiceId // 1 = "Runs Every Day" calendar
                };

                // 3. Build the Sequence (TripStopTimes)
                int sequence = 1;

                // Add all Boarding Stops
                foreach (var stop in dto.StationsFrom)
                {
                    if (stationMappings.TryGetValue(stop.Station, out int stopId))
                    {
                        newTrip.TripStopTimes.Add(new TripStopTime
                        {
                            StationId = stopId,
                            StopSequence = sequence++,
                            DepartureTime = ParseTime(stop.DepartureTime),
                            ArrivalTime = null 
                        });
                    }
                }

                // Add all Drop-off Stops
                foreach (var stop in dto.StationsTo)
                {
                    if (stationMappings.TryGetValue(stop.Station, out int stopId))
                    {
                        newTrip.TripStopTimes.Add(new TripStopTime
                        {
                            StationId = stopId,
                            StopSequence = sequence++,
                            ArrivalTime = ParseTime(stop.ArrivalTime),
                            DepartureTime = null 
                        });
                    }
                }

                // 4. Build the Pricing Matrix (TripFare)
                // Matrix: Every boarding stop to every drop-off stop
                foreach (var origin in dto.StationsFrom)
                {
                    if (!stationMappings.TryGetValue(origin.Station, out int oId)) continue;

                    foreach (var dest in dto.StationsTo)
                    {
                        if (!stationMappings.TryGetValue(dest.Station, out int dId)) continue;

                        decimal? finalPrice = null;

                        // Check the granular 'prices_by_destination' first
                        if (dto.PricesByDestination.TryGetValue(dest.Station, out var specificPrices) &&
                            specificPrices.TryGetValue(dto.BusType, out var specificPriceStr))
                        {
                            if (decimal.TryParse(specificPriceStr, out decimal sp)) finalPrice = sp;
                        }
                        // Fallback to the general trip price if specific destination isn't listed
                        else if (dto.Prices.TryGetValue(dto.BusType, out var generalPriceStr))
                        {
                            if (decimal.TryParse(generalPriceStr, out decimal gp)) finalPrice = gp;
                        }

                        if (finalPrice.HasValue)
                        {
                            newTrip.TripFares.Add(new TripFare
                            {
                                OriginStationId = oId,
                                DestinationStationId = dId,
                                CoachClassId = coachClass.CoachClassId,
                                Price = finalPrice.Value
                            });
                        }
                    }
                }

                _context.Trips.Add(newTrip);
                addedTrips++;
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Successfully imported {addedTrips} Blue Bus trips!");
        }

        // --- Helper Methods ---

        private async Task<Calendar> GetOrCreateDefaultCalendarAsync()
        {
            // Check if ANY calendar exists yet
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

        private TimeOnly ParseTime(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString)) return new TimeOnly(0, 0);

            // Handles formats
            if (DateTime.TryParseExact(timeString.Trim(), ["h:mm tt", "hh:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                return TimeOnly.FromDateTime(dt);
            }
            return new TimeOnly(0, 0);
        }

        private int ParseDurationToMinutes(string? duration)
        {
            if (string.IsNullOrWhiteSpace(duration)) return 0;

            var numbersString = new string(duration.Where(char.IsDigit).ToArray());
            if (int.TryParse(numbersString, out int hours)) return hours * 60;

            return 0;
        }
    }
}
