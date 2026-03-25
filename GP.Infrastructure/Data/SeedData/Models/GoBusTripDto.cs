using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class GoBusTripDto
    {
        [JsonPropertyName("from_station_id")]
        public int FromStationId { get; set; }

        [JsonPropertyName("to_station_id")]
        public int ToStationId { get; set; }

        [JsonPropertyName("trip_datetime")]
        public DateTime TripDateTime { get; set; }

        [JsonPropertyName("trip_price")]
        public decimal TripPrice { get; set; }

        [JsonPropertyName("total_seats")]
        public int TotalSeats { get; set; }

        [JsonPropertyName("service_class")]
        public string ServiceClass { get; set; } = null!;

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }
    }
}
