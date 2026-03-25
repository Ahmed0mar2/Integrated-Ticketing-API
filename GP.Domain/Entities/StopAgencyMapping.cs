using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class StopAgencyMapping
    {
        public int StopAgencyMappingId { get; set; }

        public int StopId { get; set; }
        public int AgencyId { get; set; }
        public string ExternalStationId { get; set; } = null!;

        public Stop Stop { get; set; } = null!;
        public Agency Agency { get; set; } = null!;
    }
}
