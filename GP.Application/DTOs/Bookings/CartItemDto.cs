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
        public string OriginStationNameAr { get; set; } = string.Empty;
        public string OriginStationNameEn { get; set; } = string.Empty;
        public string? OriginGovAr { get; set; }
        public string? OriginGovEn { get; set; }
        public string DestinationStationNameAr { get; set; } = string.Empty;
        public string DestinationStationNameEn { get; set; } = string.Empty;
        public string? DestinationGovAr { get; set; }
        public string? DestinationGovEn { get; set; }
        public DateTime BoardingTime { get; set; }
        public DateTime DropoffTime { get; set; }
        public List<TicketPassengerDto> Passengers { get; set; } = new();
    }
}
