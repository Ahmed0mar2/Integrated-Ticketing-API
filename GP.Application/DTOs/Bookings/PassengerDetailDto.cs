using System.ComponentModel.DataAnnotations;
using GP.Domain.Enums;

namespace GP.Application.DTOs.Bookings
{
    public class PassengerDetailDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 120)]
        public int Age { get; set; }

        [Required]
        public IdType IdType { get; set; }

        [Required]
        public string IdNumber { get; set; } = string.Empty;

        [Required]
        public string SeatNumber { get; set; } = string.Empty;
    }
}