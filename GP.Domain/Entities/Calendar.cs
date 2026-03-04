using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Calendar
    {
        public int ServiceId { get; set; }
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        // Navigation properties
        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
        public ICollection<CalendarDate> CalendarDates { get; set; } = new List<CalendarDate>();
    }
}
