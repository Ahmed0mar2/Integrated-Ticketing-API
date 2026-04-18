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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Calendar = GP.Domain.Entities.Calendar;

namespace GP.Infrastructure.Services
{
    public class BlueBusTripSeeder
    {
        private readonly ApplicationDbContext _context;
        private sealed record NormalizedStopRef(BlueBusStationDto Stop, string NormalizedStation);

        public BlueBusTripSeeder(ApplicationDbContext context) => _context = context;

        public async Task SeedTripsAsync(string jsonFilePath)
        {
            Console.WriteLine("Loading Blue Bus dependencies...");

            var agency = await _context.Agencies.FirstOrDefaultAsync(a => a.AgencyName == "Blue Bus");
            if (agency == null) return;
            var defaultCalendar = await GetOrCreateDefaultCalendarAsync();

            // Load all Blue Bus mapped station IDs into memory
            var rawStationMappings = await _context.StopAgencyMappings
                .Where(m => m.AgencyId == agency.AgencyId)
                .ToDictionaryAsync(m => m.ExternalStationId, m => m.StopId);

            var stationMappings = rawStationMappings
                .GroupBy(m => NormalizeStationKey(m.Key), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

            string jsonString = await File.ReadAllTextAsync(jsonFilePath);
            var trips = JsonSerializer.Deserialize<List<BlueBusTripDto>>(jsonString);

            if (trips == null) return;

            var uniqueClasses = trips
                .SelectMany(dto => ExtractClassNames(dto)
                    .Select(className => new
                    {
                        Name = BuildCoachClassName(dto.BusType, className),
                        Capacity = dto.BusCapacity > 0 ? dto.BusCapacity : 40
                    }))
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Name = g.Key,
                    Capacity = g.Max(x => x.Capacity)
                })
                .ToList();

            var existingClasses = await _context.Set<CoachClass>()
                .AsNoTracking()
                .Where(c => c.Name.StartsWith("Blue Bus - "))
                .ToListAsync();

            var classCache = existingClasses
                .ToDictionary(c => c.Name, c => c.CoachClassId, StringComparer.OrdinalIgnoreCase);

            foreach (var cls in uniqueClasses)
            {
                if (!classCache.ContainsKey(cls.Name))
                {
                    var newClass = new CoachClass
                    {
                        Name = cls.Name,
                        DefaultCapacity = cls.Capacity
                    };

                    _context.Set<CoachClass>().Add(newClass);
                    await _context.SaveChangesAsync();
                    classCache[cls.Name] = newClass.CoachClassId;
                }
            }

            _context.ChangeTracker.Clear();

            Console.WriteLine($"Found {trips.Count} Blue Bus trips. Importing...");

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
                var fromStops = dto.StationsFrom
                    .Select(s => new NormalizedStopRef(s, NormalizeStationKey(s.Station)))
                    .ToList();

                var toStops = dto.StationsTo
                    .Select(s => new NormalizedStopRef(s, NormalizeStationKey(s.Station)))
                    .ToList();

                // 1. Resolve Master Origin and Destination
                var firstOriginStop = fromStops.FirstOrDefault();
                var lastDestStop = toStops.LastOrDefault();

                if (firstOriginStop == null || lastDestStop == null) continue;

                if (!stationMappings.TryGetValue(firstOriginStop.NormalizedStation, out int originId) ||
                    !stationMappings.TryGetValue(lastDestStop.NormalizedStation, out int destId))
                {
                    Console.WriteLine($"⚠️ Warning: Could not find mapping for trip {dto.TripId}. Skipping.");
                    continue;
                }

                var departureTime = ParseTime(firstOriginStop.Stop.DepartureTime);
                if (!departureTime.HasValue)
                    continue;

                var tripKey = $"{dto.TripId}_{originId}_{destId}_{departureTime.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";
                if (existingTrips.Contains(tripKey))
                    continue;

                // 2. Parse Blueprint details
                int durationMinutes = ParseDurationToMinutes(dto.Duration);
                var finalDestinationArrival = ParseTime(lastDestStop.Stop.ArrivalTime);

                if (durationMinutes <= 0 && finalDestinationArrival.HasValue)
                {
                    durationMinutes = CalculatePositiveMinutes(departureTime.Value, finalDestinationArrival.Value);
                }

                if (durationMinutes <= 0)
                {
                    durationMinutes = 180;
                }

                var classNames = ExtractClassNames(dto);
                if (classNames.Count == 0)
                {
                    classNames.Add(dto.BusType.Trim());
                }

