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

        public async Task<List<TripSearchResponseDto>> SearchTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            // 1) Start from dated active occurrences
            var query = _dbContext.TripOccurrences
                .AsNoTracking()
                .Where(o => o.IsActive && o.OccurrenceDate == request.TravelDate);

            // 2) Dynamic origin filter (station OR governorate)
            if (request.FromStationId.HasValue)
            {
                query = query.Where(o => o.Trip.OriginStationId == request.FromStationId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.FromGovernorate))
            {
                query = query.Where(o => o.Trip.OriginStation.Governorate == request.FromGovernorate);
            }

            // 3) Dynamic destination filter (station OR governorate)
            if (request.ToStationId.HasValue)
            {
                query = query.Where(o => o.Trip.DestinationStationId == request.ToStationId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.ToGovernorate))
            {
                query = query.Where(o => o.Trip.DestinationStation.Governorate == request.ToGovernorate);
            }

            // 4) Transport mode filter
            if (request.Transport == TransportMode.Bus)
            {
                query = query.Where(o => !o.Trip.Agency.AgencyName.Contains("Railways"));
            }
            else if (request.Transport == TransportMode.Train)
            {
                query = query.Where(o => o.Trip.Agency.AgencyName.Contains("Railways"));
            }

            // 5) Project to DTO and keep only occurrences with at least one class that can satisfy seat count
            var searchResults = await query
                .Select(o => new TripSearchResponseDto
                {
                    TripOccurrenceId = o.TripOccurrenceId,
                    TripId = o.TripId,
                    AgencyName = o.Trip.Agency.AgencyName,

                    DepartureTime = o.DepartureDateTime,
                    ArrivalTime = o.Trip.TotalDurationMinutes.HasValue ? o.ArrivalDateTime : null,
                    TotalDurationMinutes = o.Trip.TotalDurationMinutes,

                    OriginStationName = o.Trip.OriginStation.ArabicName,
                    OriginGovernorate = o.Trip.OriginStation.Governorate ?? "Unknown",

                    DestinationStationName = o.Trip.DestinationStation.ArabicName,
                    DestinationGovernorate = o.Trip.DestinationStation.Governorate ?? "Unknown",

                    AvailableClasses = o.ClassInventories
                     // 1. Must have enough seats
                     .Where(i => i.RemainingSeats >= request.Passengers)

                     // 2. Only keep classes that actually have a price for this exact route
                     .Where(i => o.Trip.TripFares.Any(f => f.CoachClassId == i.CoachClassId
                                                        && f.OriginStationId == o.Trip.OriginStationId
                                                        && f.DestinationStationId == o.Trip.DestinationStationId))

                     // 3. Project it to the DTO
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
                     })
                     .ToList()
                })
                .Where(dto => dto.AvailableClasses.Any())
                .OrderBy(dto => dto.DepartureTime)
                .ToListAsync(cancellationToken);

            return searchResults;
        }
    }
}
