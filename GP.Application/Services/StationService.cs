using GP.Application.DTOs.Stations;
using GP.Application.Interfaces;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GP.Application.Services
{
    public class StationService : IStationService
    {
        private readonly ApplicationDbContext _dbContext;

        public StationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<GovernorateStationsDto>> GetStationsGroupedByGovernorateAsync(CancellationToken cancellationToken = default)
        {
            var stops = await _dbContext.Stops
                .AsNoTracking()
                .Where(s => !string.IsNullOrWhiteSpace(s.Governorate))
                .ToListAsync(cancellationToken);

            return stops
                .GroupBy(s => s.Governorate!)
                .Select(g => new GovernorateStationsDto
                {
                    Governorate = g.Key,
                    GovernorateAr = g.Select(s => s.GovernorateAr).FirstOrDefault(),
                    Stations = g.Select(s => new StationDto
                    {
                        Id = s.StopId,
                        ArabicName = s.ArabicName,
                        EnglishName = s.EnglishName,
                        Slug = s.NormalizedSlug,
                        City = s.City,
                        GovernorateAr = s.GovernorateAr
                    })
                    .OrderBy(s => s.EnglishName)
                    .ToList()
                })
                .OrderBy(g => g.Governorate)
                .ToList();
        }
    }
}
