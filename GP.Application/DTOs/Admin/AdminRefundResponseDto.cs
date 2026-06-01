namespace GP.Application.DTOs.Admin
{
    public class AdminRefundResponseDto
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string RefundStatus { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = string.Empty;
        public string OriginStationName { get; set; } = string.Empty;
        public string DestinationStationName { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
