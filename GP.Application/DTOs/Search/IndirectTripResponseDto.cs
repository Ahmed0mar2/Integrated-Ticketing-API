using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Search
{
    public class IndirectTripResponseDto
    {
        public int TotalDurationMinutes { get; set; }
        public int LayoverDurationMinutes { get; set; }
        public decimal TotalStartingPrice { get; set; }

        public List<TripSearchResponseDto> Legs { get; set; } = new();
    }
}
