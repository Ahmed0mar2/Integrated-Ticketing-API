using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class TripStopTime
    {
        public int TripStopTimeId { get; set; }
        public int TripId { get; set; }
        public int StationId { get; set; }
        public int StopSequence { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public TimeOnly? DepartureTime { get; set; }

        public Trip Trip { get; set; } = null!;
        public Stop Station { get; set; } = null!;
    }
}
