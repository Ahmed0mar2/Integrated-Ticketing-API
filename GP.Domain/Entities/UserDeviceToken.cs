using GP.Domain.Common;

namespace GP.Domain.Entities
{
    public class UserDeviceToken : BaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FcmToken { get; set; } = null!;
        public string DeviceType { get; set; } = null!;
        public DateTime LastUsedAt { get; set; } = AppTime.GetScheduleNow();

        public User User { get; set; } = null!;
    }
}
