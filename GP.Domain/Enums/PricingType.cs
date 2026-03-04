using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Enums
{
    public enum PricingType
    {
        FIXED = 0,      // Flat price (GoBus)
        DISTANCE = 1    // Distance-based (Trains)
    }
}
