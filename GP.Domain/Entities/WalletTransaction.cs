using GP.Domain.Common;
using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class WalletTransaction : BaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }

        // e.g., "Deposit via Visa ending in 4242"
        public string Description { get; set; } = string.Empty;

        // NEW: Arabic localization
        public string DescriptionAr { get; set; } = string.Empty;

        public int? BookingId { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public Booking? Booking { get; set; }
    }
}
