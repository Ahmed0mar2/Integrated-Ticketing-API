using GP.Domain.Entities;
using GP.Infrastructure.Data;
using GP.Infrastructure.Data.SeedData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Calendar = GP.Domain.Entities.Calendar;

namespace GP.Infrastructure.Services
{
    public class HorusTripSeeder
    {
        private readonly ApplicationDbContext _context;
        private sealed record HorusFareClassSpec(string Name, decimal Price, int Capacity);

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
                .SelectMany(ExtractFareClassSpecs)
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Name = g.Key,
                    Capacity = g.Max(x => x.Capacity)
                })
                .ToList();

            // 2. Fetch existing classes from DB
            var existingClasses = await _context.Set<CoachClass>()
                .AsNoTracking()
                .Where(c => c.Name.StartsWith("Horus - "))
                .ToListAsync();

            var classCache = existingClasses
                .ToDictionary(c => c.Name, c => c.CoachClassId, StringComparer.OrdinalIgnoreCase);

            // 3. Add any missing classes safely BEFORE touching the Trips
            foreach (var cls in uniqueClasses)
            {
                if (!classCache.ContainsKey(cls.Name))
                {
                    var newClass = new CoachClass { Name = cls.Name, DefaultCapacity = cls.Capacity };
                    _context.Set<CoachClass>().Add(newClass);
                    await _context.SaveChangesAsync();
                    classCache[cls.Name] = newClass.CoachClassId;
                }
            }
            // =====================================================================
            _context.ChangeTracker.Clear();

            Console.WriteLine($"Found {trips.Count} Horus trips. Importing...");

            var existingTripKeys = await _context.Trips
                .AsNoTracking()
                .Where(t => t.AgencyId == agency.AgencyId)
                .Select(t => new { t.TripCode, t.OriginStationId, t.DestinationStationId, t.DepartureTime })
                .ToListAsync();

            var existingTrips = new HashSet<string>(
                existingTripKeys.Select(t => $"{t.TripCode ?? string.Empty}_{t.OriginStationId}_{t.DestinationStationId}_{t.DepartureTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}"),
                StringComparer.OrdinalIgnoreCase);

            int addedTrips = 0;
            int pendingTrips = 0;

            foreach (var dto in trips)
            {
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

                var departureTime = ParseTime(firstStop.DepartureTime);
                if (!departureTime.HasValue)
                    continue;

                var tripKey = $"{dto.TripId}_{originStopId}_{destinationStopId}_{departureTime.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";
                if (existingTrips.Contains(tripKey))
                    continue;

                var classSpecs = ExtractFareClassSpecs(dto);
                if (classSpecs.Count == 0)
                    continue;

                foreach (var classSpec in classSpecs)
                {
                    if (!classCache.ContainsKey(classSpec.Name))
                    {
                        var newClass = new CoachClass
                        {
                            Name = classSpec.Name,
                            DefaultCapacity = classSpec.Capacity
                        };

                        _context.Set<CoachClass>().Add(newClass);
                        await _context.SaveChangesAsync();
                        classCache[classSpec.Name] = newClass.CoachClassId;
                    }
                }

                var newTrip = new Trip
                {
                    AgencyId = agency.AgencyId,
                    TripCode = dto.TripId,
                    OriginStationId = originStopId,
                    DestinationStationId = destinationStopId,
                    DepartureTime = departureTime.Value,
                    ServiceId = defaultCalendar.ServiceId
                };

                int seq = 1;
                TimeOnly? firstDepartureTime = null;
                TimeOnly? lastBoardingDeparture = null;

                foreach (var stop in dto.StationsFrom)
                {
                    if (stop.StationId.HasValue && stationMappings.TryGetValue(stop.StationId.Value.ToString(), out int stopId))
                    {
                        var stopDepartureTime = ParseTime(stop.DepartureTime);

                        newTrip.TripStopTimes.Add(new TripStopTime
                        {
                            StationId = stopId,
                            StopSequence = seq++,
                            DepartureTime = stopDepartureTime,
                            ArrivalTime = null
                        });

                        if (stopDepartureTime.HasValue)
                        {
                            firstDepartureTime ??= stopDepartureTime;
                            lastBoardingDeparture = stopDepartureTime;
                        }

                        foreach (var classSpec in classSpecs)
                        {
                            if (!classCache.TryGetValue(classSpec.Name, out int coachClassId))
                                continue;

                            newTrip.TripFares.Add(new TripFare
                            {
                                OriginStationId = stopId,
                                DestinationStationId = destinationStopId,
                                CoachClassId = coachClassId,
                                Price = classSpec.Price
                            });
                        }
                    }
                }

                if (newTrip.TripStopTimes.Count == 0 || newTrip.TripFares.Count == 0)
                    continue;

                var effectiveFirstDeparture = firstDepartureTime ?? departureTime;
                var effectiveLastBoarding = lastBoardingDeparture ?? effectiveFirstDeparture;

                int estimatedDurationMinutes = EstimateDurationMinutes(
                    effectiveFirstDeparture!.Value,
                    effectiveLastBoarding!.Value);

                newTrip.TotalDurationMinutes = estimatedDurationMinutes;

                var estimatedDestinationArrival = effectiveFirstDeparture.Value.AddMinutes(estimatedDurationMinutes);

                newTrip.TripStopTimes.Add(new TripStopTime
                {
                    StationId = destinationStopId,
                    StopSequence = seq,
                    ArrivalTime = estimatedDestinationArrival,
                    DepartureTime = null
                });

                _context.Trips.Add(newTrip);
                existingTrips.Add(tripKey);
                addedTrips++;
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

        private List<HorusFareClassSpec> ExtractFareClassSpecs(HorusTripDto dto)
        {
            var fareClasses = new List<HorusFareClassSpec>();
            var fallbackCapacity = dto.BusCapacity > 0 ? dto.BusCapacity : 40;
            var baseClassName = $"Horus - {dto.BusType}";

            if (dto.SeatsInfo.ValueKind == JsonValueKind.Object
                && dto.SeatsInfo.TryGetProperty("type", out var seatsType)
                && string.Equals(seatsType.GetString(), "double_deck", StringComparison.OrdinalIgnoreCase))
            {
                if (dto.SeatsInfo.TryGetProperty("floor_1", out var floor1))
                {
                    AddDeckClass(fareClasses, floor1, $"{baseClassName} - Floor 1 Single", fallbackCapacity);
                }

                if (dto.SeatsInfo.TryGetProperty("floor_2", out var floor2))
                {
                    bool hasNamedSubClasses = false;

                    if (floor2.TryGetProperty("single", out var floor2Single))
                    {
                        AddDeckClass(fareClasses, floor2Single, $"{baseClassName} - Floor 2 Single", fallbackCapacity);
                        hasNamedSubClasses = true;
                    }

                    if (floor2.TryGetProperty("double", out var floor2Double))
                    {
                        AddDeckClass(fareClasses, floor2Double, $"{baseClassName} - Floor 2 Double", fallbackCapacity);
                        hasNamedSubClasses = true;
                    }

                    if (!hasNamedSubClasses)
                    {
                        AddDeckClass(fareClasses, floor2, $"{baseClassName} - Floor 2", fallbackCapacity);
                    }
                }
            }

            if (fareClasses.Count == 0)
            {
                decimal basePrice = 0m;
                if (!decimal.TryParse(dto.PriceEgp, NumberStyles.Any, CultureInfo.InvariantCulture, out basePrice))
                {
                    TryGetDecimalProperty(dto.SeatsInfo, "price_egp", out basePrice);
                }

                if (basePrice > 0)
                {
                    fareClasses.Add(new HorusFareClassSpec(baseClassName, basePrice, fallbackCapacity));
                }
            }

            return fareClasses
                .Where(c => c.Price > 0)
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var sample = g.First();
                    return new HorusFareClassSpec(sample.Name, sample.Price, g.Max(x => x.Capacity));
                })
                .ToList();
        }

        private static void AddDeckClass(List<HorusFareClassSpec> fareClasses, JsonElement deckElement, string className, int fallbackCapacity)
        {
            if (!TryGetDecimalProperty(deckElement, "price_egp", out decimal price) || price <= 0)
                return;

            int capacity = fallbackCapacity;
            if (TryGetIntProperty(deckElement, "seats", out int deckSeats) && deckSeats > 0)
            {
                capacity = deckSeats;
            }

            fareClasses.Add(new HorusFareClassSpec(className, price, capacity));
        }

        private static bool TryGetDecimalProperty(JsonElement element, string propertyName, out decimal value)
        {
            value = 0m;
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
                return false;

            if (property.ValueKind == JsonValueKind.Number)
                return property.TryGetDecimal(out value);

            if (property.ValueKind == JsonValueKind.String)
                return decimal.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);

            return false;
        }

        private static bool TryGetIntProperty(JsonElement element, string propertyName, out int value)
        {
            value = 0;
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
                return false;

            if (property.ValueKind == JsonValueKind.Number)
                return property.TryGetInt32(out value);

            if (property.ValueKind == JsonValueKind.String)
                return int.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);

            return false;
        }

        private static int EstimateDurationMinutes(TimeOnly firstDeparture, TimeOnly lastBoardingDeparture)
        {
            int boardingWindowMinutes = CalculatePositiveMinutes(firstDeparture, lastBoardingDeparture);
            if (boardingWindowMinutes <= 0)
                return 180;

            int estimated = Math.Max(90, boardingWindowMinutes * 2);
            return Math.Min(estimated, 960);
        }

        private static int CalculatePositiveMinutes(TimeOnly start, TimeOnly end)
        {
            var diff = end.ToTimeSpan() - start.ToTimeSpan();
            if (diff < TimeSpan.Zero)
                diff = diff.Add(TimeSpan.FromDays(1));

            return (int)Math.Round(diff.TotalMinutes);
        }

        private TimeOnly? ParseTime(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            if (DateTime.TryParseExact(timeString.Trim(), ["h:mm tt", "hh:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return TimeOnly.FromDateTime(dt);

            return null;
        }
    }
}
