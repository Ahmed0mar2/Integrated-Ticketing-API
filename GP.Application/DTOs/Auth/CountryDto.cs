using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record CountryDto
    {
        public string CountryCode { get; init; } = null!;
        public string CountryName { get; init; } = null!;
        public string NationalityName { get; init; } = null!;
        public string? PhoneCode { get; init; }
        public bool AllowsTrainBooking { get; init; }
    }
}
