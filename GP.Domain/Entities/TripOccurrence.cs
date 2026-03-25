using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class TripOccurrence
    {
        public int TripOccurrenceId { get; set; }
        public int TripId { get; set; }
        public DateOnly OccurrenceDate { get; set; }
        public DateTime DepartureDateTime { get; set; }  
        public DateTime ArrivalDateTime { get; set; }    
        public bool IsActive { get; set; } = true;

        public Trip Trip { get; set; } = null!;
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<TripOccurrenceClassInventory> ClassInventories { get; set; } = new List<TripOccurrenceClassInventory>();
    }
}
