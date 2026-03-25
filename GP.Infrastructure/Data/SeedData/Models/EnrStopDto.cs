using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class EnrStopDto
    {
        [JsonPropertyName("station_slug")]
        public string StationSlug { get; set; } = null!;
        [JsonPropertyName("arrival")]
        public string? Arrival { get; set; }
        [JsonPropertyName("departure")]
        public string? Departure { get; set; }
        [JsonPropertyName("stop_order")]
        public int StopOrder { get; set; }
    }
}
