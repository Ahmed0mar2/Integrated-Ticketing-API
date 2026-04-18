using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class HorusTripDto
    {
        [JsonPropertyName("trip_id")]
        public string TripId { get; set; } = null!;

        [JsonPropertyName("bus_type")]
        public string BusType { get; set; } = null!;

        [JsonPropertyName("bus_capacity")]
        public int BusCapacity { get; set; }

        [JsonPropertyName("price_egp")]
        public string PriceEgp { get; set; } = null!;

        [JsonPropertyName("to_station_id")]
        public int? ToStationId { get; set; }

        [JsonPropertyName("to_en")]
        public string? ToEn { get; set; }

        [JsonPropertyName("stations_from")]
        public List<HorusStationDto> StationsFrom { get; set; } = new();

        [JsonPropertyName("seats_info")]
        public JsonElement SeatsInfo { get; set; }
    }
}
