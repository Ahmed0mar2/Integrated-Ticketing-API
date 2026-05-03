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
        public decimal TotalPrice { get; set; }
        public int SeatsBooked { get; set; }
        public DateTime BookingDate { get; set; }
        public bool IsMarketplacePurchase { get; set; }

        // Trip Details
        public string AgencyName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string OriginStation { get; set; } = string.Empty;
        public string DestinationStation { get; set; } = string.Empty;
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
        public bool IsOfferedForResale { get; set; }
    }
}
