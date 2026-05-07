using GP.Application.Common;
using GP.Application.DTOs.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Interfaces
{
    public interface ISearchService
    {
        Task<PagedResult<TripSearchResponseDto>> SearchTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default);
        Task<PagedResult<IndirectTripResponseDto>> SearchIndirectTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default);
        Task<List<PopularRouteDto>> GetPopularRoutesAsync(CancellationToken cancellationToken = default);
    }
}
