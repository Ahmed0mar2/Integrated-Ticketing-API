using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class BlueBusStationDto
    {
        [JsonPropertyName("station")]
        public string Station { get; set; } = null!;

        [JsonPropertyName("departure_time")]
        public string? DepartureTime { get; set; }

        [JsonPropertyName("arrival_time")]
        public string? ArrivalTime { get; set; }
    }
}
