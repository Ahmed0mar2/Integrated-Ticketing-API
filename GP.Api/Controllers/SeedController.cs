using GP.Application.Common;
using GP.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly GoBusDatabaseImporter _importer;
        private readonly TrainDatabaseImporter _trainImporter;

        public SeedController(GoBusDatabaseImporter importer, TrainDatabaseImporter trainImporter)
        {
            _importer = importer;
            _trainImporter = trainImporter;
        }

        [HttpPost("import-gobus")]
        public async Task<IActionResult> ImportGoBus()
        {
            var baseDir = AppContext.BaseDirectory;

            var dataPath = Path.Combine(baseDir, "Data", "SeedData", "GoBus");

            if (!Directory.Exists(dataPath))
            {
                return NotFound(new { message = $"SeedData folder not found at {dataPath}. Make sure the CSV files are set to 'Copy if newer'." });
            }

            Console.WriteLine("Starting GoBus Import Process...");

            var result = await _importer.ImportFromCsvAsync(
                stationsCsvPath: Path.Combine(dataPath, "stations.csv"),
                agenciesCsvPath: Path.Combine(dataPath, "agencies.csv"),
                coachClassesCsvPath: Path.Combine(dataPath, "coach_classes.csv"),
                tripsCsvPath: Path.Combine(dataPath, "normalized_trips.csv")
            );

            if (!result.Success)
            {
                Console.WriteLine($"❌ Import Failed: {result.Message}");
                return BadRequest(result);
            }

            Console.WriteLine("✅ Import Successful!");
            return Ok(result);
        }

        [HttpPost("import-trains")]
        public async Task<IActionResult> ImportTrains()
        {
            var baseDir = AppContext.BaseDirectory;
            var dataPath = Path.Combine(baseDir, "Data", "SeedData", "ENR");

            if (!Directory.Exists(dataPath))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"SeedData folder not found at {dataPath}. Make sure the CSV files are set to 'Copy if newer'."
                });
            }

            var result = await _trainImporter.ImportFromCsvAsync(
                agenciesPath: Path.Combine(dataPath, "agencies.csv"),
                typesPath: Path.Combine(dataPath, "train_types.csv"),
                classesPath: Path.Combine(dataPath, "coach_classes.csv"),
                configPath: Path.Combine(dataPath, "train_type_coach_config.csv"),
                stationsPath: Path.Combine(dataPath, "stations_final.csv"),
                tripsPath: Path.Combine(dataPath, "trips.csv"),
                stopTimesPath: Path.Combine(dataPath, "trip_stop_times.csv"),
                pricingPath: Path.Combine(dataPath, "trip_class_pricing.csv")
            );

            if (!result.Success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message ?? "Failed to import train data due to a validation or file error.",
                    Data = result 
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Train blueprints imported successfully!",
                Data = result
            });
        }
    }
}