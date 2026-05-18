namespace GP.Application.DTOs.Bookings
{
    public class CartItemDto
    {
        public int BookingId { get; set; }
        public decimal TotalPrice { get; set; }
        public int SeatsBooked { get; set; }
        public DateTime HoldExpiresAt { get; set; }
        public string AgencyName { get; set; } = string.Empty;
        public string? AgencyNameAr { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string? ClassNameAr { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string OriginGov { get; set; } = string.Empty;
        public string DestinationGov { get; set; } = string.Empty;
        public DateTime BoardingTime { get; set; }
        public DateTime DropoffTime { get; set; }
        public List<TicketPassengerDto> Passengers { get; set; } = new();
    }
}
