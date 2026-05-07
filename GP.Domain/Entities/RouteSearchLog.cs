using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class RouteSearchLog
    {
        public int Id { get; set; }
        public string OriginGov { get; set; } = string.Empty;
        public string DestinationGov { get; set; } = string.Empty;
        public DateTime SearchedAt { get; set; }
    }
}
