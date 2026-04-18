using GP.Application.Common;
using GP.Application.DTOs.Search;
using GP.Application.Interfaces;
using GP.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // Anyone can search for trips!
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Searches for available transit trips based on date, route, and passenger count.
        /// </summary>
        /// <remarks>
        /// Supports highly flexible routing: Users can search by specific Station IDs OR broad Governorates. 
        /// The response automatically filters out buses that do not have enough remaining seats for the requested passenger count.
        /// </remarks>
        [HttpGet]
        [HttpGet("/api/trips/search")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TripSearchResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchTrips([FromQuery] TripSearchRequestDto request, CancellationToken cancellationToken)
        {
            var results = await _searchService.SearchTripsAsync(request, cancellationToken);

            if (results.TotalCount == 0)
            {
                return Ok(ApiResponse<PagedResult<TripSearchResponseDto>>.SuccessResponse(
                    results,
                    "No trips found matching your criteria. Try a different date or route."));
            }

            return Ok(ApiResponse<PagedResult<TripSearchResponseDto>>.SuccessResponse(
                results,
                $"Successfully found {results.TotalCount} available trips."
            ));
        }

        [HttpGet("indirect")]
        [HttpGet("/api/trips/search/indirect")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<IndirectTripResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchIndirectTrips([FromQuery] TripSearchRequestDto request, CancellationToken cancellationToken)
        {
            var results = await _searchService.SearchIndirectTripsAsync(request, cancellationToken);

            if (results.TotalCount == 0)
                return Ok(ApiResponse<PagedResult<IndirectTripResponseDto>>.SuccessResponse(results, "No indirect routes found."));

            return Ok(ApiResponse<PagedResult<IndirectTripResponseDto>>.SuccessResponse(results, $"Found {results.TotalCount} indirect routes."));
        }
    }
}