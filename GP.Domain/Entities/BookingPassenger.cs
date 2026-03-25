using GP.Domain.Common;
using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class BookingPassenger : BaseEntity
    {
        public int PassengerId { get; set; }
        public int BookingId { get; set; }
        public string Name { get; set; } = null!;  
        public int Age { get; set; }
        public int OccurrenceId { get; set; }
        public int CoachClassId { get; set; }
        public string SeatNumber { get; set; } = null!;  
        public IdType IdType { get; set; }  
        public string IdNumber { get; set; } = null!;  

        // Navigation properties
        public Booking Booking { get; set; } = null!;
    }
}
