using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Route
    {
        public int RouteId { get; set; }
        public int AgencyId { get; set; }
        public string RouteName { get; set; } = null!;

        // Navigation properties
        public Agency Agency { get; set; } = null!;
        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}
