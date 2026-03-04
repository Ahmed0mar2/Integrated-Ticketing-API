using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Country
    {
        public int CountryId { get; set; }
        public string CountryCode { get; set; } = null!; // ISO Alpha-2 (EG, SA, AE)
        public string CountryName { get; set; } = null!; // Egypt, Saudi Arabia
        public string NationalityName { get; set; } = null!; // Egyptian, Saudi
        public string? PhoneCode { get; set; } // +20, +966
        public bool AllowsTrainBooking { get; set; } // Only EG 

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
