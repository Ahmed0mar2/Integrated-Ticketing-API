using GP.Domain.Common;
using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class MarketplaceListing : BaseEntity
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public int SellerId { get; set; }
        public User Seller { get; set; } = null!;

        public decimal OriginalPrice { get; set; }
        public decimal AskingPrice { get; set; }

        public ListingStatus Status { get; set; } = ListingStatus.Available;
        public DateTime ListedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SoldAt { get; set; }
    }
}
