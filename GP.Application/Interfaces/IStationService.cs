using GP.Application.DTOs.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Interfaces
{
    public interface IStationService
    {
        Task<List<GovernorateStationsDto>> GetStationsGroupedByGovernorateAsync(CancellationToken cancellationToken = default);
    }
}
