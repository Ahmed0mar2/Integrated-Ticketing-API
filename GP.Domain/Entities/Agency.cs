using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Agency
    {
        public int AgencyId { get; set; }
        public string AgencyName { get; set; } = null!;
        public AgencyType AgencyType { get; set; }

        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}
