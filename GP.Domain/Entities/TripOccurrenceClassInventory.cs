using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class TripOccurrenceClassInventory
    {
        public int TripOccurrenceClassInventoryId { get; set; }
        public int TripOccurrenceId { get; set; }
        public int CoachClassId { get; set; }
        public int TotalSeats { get; set; }
        public int RemainingSeats { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        public TripOccurrence TripOccurrence { get; set; } = null!;
        public CoachClass CoachClass { get; set; } = null!;
    }
}
