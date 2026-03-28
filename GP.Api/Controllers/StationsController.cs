using GP.Application.Common;
using GP.Application.DTOs.Stations;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] 
    public class StationsController : ControllerBase
    {
        private readonly IStationService _stationService;

        public StationsController(IStationService stationService)
        {
            _stationService = stationService;
        }

        /// <summary>
        /// Retrieves a list of all active stations grouped by Governorate.
        /// </summary>
        /// <remarks>
        /// This endpoint provides the data for the frontend "From" and "To" dropdown menus. 
        /// It returns both the Arabic name and the raw slug (as the English name) to support bilingual UIs.
        /// </remarks>
        /// <response code="200">Returns the grouped list of stations successfully.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<GovernorateStationsDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGroupedStations(CancellationToken cancellationToken)
        {
            var stations = await _stationService.GetStationsGroupedByGovernorateAsync(cancellationToken);

            return Ok(ApiResponse<List<GovernorateStationsDto>>.SuccessResponse(
                stations,
                "Stations retrieved successfully."
            ));
        }
    }
}
