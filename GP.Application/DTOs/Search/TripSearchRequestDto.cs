using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Search
{
    public class TripSearchRequestDto
    {
        public DateOnly TravelDate { get; set; }

        // --- ORIGIN ---
        public string? FromGovernorate { get; set; }
        public int? FromStationId { get; set; }

        // --- DESTINATION ---
        public string? ToGovernorate { get; set; }
        public int? ToStationId { get; set; }

        public int Passengers { get; set; } = 1; // Default to 1
        public TransportMode Transport { get; set; } = TransportMode.All; // Default to All
    }
}
