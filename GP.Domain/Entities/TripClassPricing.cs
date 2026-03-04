using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class TripClassPricing
    {
        public int TripClassPricingId { get; set; }
        public int TripId { get; set; }
        public int CoachClassId { get; set; }
        public PricingType PricingType { get; set; } 
        public decimal FullPrice { get; set; }
        public decimal? FullDistanceKm { get; set; }  
        public decimal? MinimumPrice { get; set; }
        public int? RoundingStep { get; set; }

        public Trip Trip { get; set; } = null!;
        public CoachClass CoachClass { get; set; } = null!;
    }
}
