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
        Task<List<TripSearchResponseDto>> SearchTripsAsync(TripSearchRequestDto request, CancellationToken cancellationToken = default);
    }
}
