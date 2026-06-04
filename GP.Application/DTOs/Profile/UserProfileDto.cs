namespace GP.Application.DTOs.Profile;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? IdType { get; set; }
    public string? IdNumber { get; set; }
    public bool HasSetIdentityDetails { get; set; }
    public string PreferredLanguage { get; set; } = "en";

    // Country
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;

    // The Gamification Stats
    public int TotalTripsCount { get; set; }
    public int LoyaltyPointsBalance { get; set; }
    public List<ActiveChallengeDto> ActiveChallenges { get; set; } = new();
    public int ExpiringPointsAmount { get; set; } = 0;
    public DateTime? NextExpiryDate { get; set; }

    // The Digital Money Foundation
    public decimal WalletBalance { get; set; }
}