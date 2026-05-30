using GP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record RegisterRequest
    {
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string ConfirmPassword { get; init; } = null!;
        public string PhoneNumber { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string FamilyName { get; init; } = null!;
        public Gender Gender { get; init; }
        public DateOnly DateOfBirth { get; init; }
        public IdType? IdType { get; init; }
        public string? IdNumber { get; init; }
        public string CountryCode { get; init; } = null!; //ISO code (e.g., "EG")
    }
}
