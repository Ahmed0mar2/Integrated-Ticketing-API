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
        public string? ClassNameAr { get; set; }
        public int DefaultCapacity { get; set; }
        public string? LayoutType { get; set; }
        public int DeckCount { get; set; } = 1;
        public string? SeatMapJson { get; set; }

        public ICollection<TripFare> PricingConfigs { get; set; } = new List<TripFare>();
        public ICollection<TripOccurrenceClassInventory> Inventories { get; set; } = new List<TripOccurrenceClassInventory>();
    }
}

