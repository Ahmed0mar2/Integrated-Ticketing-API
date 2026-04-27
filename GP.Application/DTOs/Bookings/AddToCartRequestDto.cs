using System.ComponentModel.DataAnnotations;

namespace GP.Application.DTOs.Bookings
{
    public class AddToCartRequestDto
    {
        [Required]
        public int TripOccurrenceId { get; set; }

        [Required]
        public int CoachClassId { get; set; }

        [Required]
        public int OriginStationId { get; set; }

        [Required]
        public int DestinationStationId { get; set; }

        [Required]
        public string ContactName { get; set; } = string.Empty;

        [Required]
        public string ContactPhone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "You must have at least one passenger.")]
        public List<PassengerDetailDto> Passengers { get; set; } = [];
    }
}
