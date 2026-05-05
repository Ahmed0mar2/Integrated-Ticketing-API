using System;

namespace GP.Domain.Entities
{
    public class DiscountRule
    {
        public int Id { get; set; }
        public int TargetTrips { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal MaxDiscountAmount { get; set; } // Financial protection (e.g., max 300 EGP)
        public bool IsActive { get; set; } = true;
    }
}
