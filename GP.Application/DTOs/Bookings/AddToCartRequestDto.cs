using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        [MinLength(1, ErrorMessage = "You must have at least one passenger.")]
        public List<PassengerDetailDto> Passengers { get; set; } = [];
    }
}
