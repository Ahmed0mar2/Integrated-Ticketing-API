namespace GP.Infrastructure.Services;

using CsvHelper;
using CsvHelper.Configuration;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Calendar = Domain.Entities.Calendar;

public class GoBusDatabaseImporter
{
    private readonly ApplicationDbContext _context;

    // Caches to avoid duplicate lookups
    private readonly Dictionary<int, Stop> _stopCache = new();
    private readonly Dictionary<string, Agency> _agencyCache = new();
    private readonly Dictionary<string, CoachClass> _coachClassCache = new();
    private readonly Dictionary<string, Trip> _tripCache = new();

    public GoBusDatabaseImporter(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<GoBusImportResult> ImportFromCsvAsync(
        string stationsCsvPath,
        string agenciesCsvPath,
        string coachClassesCsvPath,
        string tripsCsvPath)
    {
        var result = new GoBusImportResult();

        try
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║    GOBUS DATABASE IMPORT - NEW SCHEMA  ║");
            Console.WriteLine("╚════════════════════════════════════════╝");

            // Step 1: Stops
            Console.WriteLine("\n📍 Step 1: Importing Stations (Stops)...");
            result.StopsCreated = await ImportStopsAsync(stationsCsvPath);

            // Step 2: Agencies
            Console.WriteLine("\n🏢 Step 2: Importing Agencies...");
            result.AgenciesCreated = await ImportAgenciesAsync(agenciesCsvPath);

            // Step 3: CoachClasses
            Console.WriteLine("\n🎫 Step 3: Importing Coach Classes...");
            result.CoachClassesCreated = await ImportCoachClassesAsync(coachClassesCsvPath);

            // Step 4: Calendar
            var calendar = await GetOrCreateDailyCalendarAsync();
            result.CalendarsCreated = 1;

            // Step 5: Trips
            var tripResult = await ImportTripsAndBlueprintsAsync(tripsCsvPath, calendar);
            result.TripsCreated = tripResult.TripsCreated;
            result.OccurrencesCreated = tripResult.OccurrencesCreated;
            result.InventoriesCreated = tripResult.InventoriesCreated;

            result.Success = true;
            result.Message = "GoBus import completed successfully!";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        return result;
    }

    private async Task<int> ImportStopsAsync(string csvPath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<GoBusStationCsvRow>().ToList();

        int created = 0;
        var stopSet = _context.Set<Stop>();

        foreach (var record in records)
        {
            try
            {
                var existing = await stopSet.FirstOrDefaultAsync(s => s.StopName == record.station_name && s.City == record.city);
                if (existing != null)
                {
                    _stopCache[record.station_id] = existing;
                    continue;
                }

                var stop = new Stop
                {
                    StopName = record.station_name,
                    City = record.city,
                    Latitude = record.latitude,
                    Longitude = record.longitude
                };
                stopSet.Add(stop);
                await _context.SaveChangesAsync();
                _stopCache[record.station_id] = stop;
                created++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Error on station {record.station_name}: {ex.Message}");
            }
        }
        return created;
    }

    private async Task<int> ImportAgenciesAsync(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { PrepareHeaderForMatch = args => args.Header.ToLower() });
        var records = csv.GetRecords<GoBusAgencyCsvRow>().ToList();

        int created = 0;
        var agencySet = _context.Set<Agency>();

        foreach (var record in records)
        {
            var existing = await agencySet.FirstOrDefaultAsync(a => a.AgencyName == record.agency_name);
            if (existing != null)
            {
                _agencyCache[record.agency_name] = existing;
                continue;
            }

            var agency = new Agency { AgencyName = record.agency_name, AgencyType = AgencyType.Bus };
            agencySet.Add(agency);
            await _context.SaveChangesAsync();
            _agencyCache[record.agency_name] = agency;
            created++;
        }
        return created;
    }

    private async Task<int> ImportCoachClassesAsync(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { PrepareHeaderForMatch = args => args.Header.ToLower() });
        var records = csv.GetRecords<GoBusCoachClassCsvRow>().ToList();

        int created = 0;
        var classSet = _context.Set<CoachClass>();

