using GP.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Infrastructure.Identity
{
    public class RefreshToken
    {
        public int TokenId { get; set; }
        public int ApplicationUserId { get; set; }
        public string TokenHash { get; set; } = null!;
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }

        // Navigation
        public ApplicationUser ApplicationUser { get; set; } = null!;

        // Helper properties
        public bool IsExpired => AppTime.GetScheduleNow() >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
