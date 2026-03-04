using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record UserDto
    {
        public int UserId { get; init; }
        public string Email { get; init; } = null!;
        public string FullName { get; init; } = null!;
        public string PhoneNumber { get; init; } = null!;
        public string Gender { get; init; } = null!;
        public string CountryCode { get; init; } = null!; 
        public string CountryName { get; init; } = null!; 
        public string? ProfilePictureUrl { get; init; }
    }
}
