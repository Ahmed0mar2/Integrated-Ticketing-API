using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class CoachClass
    {
        public int CoachClassId { get; set; }
        public string Name { get; set; } = null!;
        public int DefaultCapacity { get; set; }

        public ICollection<TripFare> PricingConfigs { get; set; } = new List<TripFare>();
        public ICollection<TripOccurrenceClassInventory> Inventories { get; set; } = new List<TripOccurrenceClassInventory>();
    }
}

