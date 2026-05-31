using GP.Domain.Enums;

namespace GP.Application.DTOs.Support
{
    public class CreateTicketRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IssueCategory IssueCategory { get; set; }
    }
}
