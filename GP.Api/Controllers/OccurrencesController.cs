using GP.Application.Common;
using GP.Application.DTOs.Occurrences;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers;

[Route("api/occurrences")]
[ApiController]
[AllowAnonymous]
public class OccurrencesController : ControllerBase
{
    private readonly IOccurrenceSeatService _occurrenceSeatService;

    public OccurrencesController(IOccurrenceSeatService occurrenceSeatService)
    {
        _occurrenceSeatService = occurrenceSeatService;
    }

    [HttpGet("{id:int}/seats")]
    [ProducesResponseType(typeof(ApiResponse<OccurrenceSeatsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OccurrenceSeatsResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOccurrenceSeats([FromRoute] int id, CancellationToken cancellationToken)
    {
        var seatMap = await _occurrenceSeatService.GetOccurrenceSeatsAsync(id, cancellationToken);
        if (seatMap == null)
        {
            return NotFound(ApiResponse<OccurrenceSeatsResponseDto>.ErrorResponse("Trip occurrence not found."));
        }

        return Ok(ApiResponse<OccurrenceSeatsResponseDto>.SuccessResponse(seatMap, "Seat map retrieved successfully."));
    }
}