                var tripClassMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var className in classNames)
                {
                    var fullClassName = BuildCoachClassName(dto.BusType, className);
                    if (!classCache.TryGetValue(fullClassName, out int coachClassId))
                    {
                        var newClass = new CoachClass
                        {
                            Name = fullClassName,
                            DefaultCapacity = dto.BusCapacity > 0 ? dto.BusCapacity : 40
                        };

                        _context.Set<CoachClass>().Add(newClass);
                        await _context.SaveChangesAsync();
                        coachClassId = newClass.CoachClassId;
                        classCache[fullClassName] = coachClassId;
                    }

                    tripClassMap[className] = coachClassId;
                }

                var newTrip = new Trip
                {
                    AgencyId = agency.AgencyId,
                    TripCode = dto.TripId,
                    OriginStationId = originId,
                    DestinationStationId = destId,
                    DepartureTime = departureTime.Value,
                    TotalDurationMinutes = durationMinutes,
                    ServiceId = defaultCalendar.ServiceId // 1 = "Runs Every Day" calendar
                };

                // 3. Build the Sequence (TripStopTimes)
                int sequence = 1;
                var boardingStops = new List<(int StationId, string NormalizedStation)>();
                var dropOffStops = new List<(int StationId, string NormalizedStation)>();
                TripStopTime? finalDestinationStop = null;

                // Add all Boarding Stops
                foreach (var stopRef in fromStops)
                {
                    if (stationMappings.TryGetValue(stopRef.NormalizedStation, out int stopId))
                    {
                        newTrip.TripStopTimes.Add(new TripStopTime
                        {
                            StationId = stopId,
                            StopSequence = sequence++,
                            DepartureTime = ParseTime(stopRef.Stop.DepartureTime),
                            ArrivalTime = null
                        });

                        boardingStops.Add((stopId, stopRef.NormalizedStation));
                    }
                }

                // Add all Drop-off Stops
                foreach (var stopRef in toStops)
                {
                    if (stationMappings.TryGetValue(stopRef.NormalizedStation, out int stopId))
                    {
                        var tripStopTime = new TripStopTime
                        {
                            StationId = stopId,
                            StopSequence = sequence++,
                            ArrivalTime = ParseTime(stopRef.Stop.ArrivalTime),
                            DepartureTime = null
                        };

                        newTrip.TripStopTimes.Add(tripStopTime);
                        dropOffStops.Add((stopId, stopRef.NormalizedStation));

                        if (stopId == destId)
                        {
                            finalDestinationStop = tripStopTime;
                        }
                    }
                }

                if (boardingStops.Count == 0 || dropOffStops.Count == 0)
                    continue;

                var estimatedDestinationArrival = departureTime.Value.AddMinutes(durationMinutes);
                if (finalDestinationStop == null)
                {
                    newTrip.TripStopTimes.Add(new TripStopTime
                    {
                        StationId = destId,
                        StopSequence = sequence++,
                        ArrivalTime = estimatedDestinationArrival,
                        DepartureTime = null
                    });
                }
                else if (!finalDestinationStop.ArrivalTime.HasValue)
                {
                    finalDestinationStop.ArrivalTime = estimatedDestinationArrival;
                }

                // 4. Build the Pricing Matrix (TripFare)
                // Matrix: Every boarding stop to every drop-off stop
                var normalizedDestinationPrices = BuildNormalizedDestinationPriceMap(dto.PricesByDestination);
                var normalizedGeneralPrices = BuildNormalizedPriceMap(dto.Prices);

                var uniqueBoardingStops = boardingStops
                    .GroupBy(s => s.StationId)
                    .Select(g => g.First())
                    .ToList();

                var uniqueDropOffStops = dropOffStops
                    .GroupBy(s => s.StationId)
                    .Select(g => g.First())
                    .ToList();

                foreach (var origin in uniqueBoardingStops)
                {
                    foreach (var dest in uniqueDropOffStops)
                    {
                        foreach (var className in tripClassMap.Keys)
                        {
                            var finalPrice = ResolveClassPrice(
                                className,
                                dest.NormalizedStation,
                                normalizedDestinationPrices,
                                normalizedGeneralPrices);

                            if (!finalPrice.HasValue)
                                continue;

                            newTrip.TripFares.Add(new TripFare
                            {
                                OriginStationId = origin.StationId,
                                DestinationStationId = dest.StationId,
                                CoachClassId = tripClassMap[className],
                                Price = finalPrice.Value
                            });
                        }
                    }
                }

                if (newTrip.TripFares.Count == 0)
                    continue;

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

