using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Search
{
    public class TripClassOptionDto
    {
        public int CoachClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int RemainingSeats { get; set; }
        public decimal Price { get; set; }
    }
}
