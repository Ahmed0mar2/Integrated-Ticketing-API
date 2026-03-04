using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class TrainTypeCoachConfig
    {
        public int TrainTypeCoachConfigId { get; set; }
        public int TrainTypeId { get; set; }
        public int CoachClassId { get; set; }
        public int NumberOfCoaches { get; set; }
        public int SeatsPerCoach { get; set; }

        public int TotalSeatsPerClass => NumberOfCoaches * SeatsPerCoach;

        // Navigation properties
        public TrainType TrainType { get; set; } = null!;
        public CoachClass CoachClass { get; set; } = null!;
    }
}
