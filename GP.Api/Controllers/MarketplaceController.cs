using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Marketplace;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketplaceController : ControllerBase
    {
        private readonly IMarketplaceService _marketplaceService;

        public MarketplaceController(IMarketplaceService marketplaceService)
        {
            _marketplaceService = marketplaceService;
        }

        [HttpPost("list")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ListTicket([FromBody] ListTicketRequestDto request, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            var result = await _marketplaceService.ListTicketAsync(userId.Value, request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("buy/{listingId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> BuyTicket(int listingId, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            var result = await _marketplaceService.BuyTicketAsync(userId.Value, listingId, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("active")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MarketplaceListingResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveListings(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] MarketplaceSearchRequestDto? searchDto = null,
            CancellationToken cancellationToken = default)
        {
            searchDto ??= new MarketplaceSearchRequestDto();

            var listings = await _marketplaceService.GetActiveListingsAsync(pageNumber, pageSize, searchDto, cancellationToken);

            if (listings.TotalCount == 0)
            {
                return Ok(ApiResponse<PagedResult<MarketplaceListingResponseDto>>.SuccessResponse(
                    listings,
                    "No active marketplace listings found."));
            }

            return Ok(ApiResponse<PagedResult<MarketplaceListingResponseDto>>.SuccessResponse(
                listings,
                "Active marketplace listings retrieved successfully."));
        }
    }
}
