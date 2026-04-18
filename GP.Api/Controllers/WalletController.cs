using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Wallet;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpPost("deposit")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Deposit([FromBody] DepositRequestDto request, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();

            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            try
            {
                var resultMessage = await _walletService.DepositAsync(userId.Value, request, cancellationToken);
                return Ok(ApiResponse<string>.SuccessResponse(resultMessage, resultMessage));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<List<WalletTransactionResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();

            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            var history = await _walletService.GetTransactionHistoryAsync(userId.Value, cancellationToken);
            return Ok(ApiResponse<List<WalletTransactionResponseDto>>.SuccessResponse(history, "Wallet transaction history retrieved successfully."));
        }
    }
}
