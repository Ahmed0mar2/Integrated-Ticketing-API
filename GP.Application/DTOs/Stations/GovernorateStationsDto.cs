using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Stations
{
    public class GovernorateStationsDto
    {
        public string Governorate { get; set; } = string.Empty;
        public List<StationDto> Stations { get; set; } = [];
    }
}
