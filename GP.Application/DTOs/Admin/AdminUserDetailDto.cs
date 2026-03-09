namespace GP.Application.DTOs.Admin
{
    public record AdminUserDetailDto
    (
        int UserId,
        string FullName,
        string Email,
        string Phone,
        string? NationalIdNumber,
        int TotalTripsCount,
        decimal TotalDistanceTraveled,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        bool IsActive
    );
}