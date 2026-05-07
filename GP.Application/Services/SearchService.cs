using GP.Application.Common;
using GP.Application.DTOs.Search;
using GP.Application.Interfaces;
using GP.Domain.Entities;
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

        public async Task<PagedResult<TripSearchResponseDto>> SearchTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            await LogRouteSearchAsync(request, cancellationToken);

            var originIds = await ResolveStationIdsAsync(request.FromStationId, request.FromGovernorate, cancellationToken);
            var destIds = await ResolveStationIdsAsync(request.ToStationId, request.ToGovernorate, cancellationToken);

            if (!originIds.Any() || !destIds.Any())
            {
                var pageNumber = Math.Max(1, request.PageNumber);
                var pageSize = Math.Max(1, request.PageSize);

                return new PagedResult<TripSearchResponseDto>
                {
                    Items = [],
                    TotalCount = 0,
                    CurrentPage = pageNumber,
                    PageSize = pageSize
                };
            }

            return await SearchDirectCoreAsync(originIds, destIds, request, cancellationToken);
        }

        public async Task<PagedResult<IndirectTripResponseDto>> SearchIndirectTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            await LogRouteSearchAsync(request, cancellationToken);

            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Max(1, request.PageSize);

            var originIds = await ResolveStationIdsAsync(request.FromStationId, request.FromGovernorate, cancellationToken);
            var destIds = await ResolveStationIdsAsync(request.ToStationId, request.ToGovernorate, cancellationToken);

            if (!originIds.Any() || !destIds.Any())
            {
                return new PagedResult<IndirectTripResponseDto>
                {
                    Items = [],
                    TotalCount = 0,
                    CurrentPage = pageNumber,
                    PageSize = pageSize
                };
            }

            // Product rule: indirect routes are only returned when no direct routes exist.
            var directMatches = await SearchDirectCoreAsync(originIds, destIds, request, cancellationToken, applyPagination: false);
            if (directMatches.TotalCount > 0)
            {
                return new PagedResult<IndirectTripResponseDto>
                {
                    Items = [],
                    TotalCount = 0,
                    CurrentPage = pageNumber,
                    PageSize = pageSize
                };
            }

            var bounds = await _dbContext.Stops
                .AsNoTracking()
                .Where(s => originIds.Contains(s.StopId) || destIds.Contains(s.StopId))
                .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
                .ToListAsync(cancellationToken);

            if (bounds.Count < 2)
            {
                return new PagedResult<IndirectTripResponseDto>
                {
                    Items = [],
                    TotalCount = 0,
                    CurrentPage = pageNumber,
                    PageSize = pageSize
                };
            }

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

            if (!validTransferStationIds.Any())
            {
                return new PagedResult<IndirectTripResponseDto>
                {
                    Items = [],
                    TotalCount = 0,
                    CurrentPage = pageNumber,
                    PageSize = pageSize
                };
            }

            var requestLeg2Tomorrow = new TripSearchRequestDto
            {
                TravelDate = request.TravelDate.AddDays(1),
                Passengers = request.Passengers,
                Transport = request.Transport,
                SortBy = request.SortBy,
                MaxPrice = request.MaxPrice,
                PreferredAgencies = request.PreferredAgencies
            };

            var potentialLeg1s = await SearchDirectCoreAsync(originIds, validTransferStationIds, request, cancellationToken, applyPagination: false);
            var potentialLeg2sToday = await SearchDirectCoreAsync(validTransferStationIds, destIds, request, cancellationToken, applyPagination: false);
            var potentialLeg2sTomorrow = await SearchDirectCoreAsync(validTransferStationIds, destIds, requestLeg2Tomorrow, cancellationToken, applyPagination: false);

            var allPotentialLeg2s = potentialLeg2sToday.Items.Concat(potentialLeg2sTomorrow.Items).ToList();
            var validIndirectRoutes = new List<IndirectTripResponseDto>();

            foreach (var leg1 in potentialLeg1s.Items)
            {
                if (leg1.DropoffTime == default)
                    continue;

                var validConnections = allPotentialLeg2s.Where(leg2 =>
                    leg2.OriginStationId == leg1.DestinationStationId &&
                    leg2.BoardingTime >= leg1.DropoffTime.AddHours(1) &&
                    leg2.BoardingTime <= leg1.DropoffTime.AddHours(6)
                ).ToList();

                foreach (var leg2 in validConnections)
                {
                    if (leg2.DropoffTime == default)
                        continue;

                    var layover = (int)(leg2.BoardingTime - leg1.DropoffTime).TotalMinutes;
                    var leg1Duration = leg1.TotalDurationMinutes ?? (int)(leg1.DropoffTime - leg1.BoardingTime).TotalMinutes;
                    var leg2Duration = leg2.TotalDurationMinutes ?? (int)(leg2.DropoffTime - leg2.BoardingTime).TotalMinutes;

                    validIndirectRoutes.Add(new IndirectTripResponseDto
                    {
                        TotalDurationMinutes = leg1Duration + layover + leg2Duration,
                        LayoverDurationMinutes = layover,
                        TotalStartingPrice = leg1.StartingPrice + leg2.StartingPrice,
                        Legs = [leg1, leg2]
                    });
                }
            }

            var sortedIndirectRoutes = request.SortBy switch
            {
                SearchSortOption.LowestPrice => validIndirectRoutes.OrderBy(t => t.TotalStartingPrice).ToList(),
                SearchSortOption.ShortestDuration => validIndirectRoutes.OrderBy(t => t.TotalDurationMinutes).ToList(),
                _ => validIndirectRoutes.OrderBy(t => t.TotalStartingPrice).ToList()
            };

            var totalCount = sortedIndirectRoutes.Count;
            var pagedItems = sortedIndirectRoutes
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<IndirectTripResponseDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<PopularRouteDto>> GetPopularRoutesAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            return await _dbContext.RouteSearchLogs
                .AsNoTracking()
                .Where(log => log.SearchedAt >= cutoff)
                .GroupBy(log => new { log.OriginGov, log.DestinationGov })
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.OriginGov)
                .ThenBy(group => group.Key.DestinationGov)
                .Take(3)
                .Select(group => new PopularRouteDto
                {
                    OriginGov = group.Key.OriginGov,
                    DestinationGov = group.Key.DestinationGov
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<PagedResult<TripSearchResponseDto>> SearchDirectCoreAsync(
            List<int> originIds,
            List<int> destIds,
            TripSearchRequestDto request,
            CancellationToken cancellationToken,
            bool applyPagination = true)
        {
            var now = AppTime.GetScheduleNow();

            var query = _dbContext.TripOccurrences
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.Trip)
                    .ThenInclude(t => t.Agency)
                .Include(o => o.Trip)
                    .ThenInclude(t => t.TripStopTimes)
                        .ThenInclude(ts => ts.Station)
                .Include(o => o.Trip)
                    .ThenInclude(t => t.TripFares)
                .Include(o => o.ClassInventories)
                    .ThenInclude(i => i.CoachClass)
                .Where(o => o.IsActive && o.OccurrenceDate == request.TravelDate)
                .Where(o => o.Trip.TripFares.Any(f => originIds.Contains(f.OriginStationId) && destIds.Contains(f.DestinationStationId)));

            if (request.Transport == TransportMode.Bus)
                query = query.Where(o => !o.Trip.Agency.AgencyName.Contains("Railways"));
            else if (request.Transport == TransportMode.Train)
                query = query.Where(o => o.Trip.Agency.AgencyName.Contains("Railways"));

            if (request.PreferredAgencies is { Count: > 0 })
                query = query.Where(o => request.PreferredAgencies.Contains(o.Trip.Agency.AgencyName));

            var occurrences = await query.ToListAsync(cancellationToken);
            var results = new List<TripSearchResponseDto>();

            foreach (var occurrence in occurrences)
            {
                var trip = occurrence.Trip;
                var stopsByStation = trip.TripStopTimes
                    .GroupBy(ts => ts.StationId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StopSequence).First());

                var matchingSegments = trip.TripFares
                    .Where(f => originIds.Contains(f.OriginStationId) && destIds.Contains(f.DestinationStationId))
                    .GroupBy(f => new { f.OriginStationId, f.DestinationStationId })
                    .Select(g => g.Key)
                    .ToList();

                foreach (var segment in matchingSegments)
                {
                    if (!stopsByStation.TryGetValue(segment.OriginStationId, out var fromStop) ||
                        !stopsByStation.TryGetValue(segment.DestinationStationId, out var toStop))
                        continue;

                    if (fromStop.StopSequence >= toStop.StopSequence)
                        continue;

                    var boardingTimeOnly = fromStop.DepartureTime ?? fromStop.ArrivalTime;
                    var dropoffTimeOnly = toStop.ArrivalTime ?? toStop.DepartureTime;
                    if (!boardingTimeOnly.HasValue || !dropoffTimeOnly.HasValue)
                        continue;

                    var boardingTime = BuildSegmentDateTime(occurrence.DepartureDateTime, trip.DepartureTime, boardingTimeOnly.Value);
                    var dropoffTime = BuildSegmentDateTime(occurrence.DepartureDateTime, trip.DepartureTime, dropoffTimeOnly.Value);

                    if (boardingTime < now)
                        continue;

                    var classOptions = occurrence.ClassInventories
                        .Where(i => i.RemainingSeats >= request.Passengers)
                        .Select(i =>
                        {
                            var segmentFare = trip.TripFares
                                .Where(f => f.OriginStationId == segment.OriginStationId
                                         && f.DestinationStationId == segment.DestinationStationId
                                         && f.CoachClassId == i.CoachClassId)
                                .Select(f => (decimal?)f.Price)
                                .FirstOrDefault();

                            if (!segmentFare.HasValue)
                                return null;

                            return new TripClassOptionDto
                            {
                                CoachClassId = i.CoachClassId,
                                ClassName = i.CoachClass.Name,
                                RemainingSeats = i.RemainingSeats,
                                Price = segmentFare.Value
                            };
                        })
                        .Where(x => x != null)
                        .Select(x => x!)
                        .ToList();

                    if (!classOptions.Any())
                        continue;

                    var startingPrice = classOptions.Min(c => c.Price);
                    if (request.MaxPrice.HasValue && startingPrice > request.MaxPrice.Value)
                        continue;

                    var routeStops = trip.TripStopTimes
                        .Where(ts => ts.StopSequence >= fromStop.StopSequence && ts.StopSequence <= toStop.StopSequence)
                        .OrderBy(ts => ts.StopSequence)
                        .Select(ts => new IntermediateStopDto
                        {
                            StationName = ts.Station.ArabicName,
                            ArrivalTime = ts.ArrivalTime,
                            DepartureTime = ts.DepartureTime,
                            StopSequence = ts.StopSequence
                        })
                        .ToList();

                    var duration = (int)Math.Max(0, (dropoffTime - boardingTime).TotalMinutes);

                    results.Add(new TripSearchResponseDto
                    {
                        TripOccurrenceId = occurrence.TripOccurrenceId,
                        TripId = occurrence.TripId,
                        AgencyName = trip.Agency.AgencyName,
                        BoardingTime = AppTime.AsSchedule(boardingTime),
                        DropoffTime = AppTime.AsSchedule(dropoffTime),
                        DepartureTime = AppTime.AsSchedule(occurrence.DepartureDateTime),
                        ArrivalTime = AppTime.AsSchedule(occurrence.ArrivalDateTime),
                        TotalDurationMinutes = duration,
                        OriginStationId = fromStop.StationId,
                        OriginStationName = fromStop.Station.ArabicName,
                        OriginGovernorate = fromStop.Station.Governorate ?? "Unknown",
                        DestinationStationId = toStop.StationId,
                        DestinationStationName = toStop.Station.ArabicName,
                        DestinationGovernorate = toStop.Station.Governorate ?? "Unknown",
                        StartingPrice = startingPrice,
                        RouteStops = routeStops,
                        AvailableClasses = classOptions
                    });
                }
            }

            var sortedResults = request.SortBy switch
            {
                SearchSortOption.LowestPrice => results.OrderBy(dto => dto.StartingPrice).ThenBy(dto => dto.BoardingTime).ToList(),
                SearchSortOption.ShortestDuration => results.OrderBy(dto => dto.TotalDurationMinutes ?? int.MaxValue).ThenBy(dto => dto.BoardingTime).ToList(),
                _ => results.OrderBy(dto => dto.BoardingTime).ToList()
            };

            if (!applyPagination)
            {
                return new PagedResult<TripSearchResponseDto>
                {
                    Items = sortedResults,
                    TotalCount = sortedResults.Count,
                    CurrentPage = 1,
                    PageSize = sortedResults.Count == 0 ? 1 : sortedResults.Count
                };
            }

            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Max(1, request.PageSize);

            int totalItems = sortedResults.Count;
            var paginatedItems = sortedResults
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<TripSearchResponseDto>
            {
                Items = paginatedItems,
                TotalCount = totalItems,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        private static DateTime BuildSegmentDateTime(DateTime occurrenceStart, TimeOnly tripOriginDeparture, TimeOnly segmentTime)
        {
            var offset = segmentTime.ToTimeSpan() - tripOriginDeparture.ToTimeSpan();
            if (offset < TimeSpan.Zero)
                offset = offset.Add(TimeSpan.FromDays(1));

            return occurrenceStart.Add(offset);
        }

        private async Task LogRouteSearchAsync(TripSearchRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FromGovernorate) || string.IsNullOrWhiteSpace(request.ToGovernorate))
                return;

            try
            {
                _dbContext.RouteSearchLogs.Add(new RouteSearchLog
                {
                    OriginGov = request.FromGovernorate.Trim(),
                    DestinationGov = request.ToGovernorate.Trim(),
                    SearchedAt = AppTime.GetScheduleNow()
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Analytics must never block or fail the user's search.
            }
        }

        private async Task<List<int>> ResolveStationIdsAsync(int? stationId, string? governorate, CancellationToken ct)
        {
            if (stationId.HasValue)
                return [stationId.Value];

            if (!string.IsNullOrWhiteSpace(governorate))
            {
                return await _dbContext.Stops
                    .AsNoTracking()
                    .Where(s => s.Governorate == governorate)
                    .Select(s => s.StopId)
                    .ToListAsync(ct);
            }

            return [];
        }
    }
}