        private static HashSet<string> ExtractClassNames(BlueBusTripDto dto)
        {
            var classNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var className in dto.Prices.Keys)
            {
                var normalizedClassName = NormalizeClassName(className);
                if (!string.IsNullOrWhiteSpace(normalizedClassName))
                {
                    classNames.Add(normalizedClassName);
                }
            }

            foreach (var destinationPricing in dto.PricesByDestination.Values)
            {
                foreach (var className in destinationPricing.Keys)
                {
                    var normalizedClassName = NormalizeClassName(className);
                    if (!string.IsNullOrWhiteSpace(normalizedClassName))
                    {
                        classNames.Add(normalizedClassName);
                    }
                }
            }

            return classNames;
        }

        private static string BuildCoachClassName(string busType, string className)
        {
            var normalizedBusType = NormalizeClassName(busType);
            var normalizedClassName = NormalizeClassName(className);

            if (string.Equals(normalizedBusType, normalizedClassName, StringComparison.OrdinalIgnoreCase))
            {
                return $"Blue Bus - {normalizedBusType}";
            }

            return $"Blue Bus - {normalizedBusType} - {normalizedClassName}";
        }

        private static string NormalizeClassName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static Dictionary<string, decimal> BuildNormalizedPriceMap(IDictionary<string, string> prices)
        {
            var normalizedPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in prices)
            {
                var className = NormalizeClassName(kvp.Key);
                if (string.IsNullOrWhiteSpace(className))
                    continue;

                if (decimal.TryParse(kvp.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice))
                {
                    normalizedPrices[className] = parsedPrice;
                }
            }

            return normalizedPrices;
        }

        private static Dictionary<string, Dictionary<string, decimal>> BuildNormalizedDestinationPriceMap(
            IDictionary<string, Dictionary<string, string>> pricesByDestination)
        {
            var normalized = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in pricesByDestination)
            {
                var destinationKey = NormalizeStationKey(kvp.Key);
                if (string.IsNullOrWhiteSpace(destinationKey))
                    continue;

                var classPrices = BuildNormalizedPriceMap(kvp.Value);
                if (classPrices.Count == 0)
                    continue;

                normalized[destinationKey] = classPrices;
            }

            return normalized;
        }

        private static decimal? ResolveClassPrice(
            string className,
            string destinationKey,
            IDictionary<string, Dictionary<string, decimal>> destinationPrices,
            IDictionary<string, decimal> generalPrices)
        {
            var normalizedClassName = NormalizeClassName(className);
            if (string.IsNullOrWhiteSpace(normalizedClassName))
                return null;

            if (destinationPrices.TryGetValue(destinationKey, out var destinationClassPrices)
                && destinationClassPrices.TryGetValue(normalizedClassName, out decimal destinationPrice))
            {
                return destinationPrice;
            }

            if (generalPrices.TryGetValue(normalizedClassName, out decimal generalPrice))
            {
                return generalPrice;
            }

            return null;
        }

        private static string NormalizeStationKey(string? station)
        {
            if (string.IsNullOrWhiteSpace(station))
                return string.Empty;

            var normalized = station
                .Trim()
                .Normalize(NormalizationForm.FormKC)
                .Replace("ΓÇô", "-")
                .Replace("â€“", "-")
                .Replace('\u2013', '-')
                .Replace('\u2014', '-')
                .Replace('\u2015', '-')
                .Replace('\u2012', '-')
                .Replace('\u2212', '-');

            normalized = Regex.Replace(normalized, @"\s*-\s*", "-");
            normalized = Regex.Replace(normalized, @"\s+", " ");

            return normalized.Trim();
        }

        private TimeOnly? ParseTime(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            if (DateTime.TryParseExact(timeString.Trim(), ["h:mm tt", "hh:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                return TimeOnly.FromDateTime(dt);
            }

            return null;
        }

        private int ParseDurationToMinutes(string? duration)
        {
            if (string.IsNullOrWhiteSpace(duration)) return 0;

            var numbersString = new string(duration.Where(char.IsDigit).ToArray());
            if (int.TryParse(numbersString, out int hours)) return hours * 60;

            return 0;
        }

        private static int CalculatePositiveMinutes(TimeOnly start, TimeOnly end)
        {
            var diff = end.ToTimeSpan() - start.ToTimeSpan();
            if (diff < TimeSpan.Zero)
            {
                diff = diff.Add(TimeSpan.FromDays(1));
            }

            return (int)Math.Round(diff.TotalMinutes);
        }
    }
}
