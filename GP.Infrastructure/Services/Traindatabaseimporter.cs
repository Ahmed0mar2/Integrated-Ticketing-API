using CsvHelper;
using CsvHelper.Configuration;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Calendar = GP.Domain.Entities.Calendar;

namespace GP.Infrastructure.Services;

public class TrainDatabaseImporter
{
    private readonly ApplicationDbContext _context;

    // Translation Dictionaries: Mapping [CSV ID] -> [Database Primary Key]
    private readonly Dictionary<int, int> _agencyMap = new();
    private readonly Dictionary<int, int> _trainTypeMap = new();
    private readonly Dictionary<int, int> _coachClassMap = new();
    private readonly Dictionary<int, int> _stopMap = new();
    private readonly Dictionary<int, int> _tripMap = new();

    public TrainDatabaseImporter(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainImportResult> ImportFromCsvAsync(
        string agenciesPath, string typesPath, string classesPath,
        string configPath, string stationsPath, string tripsPath,
        string stopTimesPath, string pricingPath)
    {
        var result = new TrainImportResult();
        try
        {
            // Clear tracker to start fresh
            _context.ChangeTracker.Clear();

            await ImportAgenciesAsync(agenciesPath);
            await ImportTrainTypesAsync(typesPath);
            await ImportCoachClassesAsync(classesPath);
            await ImportTrainTypeCoachConfigAsync(configPath);
            
            result.StopsCreated = await ImportStationsAsync(stationsPath);
            var calendar = await GetOrCreateDailyCalendarAsync();
            
            result.TripsCreated = await ImportTripsAsync(tripsPath, calendar);
            result.TripStopTimesCreated = await ImportTripStopTimesAsync(stopTimesPath);
            result.TripClassPricingsCreated = await ImportTripClassPricingAsync(pricingPath);

            result.Success = true;
            result.Message = "Train blueprints imported successfully!";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }
        return result;
    }

    private async Task ImportAgenciesAsync(string path)
    {
        var records = ReadCsv<AgencyCsvRow>(path);
        int nextId = (await _context.Agencies.MaxAsync(a => (int?)a.AgencyId) ?? 0) + 1;

        foreach (var r in records)
        {
            var existingAgency = await _context.Agencies
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AgencyName == r.name);

            if (existingAgency != null)
            {
                _agencyMap[r.id] = existingAgency.AgencyId;
                continue;
            }

            var newAgency = new Agency
            {
                AgencyId = nextId++,
                AgencyName = r.name,
                AgencyType = AgencyType.Train
            };

            _context.Agencies.Add(newAgency);
            await _context.SaveChangesAsync();

            _agencyMap[r.id] = newAgency.AgencyId;
        }
    }

