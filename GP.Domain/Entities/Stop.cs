using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Domain.Entities
{
    public class Stop
    {
        public int StopId { get; set; }

        // Unified Names
        public string ArabicName { get; set; } = null!;
        public string NormalizedSlug { get; set; } = null!;
        public string City { get; set; } = null!;

        [NotMapped]
        public string EnglishName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NormalizedSlug)) return string.Empty;
                var withSpaces = NormalizedSlug.Replace("-", " ");
                return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(withSpaces);
            }
        }
        public string? Governorate { get; set; }
        public string? GovernorateAr { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public ICollection<TripStopTime> TripStopTimes { get; set; } = new List<TripStopTime>();

        // The link to the mappings table
        public ICollection<StopAgencyMapping> AgencyMappings { get; set; } = new List<StopAgencyMapping>();
    }
}
