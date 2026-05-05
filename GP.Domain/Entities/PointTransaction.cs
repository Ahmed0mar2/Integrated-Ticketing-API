using GP.Domain.Enums;
using System;

namespace GP.Domain.Entities
{
    public class PointTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int Amount { get; set; }
        public int AvailableAmount { get; set; }

        // e.g., "Earned from Trip to Alexandria", "Redeemed for Checkout Discount"
        public string Description { get; set; } = string.Empty;

        // Nullable because redemptions aren't always tied to a specific booking they earned points from
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int? ParentTransactionId { get; set; }
        public PointTransaction? ParentTransaction { get; set; }

        public PointSource Source { get; set; }
        public PointTransactionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UnlocksAt { get; set; } // The Trip's DepartureDateTime
        public DateTime? ExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
    }
}
