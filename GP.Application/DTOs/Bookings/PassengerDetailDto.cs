using System.ComponentModel.DataAnnotations;

namespace GP.Application.DTOs.Bookings
{
    public class PassengerDetailDto
    {
        public string? SeatNumber { get; set; }

        public string? PassengerName { get; set; }

        public string? IdType { get; set; }

        public string? IdNumber { get; set; }
    }
}