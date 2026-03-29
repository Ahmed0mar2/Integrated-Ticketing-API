using GP.Application.DTOs.Search;
using GP.Application.Interfaces;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GP.Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _dbContext;

        public SearchService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ==========================================
        // 1. DIRECT SEARCH
        // ==========================================
        public async Task<List<TripSearchResponseDto>> SearchTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            // Resolve exact Station IDs if they passed Governorates
            var originIds = await ResolveStationIdsAsync(request.FromStationId, request.FromGovernorate, cancellationToken);
            var destIds = await ResolveStationIdsAsync(request.ToStationId, request.ToGovernorate, cancellationToken);

            if (!originIds.Any() || !destIds.Any()) return new List<TripSearchResponseDto>();

            // Let the database do all the filtering and sorting!
            return await BuildSearchQuery(originIds, destIds, request)
                .ToListAsync(cancellationToken);
        }

        // ==========================================
        // 2. INDIRECT SEARCH (DIJKSTRA 1-STOP)
        // ==========================================
        public async Task<List<IndirectTripResponseDto>> SearchIndirectTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            var originIds = await ResolveStationIdsAsync(request.FromStationId, request.FromGovernorate, cancellationToken);
            var destIds = await ResolveStationIdsAsync(request.ToStationId, request.ToGovernorate, cancellationToken);

            if (!originIds.Any() || !destIds.Any()) return new List<IndirectTripResponseDto>();

            // Get valid coordinates for the bounding box
            var bounds = await _dbContext.Stops
                .AsNoTracking()
                .Where(s => originIds.Contains(s.StopId) || destIds.Contains(s.StopId))
                .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
                .ToListAsync(cancellationToken);

            if (bounds.Count < 2) return new List<IndirectTripResponseDto>();

            // Calculate Spatial Pruning Box
            decimal minLat = bounds.Min(s => s.Latitude!.Value) - 0.5m;
            decimal maxLat = bounds.Max(s => s.Latitude!.Value) + 0.5m;
            decimal minLng = bounds.Min(s => s.Longitude!.Value) - 0.5m;
            decimal maxLng = bounds.Max(s => s.Longitude!.Value) + 0.5m;

            var validTransferStationIds = await _dbContext.Stops
                .AsNoTracking()
                .Where(s => s.Latitude >= minLat && s.Latitude <= maxLat && s.Longitude >= minLng && s.Longitude <= maxLng)
                .Where(s => !originIds.Contains(s.StopId) && !destIds.Contains(s.StopId))
                .Select(s => s.StopId)
                .ToListAsync(cancellationToken);

            if (!validTransferStationIds.Any()) return new List<IndirectTripResponseDto>();

            // Fetch Legs using the shared Query Builder 
            var requestLeg2Tomorrow = new TripSearchRequestDto { TravelDate = request.TravelDate.AddDays(1), Passengers = request.Passengers, Transport = request.Transport };

            var potentialLeg1s = await BuildSearchQuery(originIds, validTransferStationIds, request).ToListAsync(cancellationToken);
            var potentialLeg2sToday = await BuildSearchQuery(validTransferStationIds, destIds, request).ToListAsync(cancellationToken);
            var potentialLeg2sTomorrow = await BuildSearchQuery(validTransferStationIds, destIds, requestLeg2Tomorrow).ToListAsync(cancellationToken);

            var allPotentialLeg2s = potentialLeg2sToday.Concat(potentialLeg2sTomorrow).ToList();
            var indirectTrips = new List<IndirectTripResponseDto>();

            foreach (var leg1 in potentialLeg1s)
            {
                if (!leg1.ArrivalTime.HasValue) continue;

                var validConnections = allPotentialLeg2s.Where(leg2 =>
                    leg2.OriginStationId == leg1.DestinationStationId &&
                    leg2.DepartureTime >= leg1.ArrivalTime.Value.AddHours(1) &&
                    leg2.DepartureTime <= leg1.ArrivalTime.Value.AddHours(6)
                ).ToList();

                foreach (var leg2 in validConnections)
                {
                    if (!leg2.TotalDurationMinutes.HasValue && !leg2.ArrivalTime.HasValue) continue;

                    var layover = (int)(leg2.DepartureTime - leg1.ArrivalTime.Value).TotalMinutes;
                    var leg1Duration = leg1.TotalDurationMinutes ?? (int)(leg1.ArrivalTime.Value - leg1.DepartureTime).TotalMinutes;
                    var leg2Duration = leg2.TotalDurationMinutes ?? (int)(leg2.ArrivalTime!.Value - leg2.DepartureTime).TotalMinutes;

                    indirectTrips.Add(new IndirectTripResponseDto
                    {
                        TotalDurationMinutes = leg1Duration + layover + leg2Duration,
                        LayoverDurationMinutes = layover,
                        TotalStartingPrice = leg1.AvailableClasses.Min(c => c.Price) + leg2.AvailableClasses.Min(c => c.Price),
                        Legs = new List<TripSearchResponseDto> { leg1, leg2 }
                    });
                }
            }

            // Memory sort for indirect routes since they are constructed in C#
            return request.SortBy switch
            {
                SearchSortOption.LowestPrice => indirectTrips.OrderBy(t => t.TotalStartingPrice).ToList(),
                SearchSortOption.ShortestDuration => indirectTrips.OrderBy(t => t.TotalDurationMinutes).ToList(),
                _ => indirectTrips.OrderBy(t => t.TotalStartingPrice).ToList()
            };
        }

        // ==========================================
        // HELPER: THE IQUERYABLE BUILDER
        // ==========================================
        private IQueryable<TripSearchResponseDto> BuildSearchQuery(List<int> originIds, List<int> destIds, TripSearchRequestDto request)
        {
            // 1. Base Query
            var query = _dbContext.TripOccurrences
                .AsNoTracking()
                .Where(o => o.IsActive && o.OccurrenceDate == request.TravelDate)
                .Where(o => originIds.Contains(o.Trip.OriginStationId))
                .Where(o => destIds.Contains(o.Trip.DestinationStationId));

            // 2. Transport Filters
            if (request.Transport == TransportMode.Bus)
                query = query.Where(o => !o.Trip.Agency.AgencyName.Contains("Railways"));
            else if (request.Transport == TransportMode.Train)
                query = query.Where(o => o.Trip.Agency.AgencyName.Contains("Railways"));

            // 3. Agency Filters
            if (request.PreferredAgencies != null && request.PreferredAgencies.Any())
                query = query.Where(o => request.PreferredAgencies.Contains(o.Trip.Agency.AgencyName));

            var projectedQuery = query.Select(o => new TripSearchResponseDto
            {
                TripOccurrenceId = o.TripOccurrenceId,
                TripId = o.TripId,
                AgencyName = o.Trip.Agency.AgencyName,
                DepartureTime = o.DepartureDateTime,
                ArrivalTime = o.Trip.TotalDurationMinutes.HasValue ? o.ArrivalDateTime : null,
                TotalDurationMinutes = o.Trip.TotalDurationMinutes,
                OriginStationId = o.Trip.OriginStationId,
                OriginStationName = o.Trip.OriginStation.ArabicName,
                OriginGovernorate = o.Trip.OriginStation.Governorate ?? "Unknown",
                DestinationStationId = o.Trip.DestinationStationId,
                DestinationStationName = o.Trip.DestinationStation.ArabicName,
                DestinationGovernorate = o.Trip.DestinationStation.Governorate ?? "Unknown",
                AvailableClasses = o.ClassInventories
                    .Where(i => i.RemainingSeats >= request.Passengers)
                    .Where(i => o.Trip.TripFares.Any(f => f.CoachClassId == i.CoachClassId
                                                       && f.OriginStationId == o.Trip.OriginStationId
                                                       && f.DestinationStationId == o.Trip.DestinationStationId))
                    .Select(i => new TripClassOptionDto
                    {
                        CoachClassId = i.CoachClassId,
                        ClassName = i.CoachClass.Name,
                        RemainingSeats = i.RemainingSeats,
                        Price = o.Trip.TripFares
                            .Where(f => f.CoachClassId == i.CoachClassId
                                     && f.OriginStationId == o.Trip.OriginStationId
                                     && f.DestinationStationId == o.Trip.DestinationStationId)
                            .Select(f => f.Price)
                            .FirstOrDefault()
                    }).ToList()
            }).Where(dto => dto.AvailableClasses.Any());

            // 4. Price Filter 
            if (request.MaxPrice.HasValue)
                projectedQuery = projectedQuery.Where(dto => dto.AvailableClasses.Min(c => c.Price) <= request.MaxPrice.Value);

            projectedQuery = request.SortBy switch
            {
                SearchSortOption.LowestPrice => projectedQuery.OrderBy(dto => dto.AvailableClasses.Min(c => c.Price)).ThenBy(dto => dto.DepartureTime),
                SearchSortOption.ShortestDuration => projectedQuery.OrderBy(dto => dto.TotalDurationMinutes ?? 9999).ThenBy(dto => dto.DepartureTime),
                _ => projectedQuery.OrderBy(dto => dto.DepartureTime)
            };

            return projectedQuery;
        }

        // HELPER: Resolve Strings to IDs
        private async Task<List<int>> ResolveStationIdsAsync(int? stationId, string? governorate, CancellationToken ct)
        {
            if (stationId.HasValue) return new List<int> { stationId.Value };
            if (!string.IsNullOrWhiteSpace(governorate))
                return await _dbContext.Stops.AsNoTracking().Where(s => s.Governorate == governorate).Select(s => s.StopId).ToListAsync(ct);
            return new List<int>();
        }
    }
}