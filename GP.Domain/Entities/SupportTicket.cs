using GP.Domain.Common;
using GP.Domain.Enums;

namespace GP.Domain.Entities
{
    public class SupportTicket : BaseEntity
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public IssueCategory Category { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public User User { get; set; } = null!;
    }
}
