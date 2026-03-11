namespace GP.Application.DTOs.Admin
{
    public record AdminUserDetailDto
    {
        public int UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string? NationalIdNumber { get; init; }
        public int TotalTripsCount { get; init; }
        public decimal TotalDistanceTraveled { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public bool IsActive { get; init; }
        public string CountryCode { get; init; } = string.Empty;
        public string CountryName { get; init; } = string.Empty;
        public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

        public AdminUserDetailDto() { }
    }
}