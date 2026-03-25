using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class BlueBusTripDto
    {
        [JsonPropertyName("trip_id")]
        public string TripId { get; set; } = null!;

        [JsonPropertyName("bus_type")]
        public string BusType { get; set; } = null!;

        [JsonPropertyName("bus_capacity")]
        public int BusCapacity { get; set; }

        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        [JsonPropertyName("prices")]
        public Dictionary<string, string> Prices { get; set; } = new();

        [JsonPropertyName("prices_by_destination")]
        public Dictionary<string, Dictionary<string, string>> PricesByDestination { get; set; } = new();

        [JsonPropertyName("stations_from")]
        public List<BlueBusStationDto> StationsFrom { get; set; } = new();

        [JsonPropertyName("stations_to")]
        public List<BlueBusStationDto> StationsTo { get; set; } = new();
    }
}
