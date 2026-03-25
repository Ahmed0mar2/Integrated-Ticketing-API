using GP.Domain.Entities;
using GP.Infrastructure.Data;
using GP.Infrastructure.Data.SeedData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GP.Infrastructure.Services
{
    public class GoBusTripSeeder
    {
        private readonly ApplicationDbContext _context;

        public GoBusTripSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedTripsAsync(string jsonFilePath)
        {
            Console.WriteLine("Loading GoBus dependencies...");

            var agency = await _context.Agencies.FirstOrDefaultAsync(a => a.AgencyName == "GoBus");
            if (agency == null) return;
            var defaultCalendar = await GetOrCreateDefaultCalendarAsync();

            // Load all GoBus mapped station IDs into memory
            var stationMappings = await _context.StopAgencyMappings
                .Where(m => m.AgencyId == agency.AgencyId)
                .ToDictionaryAsync(m => m.ExternalStationId, m => m.StopId);

            string jsonString = await File.ReadAllTextAsync(jsonFilePath);
            var rawTrips = JsonSerializer.Deserialize<List<GoBusTripDto>>(jsonString);
            if (rawTrips == null) return;

            // 1. NORMALIZE THE CAPACITIES
            // Group by class name and find the MAX seats for each class
            var classCapacities = rawTrips
                .GroupBy(t => t.ServiceClass)
                .ToDictionary(g => g.Key, g => g.Max(t => t.TotalSeats));

            // 2. EXTRACT UNIQUE BLUEPRINTS
            // Origin + Dest + TimeOnly
            var uniqueBlueprints = rawTrips
                .GroupBy(t => new {
                    t.FromStationId,
                    t.ToStationId,
                    Time = TimeOnly.FromDateTime(t.TripDateTime),
                    t.ServiceClass
                })
                .Select(g => g.First()) 
                .ToList();

            Console.WriteLine($"Found {uniqueBlueprints.Count} unique GoBus blueprints. Importing...");

            int addedTrips = 0;

            foreach (var dto in uniqueBlueprints)
            {
                string externalOrigin = dto.FromStationId.ToString();
                string externalDest = dto.ToStationId.ToString();

                if (!stationMappings.TryGetValue(externalOrigin, out int originId) ||
                    !stationMappings.TryGetValue(externalDest, out int destId))
                {
                    continue; 
                }

                // Create a synthetic TripCode 
                TimeOnly tripTime = TimeOnly.FromDateTime(dto.TripDateTime);
                string tripCode = $"GB-{dto.FromStationId}-{dto.ToStationId}-{tripTime:HHmm}";

                if (await _context.Trips.AnyAsync(t => t.TripCode == tripCode)) continue;

                // Get normalized Coach Class
                int capacity = classCapacities[dto.ServiceClass];
                var coachClass = await GetOrCreateCoachClassAsync($"GoBus - {dto.ServiceClass}", capacity);

                var newTrip = new Trip
                {
                    AgencyId = agency.AgencyId,
                    TripCode = tripCode,
                    OriginStationId = originId,
                    DestinationStationId = destId,
                    DepartureTime = tripTime,
                    TotalDurationMinutes = dto.DurationMinutes,
                    ServiceId = defaultCalendar.ServiceId
                };

                // Point A -> Point B Timetable
                newTrip.TripStopTimes.Add(new TripStopTime { StationId = originId, StopSequence = 1, DepartureTime = tripTime });
                newTrip.TripStopTimes.Add(new TripStopTime { StationId = destId, StopSequence = 2, ArrivalTime = tripTime.AddMinutes(dto.DurationMinutes) });

                // Pricing Matrix
                newTrip.TripFares.Add(new TripFare
                {
                    OriginStationId = originId,
                    DestinationStationId = destId,
                    CoachClassId = coachClass.CoachClassId,
                    Price = dto.TripPrice
                });

                _context.Trips.Add(newTrip);
                addedTrips++;
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Successfully imported {addedTrips} GoBus trips!");
        }

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
                    StartDate = new DateOnly(DateTime.UtcNow.Year, 1, 1),      // Start of this year
                    EndDate = new DateOnly(DateTime.UtcNow.Year + 2, 12, 31)   // Valid for the next 2 years
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
    }
}
