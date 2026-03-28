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
            return await _dbContext.Stops
                .AsNoTracking()
                .Where(s => !string.IsNullOrWhiteSpace(s.Governorate))
                .GroupBy(s => s.Governorate!)
                .Select(g => new GovernorateStationsDto
                {
                    Governorate = g.Key,
                    Stations = g.Select(s => new StationDto
                    {
                        Id = s.StopId,
                        ArabicName = s.ArabicName,
                        EnglishName = s.NormalizedSlug,
                        Slug = s.NormalizedSlug,
                        City = s.City
                    })
                    .OrderBy(s => s.EnglishName)
                    .ToList()
                })
                .OrderBy(g => g.Governorate)
                .ToListAsync(cancellationToken);
        }
    }
}