        foreach (var record in records)
        {
            var existing = await classSet.FirstOrDefaultAsync(c => c.CoachClassId == record.coach_class_id);
            if (existing != null)
            {
                _coachClassCache[record.class_name] = existing;
                continue;
            }

            var coachClass = new CoachClass { CoachClassId = record.coach_class_id, Name = record.class_name };
            classSet.Add(coachClass);
            await _context.SaveChangesAsync();
            _coachClassCache[record.class_name] = coachClass;
            created++;
        }
        return created;
    }

    private async Task<(int TripsCreated, int OccurrencesCreated, int InventoriesCreated)> ImportTripsAndBlueprintsAsync(string csvPath, Calendar calendar)
    {
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var records = csv.GetRecords<GoBusTripCsvRow>().ToList();
        int tripsCreated = 0;
        var tripSet = _context.Set<Trip>();
        var stopTimeSet = _context.Set<TripStopTime>();

        //group by stations, service class, AND Departure Time
        var blueprintGroups = records.GroupBy(r => new {
            r.from_station_id,
            r.to_station_id,
            r.service_class,
            Time = TimeOnly.FromDateTime(DateTime.Parse(r.trip_datetime))
        });

        Console.WriteLine($"\n🔍 Found {blueprintGroups.Count()} unique trip blueprints in CSV.");

        foreach (var group in blueprintGroups)
        {
            var first = group.First();
            var departureTime = group.Key.Time;

            // Validation: Ensure stations and agency exist in our caches
            if (!_stopCache.TryGetValue(first.from_station_id, out var from) ||
                !_stopCache.TryGetValue(first.to_station_id, out var to)) continue;
            if (!_agencyCache.TryGetValue("Go Bus", out var ag)) continue;

            // 2. Check if this Blueprint already exists in DB
            var existingTrip = await tripSet
                .FirstOrDefaultAsync(t =>
                    t.OriginStationId == from.StopId &&
                    t.DestinationStationId == to.StopId &&
                    t.DepartureTime == departureTime &&
                    t.ServiceClass == first.service_class &&
                    t.AgencyId == ag.AgencyId);

            if (existingTrip == null)
            {
                // 3. Create the Master Trip Blueprint
                var trip = new Trip
                {
                    AgencyId = ag.AgencyId,
                    OriginStationId = from.StopId,
                    DestinationStationId = to.StopId,
                    DepartureTime = departureTime,
                    TotalDurationMinutes = (first.duration_hours * 60) + first.duration_minutes,
                    ServiceId = calendar.ServiceId,
                    ServiceClass = first.service_class,
                    BasePrice = first.trip_price,
                    TotalSeats = first.total_seats
                };

                tripSet.Add(trip);
                await _context.SaveChangesAsync(); // Save to get the TripId

                // 4. Create the TripStopTimes

                var stopTimes = new List<TripStopTime>
            {
                new TripStopTime {
                    TripId = trip.TripId,
                    StationId = from.StopId,
                    StopSequence = 1,
                    ArrivalOffsetMinutes = 0,
                    DepartureOffsetMinutes = 0
                },
                new TripStopTime {
                    TripId = trip.TripId,
                    StationId = to.StopId,
                    StopSequence = 2,
                    ArrivalOffsetMinutes = trip.TotalDurationMinutes,
                    DepartureOffsetMinutes = trip.TotalDurationMinutes
                }
            };

                stopTimeSet.AddRange(stopTimes);
                await _context.SaveChangesAsync();

                tripsCreated++;
                if (tripsCreated % 100 == 0) Console.WriteLine($"   ✅ Created {tripsCreated} blueprints...");
            }
        }

        //return 0 for Occurrences and Inventories because they'll be 
        // handled by the "Sliding Window" service later
        return (tripsCreated, 0, 0);
    }
    private async Task<Calendar> GetOrCreateDailyCalendarAsync()
    {
        var calendar = await _context.Set<Calendar>().FirstOrDefaultAsync();
        if (calendar == null)
        {
            calendar = new Calendar
            {
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
                Saturday = true,
                Sunday = true,
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2026, 12, 31)
            };
            _context.Set<Calendar>().Add(calendar);
            await _context.SaveChangesAsync();
        }
        return calendar;
    }


    private int CalculateTotalDurationMinutes(int hours, int minutes)
    {
        return (hours * 60) + minutes;
    }

    
}

#region CSV ROW CLASSES

public class GoBusStationCsvRow
{
    public string city { get; set; } = null!;
    public string station_name { get; set; } = null!;
    public int station_id { get; set; }
    public decimal latitude { get; set; }
    public decimal longitude { get; set; }
}

public class GoBusAgencyCsvRow
{
    public string agency_name { get; set; } = null!;
}

public class GoBusCoachClassCsvRow
{
    public int coach_class_id { get; set; }
    public string class_name { get; set; } = null!;
}

public class GoBusTripCsvRow
{
    public string from_city { get; set; } = null!;
    public string from_station_name { get; set; } = null!;
    public int from_station_id { get; set; }
    public decimal from_latitude { get; set; }
    public decimal from_longitude { get; set; }
    public string to_city { get; set; } = null!;
    public string to_station_name { get; set; } = null!;
    public int to_station_id { get; set; }
    public decimal to_latitude { get; set; }
    public decimal to_longitude { get; set; }
    public string trip_datetime { get; set; } = null!;
    public decimal trip_price { get; set; }
    public int total_seats { get; set; }
    public string service_class { get; set; } = null!;
    public int duration_hours { get; set; }
    public int duration_minutes { get; set; }
    public string office_from { get; set; } = null!;
    public string office_to { get; set; } = null!;
}

#endregion

#region RESULT CLASS

public class GoBusImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int StopsCreated { get; set; }
    public int AgenciesCreated { get; set; }
    public int CoachClassesCreated { get; set; }
    public int CalendarsCreated { get; set; }
    public int TripsCreated { get; set; }
    public int OccurrencesCreated { get; set; }
    public int InventoriesCreated { get; set; }
}

#endregion