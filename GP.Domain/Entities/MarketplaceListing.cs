using GP.Domain.Common;
using GP.Domain.Enums;

namespace GP.Domain.Entities
{
    public class MarketplaceListing : BaseEntity
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        // The exact passenger ticket being resold
        public int PassengerId { get; set; }

        public int SellerId { get; set; }
        public User Seller { get; set; } = null!;

        public decimal OriginalPrice { get; set; }
        public decimal AskingPrice { get; set; }

        public ListingStatus Status { get; set; } = ListingStatus.Available;
        public DateTime ListedAt { get; set; } = AppTime.GetScheduleNow();
        public DateTime? SoldAt { get; set; }
    }
}
