using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Bookings;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IBoardingPassService _boardingPassService;

        public BookingsController(
            IBookingService bookingService,
            IBoardingPassService boardingPassService)
        {
            _bookingService = bookingService;
            _boardingPassService = boardingPassService;
        }

        [HttpPost("cart")]
        [HttpPost("cart/add")]
        [ProducesResponseType(typeof(ApiResponse<BookingCartResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BookingCartResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<BookingCartResponseDto>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            try
            {
                var result = await _bookingService.AddToCartAsync(userId.Value, request, cancellationToken);

                return Ok(ApiResponse<BookingCartResponseDto>.SuccessResponse(
                    result,
                    "Trip added to cart successfully."));
            }
            catch (CartConcurrencyException ex)
            {
                return Conflict(ApiResponse<BookingCartResponseDto>.ErrorResponse(ex.Message));
            }
            catch (CartValidationException ex)
            {
                return BadRequest(ApiResponse<BookingCartResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.ErrorResponse("An unexpected error occurred while adding this trip to cart."));
            }
        }

        [HttpGet("cart")]
        [ProducesResponseType(typeof(ApiResponse<BookingCartResponseDto?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetActiveCart(CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            var cart = await _bookingService.GetActiveCartAsync(userId.Value, cancellationToken);

            if (cart == null)
            {
                return Ok(ApiResponse<BookingCartResponseDto?>.SuccessResponse(null, "No active cart found."));
            }

            return Ok(ApiResponse<BookingCartResponseDto?>.SuccessResponse(cart, "Active cart retrieved successfully."));
        }

        [HttpDelete("{bookingId}")]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelCartHold([FromRoute] int bookingId, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            try
            {
                await _bookingService.CancelCartHoldAsync(userId.Value, bookingId, cancellationToken);
                return Ok(ApiResponse<object?>.SuccessResponse(null, "Cart hold cancelled successfully."));
            }
            catch (CartValidationException ex)
            {
                return BadRequest(ApiResponse<object?>.ErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.ErrorResponse("An unexpected error occurred while cancelling cart hold."));
            }
        }

        [HttpGet("my-tickets")]
        [ProducesResponseType(typeof(ApiResponse<List<MyTicketResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyTickets(CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            var tickets = await _bookingService.GetMyTicketsAsync(userId.Value, cancellationToken);
            return Ok(ApiResponse<List<MyTicketResponseDto>>.SuccessResponse(tickets, "Tickets retrieved successfully."));
        }

        [HttpPost("checkout")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            try
            {
                var resultMessage = await _bookingService.CheckoutAsync(userId.Value, request, cancellationToken);

                return Ok(ApiResponse<string>.SuccessResponse(resultMessage, resultMessage));
            }
            catch (CartValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
            }
            catch (CartConcurrencyException ex)
            {
                return Conflict(ApiResponse<string>.ErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.ErrorResponse("An unexpected error occurred during checkout."));
            }
        }

        [HttpGet("{bookingId}/passengers/{passengerId}/qr-payload")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBoardingPassPayload(
            [FromRoute] int bookingId,
            [FromRoute] int passengerId,
            CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
            }

            try
            {
                var payload = await _boardingPassService.GenerateBoardingPassPayloadAsync(
                    userId.Value,
                    bookingId,
                    passengerId,
                    cancellationToken);

                return Ok(ApiResponse<string>.SuccessResponse(payload, "Boarding pass generated successfully."));
            }
            catch (BadHttpRequestException ex)
            {
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("verify-pass")]
        [ProducesResponseType(typeof(ApiResponse<VerifyPassResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyBoardingPass(
            [FromBody] VerifyPassRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Payload))
            {
                return BadRequest(ApiResponse.ErrorResponse("Boarding pass payload is required."));
            }

            try
            {
                var result = await _boardingPassService.VerifyBoardingPassAsync(
                    request.Payload,
                    cancellationToken);

                return Ok(ApiResponse<VerifyPassResponseDto>.SuccessResponse(
                    result,
                    "Boarding pass verified successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (BadHttpRequestException ex)
            {
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
        }
    }
}
