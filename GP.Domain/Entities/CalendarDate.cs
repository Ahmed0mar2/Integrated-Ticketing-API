using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class CalendarDate
    {
        public int CalendarDateId { get; set; }
        public int ServiceId { get; set; }
        public DateOnly Date { get; set; }
        public ExceptionType ExceptionType { get; set; }

        // Navigation properties
        public Calendar Calendar { get; set; } = null!;
    }
}
