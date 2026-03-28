using GP.Application.Common;
using GP.Application.Interfaces;
using GP.Application.Services;
using GP.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Authorize(Policy = Policies.RequireAdminRole)]
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly MasterStationSeeder _masterSeeder;
        private readonly GoBusTripSeeder _goBusSeeder;
        private readonly HorusTripSeeder _horusSeeder;
        private readonly BlueBusTripSeeder _blueBusSeeder;
        private readonly EnrTripSeeder _enrSeeder;
        private readonly IServiceProvider _serviceProvider;

        public SeedController(
            MasterStationSeeder masterSeeder,
            GoBusTripSeeder goBusSeeder,
            HorusTripSeeder horusSeeder,
            BlueBusTripSeeder blueBusSeeder,
            EnrTripSeeder enrSeeder,
            IServiceProvider serviceProvider)
        {
            _masterSeeder = masterSeeder;
            _goBusSeeder = goBusSeeder;
            _horusSeeder = horusSeeder;
            _blueBusSeeder = blueBusSeeder;
            _enrSeeder = enrSeeder;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Initializes the identity system by seeding default roles and Admin credentials.
        /// </summary>
        /// <remarks>
        /// This endpoint should be run first to ensure the authorization tables (Roles and Users) are populated before interacting with secure endpoints.
        /// Responses are wrapped in `ApiResponse`.
        /// </remarks>
        [HttpPost("init-identity")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InitializeIdentity()
        {
            try
            {
                await DbInitializer.InitializeAsync(_serviceProvider);
                return Ok(ApiResponse.Ok("Roles and Admin credentials seeded successfully!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail("Failed to initialize identity", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Imports the unified master station spatial database and agency identity mappings from a JSON file.
        /// </summary>
        /// <remarks>
        /// This must be executed before importing any agency trips, as it builds the foundational geography (Cities, Governorates, GPS) and the cross-agency mapping table.
        /// Responses are wrapped in `ApiResponse`.
        /// </remarks>
        /// <param name="filePath">The absolute file path to the master_stations.json file.</param>
        [HttpPost("import-master-stations")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportMasterStations()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "Master_stations.json");
            if (!System.IO.File.Exists(filePath))
                return NotFound(ApiResponse.Fail($"File not found at: {filePath}"));

            try
            {
                await _masterSeeder.SeedStationsAsync(filePath);
                return Ok(ApiResponse.Ok("Master Stations imported successfully!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail("Import failed", [ex.Message]));
            }
        }

        /// <summary>
        /// Imports Horus bus trips, schedules, and flat pricing matrices from a JSON file.
        /// </summary>
        /// <remarks>
        /// Responses are wrapped in `ApiResponse`.
        /// </remarks>
        [HttpPost("import-horus")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportHorus()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "Horus_trips.json");

            if (!System.IO.File.Exists(filePath))
                return NotFound(ApiResponse.Fail($"File not found at: {filePath}"));

            try
            {
                await _horusSeeder.SeedTripsAsync(filePath);
                return Ok(ApiResponse.Ok("Horus Trips imported successfully!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail("Import failed", [ex.Message]));
            }
        }

        /// <summary>
        /// Imports GoBus trips, normalizes class capacities, and generates unique schedule blueprints.
        /// </summary>
        /// <remarks>
        /// Because GoBus data lacks static Trip IDs, this endpoint automatically groups data by origin, destination, and departure time to generate synthetic Trip Blueprints.
        /// Responses are wrapped in `ApiResponse`.
        /// </remarks>
        [HttpPost("import-gobus")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportGoBus()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "gobus_trips.json");

            if (!System.IO.File.Exists(filePath))
                return NotFound(ApiResponse.Fail($"File not found at: {filePath}"));

            try
            {
                await _goBusSeeder.SeedTripsAsync(filePath);
                return Ok(ApiResponse.Ok("GoBus Trips imported successfully!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail("Import failed", [ex.Message]));
            }
        }

        /// <summary>
        /// Imports premium Blue Bus trips, including granular destination-based pricing matrices.
        /// </summary>
        /// <remarks>
        /// Responses are wrapped in `ApiResponse`.
        /// </remarks>
        [HttpPost("import-bluebus")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportBlueBus()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "bluebus_trips.json");

            if (!System.IO.File.Exists(filePath))
                return NotFound(ApiResponse.Fail($"File not found at: {filePath}"));

            try
            {
                await _blueBusSeeder.SeedTripsAsync(filePath);
                return Ok(ApiResponse.Ok("Blue Bus Trips imported successfully!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail("Import failed", [ex.Message]));
            }
        }

        /// <summary>
        /// Imports ENR train blueprints, multi-stop sequences, and tiered pricing matrices.
        /// </summary>
        /// <remarks>
        /// This endpoint requires two files: one for the train stop schedules and one for the complex class-based pricing rules. It also calculates overnight duration math automatically.
        /// Responses are wrapped in `ApiResponse`.
        /// </remarks>
        [HttpPost("import-trains")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportTrains()
        {
            var stopsFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "train_stops.json");
            var pricesFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "trains_trips.json");

            if (!System.IO.File.Exists(stopsFilePath) || !System.IO.File.Exists(pricesFilePath))
                return NotFound(ApiResponse.Fail("One or more train JSON files are missing from Data/SeedData."));

            try
            {
                await _enrSeeder.SeedTrainsAsync(stopsFilePath, pricesFilePath);
                return Ok(ApiResponse.Ok("ENR Trains imported successfully!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail("Import failed", [ex.Message]));
            }
        }

        /// <summary>
        /// Generates the physical dates and seat inventories for all active trips for the next 60 days.
        /// </summary>
        [HttpPost("generate-occurrences")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateOccurrences([FromServices] ITripOccurrenceService service)
        {
            await service.GenerateOccurrencesAsync(60);
            return Ok(ApiResponse.Ok("60-Day Calendar generated successfully!"));
        }
    }
}