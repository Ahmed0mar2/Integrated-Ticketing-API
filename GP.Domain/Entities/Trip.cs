using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Trip
    {
        public int TripId { get; set; }
        public int AgencyId { get; set; }
        public string? TripCode { get; set; }
        // 1. Route Definition
        public int OriginStationId { get; set; }
        public int DestinationStationId { get; set; }

        // 2. Schedule Template (The Blueprint)
        public TimeOnly DepartureTime { get; set; }
        public int? TotalDurationMinutes { get; set; } 
        public int ServiceId { get; set; }

        // 4. Navigation Properties
        public Agency Agency { get; set; } = null!;
        public Calendar Calendar { get; set; } = null!;
        public Stop OriginStation { get; set; } = null!;
        public Stop DestinationStation { get; set; } = null!;
        public ICollection<TripStopTime> TripStopTimes { get; set; } = new List<TripStopTime>();
        public ICollection<TripFare> TripFares { get; set; } = new List<TripFare>();
        public ICollection<TripOccurrence> TripOccurrences { get; set; } = new List<TripOccurrence>();

    }
}
