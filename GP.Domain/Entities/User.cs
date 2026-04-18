using GP.Domain.Enums;

namespace GP.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }

        // Basic Info
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FamilyName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;

        // Personal Info
        public Gender Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? NationalIdNumber { get; set; }
        public bool IsNationalIdVerified { get; set; }
        public string Nationality { get; set; } = null!;

        // Location & Stats
        public string? CurrentCity { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int TotalTripsCount { get; set; }  // Completed trips count
        public decimal TotalDistanceTraveled { get; set; }  // In kilometers
        public decimal WalletBalance { get; set; } = 0m;
        public int CountryId { get; set; }

        // Account
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Booking> Bookings { get; set; } = [];
        public Country Country { get; set; } = null!;
        public ICollection<WalletTransaction> WalletTransactions { get; set; } = [];
    }
}
