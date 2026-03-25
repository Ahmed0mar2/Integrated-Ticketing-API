using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class TripFare
    {
        public int TripFareId { get; set; }
        public int TripId { get; set; }

        public int OriginStationId { get; set; }
        public int DestinationStationId { get; set; }
        public int CoachClassId { get; set; }
        public decimal Price { get; set; }

        public Trip Trip { get; set; } = null!;
        public Stop OriginStation { get; set; } = null!;
        public Stop DestinationStation { get; set; } = null!;
        public CoachClass CoachClass { get; set; } = null!;
    }
}
