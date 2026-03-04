using GP.Domain.Common;
using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Booking : BaseEntity
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int TripId { get; set; }
        public int OccurrenceId { get; set; }  
        public int CoachClassId { get; set; }  
        public DateOnly TravelDate { get; set; }  
        public int SeatsBooked { get; set; }  
        public decimal TotalPrice { get; set; }  
        public DateTime BookingTime { get; set; } 
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // Navigation properties
        public User User { get; set; } = null!;
        public Trip Trip { get; set; } = null!;
        public TripOccurrence Occurrence { get; set; } = null!;
        public CoachClass CoachClass { get; set; } = null!;
        public ICollection<BookingPassenger> BookingPassengers { get; set; } = new List<BookingPassenger>();
    }
}
