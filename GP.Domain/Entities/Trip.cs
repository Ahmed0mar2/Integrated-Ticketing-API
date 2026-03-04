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
        public int? TrainTypeId { get; set; }

        // 1. Route Definition
        public int OriginStationId { get; set; }
        public int DestinationStationId { get; set; }

        // 2. Schedule Template (The Blueprint)
        public TimeOnly DepartureTime { get; set; }
        public int TotalDurationMinutes { get; set; } 
        public int ServiceId { get; set; }

        // 3. GoBus Specific Template Data
        public string? ServiceClass { get; set; }
        public decimal? BasePrice { get; set; }
        public int TotalSeats { get; set; }

        // 4. Navigation Properties
        public Agency Agency { get; set; } = null!;
        public TrainType? TrainType { get; set; } 
        public Calendar Calendar { get; set; } = null!;

        public ICollection<TripStopTime> TripStopTimes { get; set; } = new List<TripStopTime>();
        public ICollection<TripOccurrence> TripOccurrences { get; set; } = new List<TripOccurrence>();

        // For Trains with multiple classes (1st, 2nd, etc.)
        public ICollection<TripClassPricing> TripClassPricings { get; set; } = new List<TripClassPricing>();
    }
}
