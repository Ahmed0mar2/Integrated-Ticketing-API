namespace GP.Application.DTOs.Support
{
    public class AdminTicketResponseDto : TicketResponseDto
    {
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
    }
}
