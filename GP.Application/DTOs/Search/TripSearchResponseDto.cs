using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Search
{
    public class TripSearchResponseDto
    {
        public int TripOccurrenceId { get; set; }
        public int TripId { get; set; }
        public string AgencyName { get; set; } = string.Empty;

        // Segment-specific times (requested boarding -> requested dropoff)
        public DateTime BoardingTime { get; set; }
        public DateTime DropoffTime { get; set; }

        // Global trip times (entire occurrence)
        public DateTime DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }

        public int? TotalDurationMinutes { get; set; }
        public int OriginStationId { get; set; }
        public int DestinationStationId { get; set; }
        public string OriginStationName { get; set; } = string.Empty;
        public string OriginGovernorate { get; set; } = string.Empty;

        public string DestinationStationName { get; set; } = string.Empty;
        public string DestinationGovernorate { get; set; } = string.Empty;

        public decimal StartingPrice { get; set; }
        public List<IntermediateStopDto> RouteStops { get; set; } = new();

        public List<TripClassOptionDto> AvailableClasses { get; set; } = new();
    }
}
