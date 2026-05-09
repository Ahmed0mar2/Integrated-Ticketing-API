using GP.Application.Common;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILoyaltyService _loyaltyService;
        private readonly ApplicationDbContext _dbContext;


        public JobsController(
            ITripOccurrenceService tripOccurrenceService,
            IBookingService bookingService,
            IConfiguration configuration,
            ILoyaltyService loyaltyService,
            ApplicationDbContext dbContext)
        {
            _tripOccurrenceService = tripOccurrenceService;
            _bookingService = bookingService;
            _configuration = configuration;
            _loyaltyService = loyaltyService;
            _dbContext = dbContext;
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

        // Job 4: Sends boarding alerts before trip departure
        [HttpPost("process-boarding-alerts")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessBoardingAlerts([FromQuery] string secret, CancellationToken cancellationToken)
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

            await _bookingService.ProcessUpcomingBoardingAlertsAsync(cancellationToken);

            return Ok("Boarding alerts processed successfully.");
        }

        // Job 5: Expires old loyalty points
        [HttpPost("expire-points")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExpirePoints([FromQuery] string secret, CancellationToken cancellationToken)
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

            var result = await _loyaltyService.ExpireOldPointsAsync(cancellationToken);

            return Ok(result);
        }

        // Job 6: Reset monthly challenges
        [HttpPost("reset-monthly-challenges")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetMonthlyChallenges([FromQuery] string secret, CancellationToken cancellationToken)
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

            var result = await _loyaltyService.ResetMonthlyChallengesAsync(cancellationToken);

            return Ok(result);
        }

        // Job 7: Seed monthly challenges
        [HttpPost("seed-challenges")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SeedChallenges([FromQuery] string secret, CancellationToken cancellationToken)
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

            if (await _dbContext.Challenges.AnyAsync(cancellationToken))
            {
                return Ok(ApiResponse.Ok("Already seeded."));
            }

            var standardChallenges = new List<Challenge>
            {
               new() { Title = "Frequent Traveler", Description = "Take 4 trips this month to earn bonus points.", Type = ChallengeType.TotalTrips, GoalValue = 4, RewardPoints = 600, IsActive = true, Frequency = ChallengeFrequency.Monthly },
               new() { Title = "High Roller", Description = "Spend 2,500 EGP this month to earn a massive bonus.", Type = ChallengeType.TotalSpend, GoalValue = 2500, RewardPoints = 1000, IsActive = true, Frequency = ChallengeFrequency.Monthly },
               new() { Title = "The Getaway", Description = "Complete a round trip this month to earn points.", Type = ChallengeType.RoundTrip, GoalValue = 1, RewardPoints = 300, IsActive = true, Frequency = ChallengeFrequency.Monthly },
               new() { Title = "The Explorer", Description = "Book a multi-destination trip this month to earn extra points.", Type = ChallengeType.MultiDestination, GoalValue = 1, RewardPoints = 500, IsActive = true, Frequency = ChallengeFrequency.Monthly }
            };

            _dbContext.Challenges.AddRange(standardChallenges);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok("Challenges seeded successfully."));
        }
    }
}
