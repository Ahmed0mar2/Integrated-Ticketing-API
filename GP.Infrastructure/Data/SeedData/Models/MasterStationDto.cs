using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class MasterStationDto
    {
        [JsonPropertyName("arabic")]
        public string Arabic { get; set; } = null!;

        [JsonPropertyName("normalized_slug")]
        public string NormalizedSlug { get; set; } = null!;

        [JsonPropertyName("city")]
        public string City { get; set; } = null!;

        [JsonPropertyName("governorate")]
        public string? Governorate { get; set; }

        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("mappings")]
        public StationMappingsDto Mappings { get; set; } = new();
    }

    public class StationMappingsDto
    {
        [JsonPropertyName("train_slug")]
        public string? TrainSlug { get; set; }

        [JsonPropertyName("gobus_id")]
        public object? GoBusId { get; set; }

        [JsonPropertyName("horus_id")]
        public object? HorusId { get; set; }

        [JsonPropertyName("bluebus_slug")]
        public string? BlueBusSlug { get; set; }
    }
}
