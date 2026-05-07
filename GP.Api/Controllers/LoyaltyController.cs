using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Loyalty;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;

        public LoyaltyController(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService;
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PointTransactionHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            var result = await _loyaltyService.GetUserPointHistoryAsync(userId.Value, pageNumber, pageSize, cancellationToken);

            return Ok(ApiResponse<PagedResult<PointTransactionHistoryDto>>.SuccessResponse(result, "Point history retrieved."));
        }

        [HttpGet("challenges")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserChallengeHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetChallenges([FromQuery] bool? isCompleted, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            var result = await _loyaltyService.GetUserChallengesAsync(userId.Value, isCompleted, pageNumber, pageSize, cancellationToken);

            return Ok(ApiResponse<PagedResult<UserChallengeHistoryDto>>.SuccessResponse(result, "Challenge history retrieved."));
        }
    }
}
