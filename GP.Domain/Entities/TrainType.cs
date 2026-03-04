using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class TrainType
    {
        public int TrainTypeId { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<TrainTypeCoachConfig> CoachConfigs { get; set; } = new List<TrainTypeCoachConfig>();
    }
}
