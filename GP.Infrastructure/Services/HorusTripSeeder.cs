using GP.Domain.Entities;
using GP.Domain.Common;
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

            var mappedStopIds = stationMappings.Values.Distinct().ToList();
            var stationCoordinates = await _context.Set<Stop>()
                .AsNoTracking()
                .Where(s => mappedStopIds.Contains(s.StopId))
                .Select(s => new { s.StopId, s.Latitude, s.Longitude })
                .ToDictionaryAsync(s => s.StopId, s => (s.Latitude, s.Longitude));

            string jsonString = await File.ReadAllTextAsync(jsonFilePath);
            var trips = JsonSerializer.Deserialize<List<HorusTripDto>>(jsonString);
            if (trips == null) return;

            Console.WriteLine("Pre-flight: Resolving Coach Classes...");

            var uniqueClasses = trips
                .SelectMany(ExtractFareClassSpecs)
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Name = g.Key,
                    Capacity = g.Max(x => x.Capacity)
                })
                .ToList();

            var existingClasses = await _context.Set<CoachClass>()
                .AsNoTracking()
                .Where(c => c.Name.StartsWith("Horus - "))
                .ToListAsync();

            var classCache = existingClasses
                .ToDictionary(c => c.Name, c => c.CoachClassId, StringComparer.OrdinalIgnoreCase);

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

                int estimatedDurationMinutes = 180;
                if (stationCoordinates.TryGetValue(originStopId, out var originCoords)
                    && stationCoordinates.TryGetValue(destinationStopId, out var destinationCoords)
                    && originCoords.Latitude.HasValue
                    && originCoords.Longitude.HasValue
                    && destinationCoords.Latitude.HasValue
                    && destinationCoords.Longitude.HasValue)
                {
                    var distanceKm = CalculateDistanceKm(
                        (double)originCoords.Latitude.Value,
                        (double)originCoords.Longitude.Value,
                        (double)destinationCoords.Latitude.Value,
                        (double)destinationCoords.Longitude.Value);

                    var computedDuration = (distanceKm * 1.2 / 75.0) * 60.0;
                    estimatedDurationMinutes = Math.Max(1, (int)Math.Round(computedDuration));
                }

                var newTrip = new Trip
                {
                    AgencyId = agency.AgencyId,
                    TripCode = dto.TripId,
                    OriginStationId = originStopId,
                    DestinationStationId = destinationStopId,
                    DepartureTime = departureTime.Value,
                    ServiceId = defaultCalendar.ServiceId,
                    TotalDurationMinutes = estimatedDurationMinutes
                };

                int seq = 1;

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

                var estimatedDestinationArrival = departureTime.Value.AddMinutes(estimatedDurationMinutes);

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
                var currentYear = AppTime.GetScheduleNow().Year;

                calendar = new Calendar
                {
                    Monday = true,
                    Tuesday = true,
                    Wednesday = true,
                    Thursday = true,
                    Friday = true,
                    Saturday = true,
                    Sunday = true,
                    StartDate = new DateOnly(currentYear, 1, 1),
                    EndDate = new DateOnly(currentYear + 2, 12, 31)
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
            var baseClassName = BuildCoachClassBaseName(dto.BusType);

            if (dto.SeatsInfo.ValueKind == JsonValueKind.Object
                && dto.SeatsInfo.TryGetProperty("type", out var seatsType)
                && string.Equals(seatsType.GetString(), "double_deck", StringComparison.OrdinalIgnoreCase))
            {
                if (dto.SeatsInfo.TryGetProperty("floor_1", out var floor1)
                    && TryGetDecimalProperty(floor1, "price_egp", out decimal floor1Price)
                    && floor1Price > 0)
                {
                    int floor1Capacity = fallbackCapacity;
                    if (TryGetIntProperty(floor1, "seats", out int floor1Seats) && floor1Seats > 0)
                    {
                        floor1Capacity = floor1Seats;
                    }

                    fareClasses.Add(new HorusFareClassSpec($"{baseClassName} - Floor 1", floor1Price, floor1Capacity));
                }

                if (dto.SeatsInfo.TryGetProperty("floor_2", out var floor2))
                {
                    decimal floor2Price = 0m;
                    int floor2Capacity = 0;

                    if (floor2.TryGetProperty("single", out var floor2Single))
                    {
                        if (TryGetDecimalProperty(floor2Single, "price_egp", out decimal singlePrice) && singlePrice > 0)
                        {
                            floor2Price = singlePrice;
                        }

                        if (TryGetIntProperty(floor2Single, "seats", out int singleSeats) && singleSeats > 0)
                        {
                            floor2Capacity += singleSeats;
                        }
                    }

                    if (floor2.TryGetProperty("double", out var floor2Double))
                    {
                        if (floor2Price <= 0 && TryGetDecimalProperty(floor2Double, "price_egp", out decimal doublePrice) && doublePrice > 0)
                        {
                            floor2Price = doublePrice;
                        }

                        if (TryGetIntProperty(floor2Double, "seats", out int doubleSeats) && doubleSeats > 0)
                        {
                            floor2Capacity += doubleSeats;
                        }
                    }

                    if (floor2Price <= 0)
                    {
                        TryGetDecimalProperty(floor2, "price_egp", out floor2Price);
                    }

                    if (floor2Capacity <= 0)
                    {
                        if (TryGetIntProperty(floor2, "seats", out int floor2Seats) && floor2Seats > 0)
                        {
                            floor2Capacity = floor2Seats;
                        }
                        else
                        {
                            floor2Capacity = fallbackCapacity;
                        }
                    }

                    if (floor2Price > 0)
                    {
                        fareClasses.Add(new HorusFareClassSpec($"{baseClassName} - Floor 2", floor2Price, floor2Capacity));
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

        private static string BuildCoachClassBaseName(string busType)
        {
            var normalizedBusType = string.IsNullOrWhiteSpace(busType)
                ? "Unknown"
                : string.Join(" ", busType.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return $"Horus - {normalizedBusType}";
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

        private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusKm = 6371.0;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(DegreesToRadians(lat1))
                    * Math.Cos(DegreesToRadians(lat2))
                    * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

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
