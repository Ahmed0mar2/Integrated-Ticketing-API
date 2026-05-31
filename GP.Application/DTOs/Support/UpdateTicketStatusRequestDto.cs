using GP.Domain.Enums;

namespace GP.Application.DTOs.Support
{
    public class UpdateTicketStatusRequestDto
    {
        public TicketStatus Status { get; set; }
    }
}
