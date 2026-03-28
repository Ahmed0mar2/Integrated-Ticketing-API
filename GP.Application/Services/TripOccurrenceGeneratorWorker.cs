//using GP.Application.Interfaces;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GP.Application.Services
//{
//    public class TripOccurrenceGeneratorWorker : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<TripOccurrenceGeneratorWorker> _logger;

//        public TripOccurrenceGeneratorWorker(IServiceProvider serviceProvider, ILogger<TripOccurrenceGeneratorWorker> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                _logger.LogInformation("Background Worker: Checking Trip Occurrences at {time}", DateTimeOffset.Now);

//                try
//                {
//                    using var scope = _serviceProvider.CreateScope();
//                    var generator = scope.ServiceProvider.GetRequiredService<ITripOccurrenceService>();

//                    // Generate for the next 60 days
//                    await generator.GenerateOccurrencesAsync(60, stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "FATAL: Error occurred during background occurrence generation.");
//                }

//                // Go to sleep for 24 hours. It will wake up tomorrow and generate Day 61
//                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
//            }
//        }
//    }
//}