    private async Task ImportTrainTypesAsync(string path)
    {
        var records = ReadCsv<TrainTypeCsvRow>(path);
        int nextId = (await _context.TrainTypes.MaxAsync(t => (int?)t.TrainTypeId) ?? 0) + 1;

        foreach (var r in records)
        {
            var existingType = await _context.TrainTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == r.name);

            if (existingType != null)
            {
                _trainTypeMap[r.id] = existingType.TrainTypeId;
                continue;
            }

            var newType = new TrainType
            {
                TrainTypeId = nextId++,
                Name = r.name
            };

            _context.TrainTypes.Add(newType);
            await _context.SaveChangesAsync();

            _trainTypeMap[r.id] = newType.TrainTypeId;
        }
    }

    private async Task ImportCoachClassesAsync(string path)
    {
        var records = ReadCsv<CoachClassCsvRow>(path);

        // Get the current max ID so we know where to start for new ones
        int nextId = (await _context.CoachClasses.MaxAsync(c => (int?)c.CoachClassId) ?? 0) + 1;

        foreach (var r in records)
        {
            var existingClass = await _context.CoachClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == r.name);

            if (existingClass != null)
            {
                _coachClassMap[r.id] = existingClass.CoachClassId;
                continue;
            }

            var newClass = new CoachClass
            {
                CoachClassId = nextId++,
                Name = r.name
            };

            _context.CoachClasses.Add(newClass);
            await _context.SaveChangesAsync();

            _coachClassMap[r.id] = newClass.CoachClassId;
        }
    }

    private async Task<int> ImportStationsAsync(string path)
    {
        var records = ReadCsv<StationCsvRow>(path);
        int created = 0;
        foreach (var r in records)
        {
            var existing = await _context.Stops
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StopName == r.name_en && s.City == r.city_en);

            if (existing == null)
            {
                existing = new Stop { StopName = r.name_en, City = r.city_en, Latitude = r.latitude, Longitude = r.longitude };
                _context.Stops.Add(existing);
                await _context.SaveChangesAsync();
                created++;
            }
            _stopMap[r.id] = existing.StopId;
        }
        return created;
    }

    private async Task<int> ImportTripsAsync(string path, Calendar calendar)
    {
        var records = ReadCsv<TrainTripCsvRow>(path);
        int created = 0;
        foreach (var r in records)
        {
            var timeStr = NormalizeArabicDigits(r.departure_time);
            if (!TimeOnly.TryParse(timeStr, out var depTime)) continue;

            var trip = new Trip
            {
                AgencyId = _agencyMap[r.agency_id],
                TrainTypeId = _trainTypeMap[r.train_type_id],
                OriginStationId = _stopMap[r.origin_station_id],
                DestinationStationId = _stopMap[r.destination_station_id],
                DepartureTime = depTime,
                TotalDurationMinutes = r.total_duration_minutes,
                ServiceId = calendar.ServiceId
            };
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();
            _tripMap[r.trip_id] = trip.TripId;
            created++;
        }
        return created;
    }

    private async Task<int> ImportTripStopTimesAsync(string path)
    {
        var records = ReadCsv<TripStopTimeCsvRow>(path);
        int created = 0;
        foreach (var r in records)
        {
            if (!_tripMap.TryGetValue(r.trip_id, out int dbTripId) || 
                !_stopMap.TryGetValue(r.station_id, out int dbStopId)) continue;

            var st = new TripStopTime
            {
                TripId = dbTripId,
                StationId = dbStopId,
                StopSequence = r.stop_sequence,
                ArrivalOffsetMinutes = r.arrival_offset_minutes,
                DepartureOffsetMinutes = r.departure_offset_minutes,
                DistanceFromOriginKm = r.distance_from_origin_km
            };
            _context.TripStopTimes.Add(st);
            created++;
            if (created % 250 == 0) await _context.SaveChangesAsync();
        }
        await _context.SaveChangesAsync();
        return created;
    }

    private async Task<int> ImportTripClassPricingAsync(string path)
    {
        var records = ReadCsv<TripClassPricingCsvRow>(path);
        int count = 0;
        foreach (var r in records)
        {
            if (!_tripMap.TryGetValue(r.trip_id, out int dbTripId) || 
                !_coachClassMap.TryGetValue(r.class_id, out int dbClassId)) continue;

            _context.TripClassPricings.Add(new TripClassPricing
            {
                TripId = dbTripId,
                CoachClassId = dbClassId,
                PricingType = PricingType.DISTANCE,
                FullPrice = r.full_price,
                FullDistanceKm = r.full_distance_km,
                MinimumPrice = r.minimum_price,
                RoundingStep = r.rounding_step
            });
            count++;
            if (count % 250 == 0) await _context.SaveChangesAsync();
        }
        await _context.SaveChangesAsync();
        return count;
    }

    private async Task<int> ImportTrainTypeCoachConfigAsync(string path)
    {
        var records = ReadCsv<TrainTypeCoachConfigCsvRow>(path);
        int count = 0;
        foreach (var r in records)
        {
            if (!_trainTypeMap.TryGetValue(r.train_type_id, out int dbTypeId) || 
                !_coachClassMap.TryGetValue(r.coach_class_id, out int dbClassId)) continue;

            var existing = await _context.Set<TrainTypeCoachConfig>()
                .AnyAsync(c => c.TrainTypeId == dbTypeId && c.CoachClassId == dbClassId);

            if (!existing)
            {
                _context.Set<TrainTypeCoachConfig>().Add(new TrainTypeCoachConfig
                {
                    TrainTypeId = dbTypeId,
                    CoachClassId = dbClassId,
                    NumberOfCoaches = r.number_of_coaches,
                    SeatsPerCoach = r.seats_per_coach
                });
                count++;
            }
        }
        await _context.SaveChangesAsync();
        return count;
    }

    private List<T> ReadCsv<T>(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { PrepareHeaderForMatch = a => a.Header.ToLower() });
        return csv.GetRecords<T>().ToList();
    }

    private string NormalizeArabicDigits(string input)
    {
        string[] arabic = { "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" };
        string[] english = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        for (int i = 0; i < 10; i++) input = input.Replace(arabic[i], english[i]);
        return input;
    }

    private async Task<Calendar> GetOrCreateDailyCalendarAsync()
    {
        var cal = await _context.Calendars.AsNoTracking().FirstOrDefaultAsync();
        if (cal == null)
        {
            cal = new Calendar { Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, Saturday = true, Sunday = true, StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2026, 12, 31) };
            _context.Calendars.Add(cal);
            await _context.SaveChangesAsync();
        }
        return cal;
    }
}

#region CSV ROW CLASSES

public class AgencyCsvRow { public int id { get; set; } public string name { get; set; } = null!; }
public class TrainTypeCsvRow { public int id { get; set; } public string name { get; set; } = null!; }
public class CoachClassCsvRow { public int id { get; set; } public string name { get; set; } = null!; }
public class TrainTypeCoachConfigCsvRow { public int train_type_id { get; set; } public int coach_class_id { get; set; } public int number_of_coaches { get; set; } public int seats_per_coach { get; set; } }
public class StationCsvRow { public int id { get; set; } public string name_en { get; set; } = null!; public string city_en { get; set; } = null!; public decimal latitude { get; set; } public decimal longitude { get; set; } }
public class TrainTripCsvRow { public int trip_id { get; set; } public int agency_id { get; set; } public int train_type_id { get; set; } public int origin_station_id { get; set; } public int destination_station_id { get; set; } public string departure_time { get; set; } = null!; public int total_duration_minutes { get; set; } }
public class TripStopTimeCsvRow { public int trip_id { get; set; } public int station_id { get; set; } public int stop_sequence { get; set; } public int arrival_offset_minutes { get; set; } public int departure_offset_minutes { get; set; } public decimal distance_from_origin_km { get; set; } }
public class TripClassPricingCsvRow { public int trip_id { get; set; } public int class_id { get; set; } public decimal full_price { get; set; } public decimal full_distance_km { get; set; } public decimal minimum_price { get; set; } public int rounding_step { get; set; } }

#endregion

public class TrainImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int StopsCreated { get; set; }
    public int TripsCreated { get; set; }
    public int TripStopTimesCreated { get; set; }
    public int TripClassPricingsCreated { get; set; }
}