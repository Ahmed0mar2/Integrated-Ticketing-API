using GP.Application.Common;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly ITripOccurrenceService _tripOccurrenceService;
        private readonly IBookingService _bookingService;
        private readonly IConfiguration _configuration;

        public JobsController(
            ITripOccurrenceService tripOccurrenceService,
            IBookingService bookingService,
            IConfiguration configuration)
        {
            _tripOccurrenceService = tripOccurrenceService;
            _bookingService = bookingService;
            _configuration = configuration;
        }

        // Job 1: Generates the 60-day window
        [HttpPost("generate-occurrences")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateOccurrences([FromQuery] string secret, CancellationToken cancellationToken)
        {
            var expectedSecret = _configuration["JobSecretKey"];
            if (string.IsNullOrWhiteSpace(expectedSecret))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Fail("Job secret key is not configured."));
            }

            if (!string.Equals(secret, expectedSecret, StringComparison.Ordinal))
            {
                return Unauthorized(ApiResponse.Fail("Invalid secret key."));
            }

            // Run your generator logic
            await _tripOccurrenceService.GenerateOccurrencesAsync(60, cancellationToken);

            return Ok(ApiResponse.Ok("Trip occurrences generated successfully."));
        }

        // Job 2: Cleans up finished trips and rewards users
        [HttpPost("process-completed-trips")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessCompletedTrips([FromQuery] string secret, CancellationToken cancellationToken)
        {
            var expectedSecret = _configuration["JobSecretKey"];
            if (string.IsNullOrWhiteSpace(expectedSecret))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Fail("Job secret key is not configured."));
            }

            if (!string.Equals(secret, expectedSecret, StringComparison.Ordinal))
            {
                return Unauthorized(ApiResponse.Fail("Invalid secret key."));
            }

            // You will add a method to your BookingService that finds all Bookings 
            // where ArrivalDateTime is in the past, marks them as 'Completed', 
            // and adds the Trip Distance to the User's TotalDistance.
            await _bookingService.ProcessCompletedTripsAsync(cancellationToken);

            return Ok(ApiResponse.Ok("Completed trips processed successfully."));
        }

        // Job 3: Releases expired cart holds and restores seats
        [HttpPost("release-expired-holds")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReleaseExpiredHolds([FromQuery] string secret, CancellationToken cancellationToken)
        {
            var expectedSecret = _configuration["JobSecretKey"];
            if (string.IsNullOrWhiteSpace(expectedSecret))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Fail("Job secret key is not configured."));
            }

            if (!string.Equals(secret, expectedSecret, StringComparison.Ordinal))
            {
                return Unauthorized(ApiResponse.Fail("Invalid secret key."));
            }

            await _bookingService.ReleaseExpiredHoldsAsync(cancellationToken);

            return Ok(ApiResponse.Ok("Expired holds released and inventory restored."));
        }
    }
}
