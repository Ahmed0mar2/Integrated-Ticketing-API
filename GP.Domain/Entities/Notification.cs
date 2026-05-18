using GP.Domain.Common;

namespace GP.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string MessageAr { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRead { get; set; } = false;

        public User User { get; set; } = null!;
    }
}