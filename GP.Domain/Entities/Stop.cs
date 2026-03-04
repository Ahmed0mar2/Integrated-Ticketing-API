using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Stop
    {
        public int StopId { get; set; }
        public string StopName { get; set; } = null!;
        public string City { get; set; } = null!;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public ICollection<TripStopTime> TripStopTimes { get; set; } = new List<TripStopTime>();

    }
}
