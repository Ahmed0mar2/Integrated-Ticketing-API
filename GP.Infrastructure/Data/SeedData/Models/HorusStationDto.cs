using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class HorusStationDto
    {
        [JsonPropertyName("station_id")]
        public int? StationId { get; set; }

        [JsonPropertyName("departure_time")]
        public string DepartureTime { get; set; } = null!;
    }
}
