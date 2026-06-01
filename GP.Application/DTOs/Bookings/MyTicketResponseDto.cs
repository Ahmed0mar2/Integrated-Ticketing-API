using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Bookings
{
    public class MyTicketResponseDto
    {
        public int BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? RefundStatus { get; set; }
        public decimal TotalPrice { get; set; }
        public int SeatsBooked { get; set; }
        public DateTime BookingDate { get; set; }
        public bool IsMarketplacePurchase { get; set; }
        public int? ActiveListingId { get; set; }
        public bool IsOfferedForResale { get; set; }

        // Trip Details
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

        // Passenger & Seat Details
        public List<TicketPassengerDto> Passengers { get; set; } = new();
    }

    public class TicketPassengerDto
    {
        public int PassengerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IdNumber { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
    }
}
