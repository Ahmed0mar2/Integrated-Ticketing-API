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

        public ICollection<TrainTypeCoachConfig> TrainTypeConfigs { get; set; } = new List<TrainTypeCoachConfig>();
        public ICollection<TripClassPricing> PricingConfigs { get; set; } = new List<TripClassPricing>();
        public ICollection<TripOccurrenceClassInventory> Inventories { get; set; } = new List<TripOccurrenceClassInventory>();
    }

}

