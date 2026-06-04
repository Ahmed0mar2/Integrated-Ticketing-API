using System.ComponentModel.DataAnnotations;
using GP.Domain.Enums;

namespace GP.Application.DTOs.Profile;

public class UpdateUserProfileDto
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string FamilyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public IdType? IdType { get; set; }

    public string? IdNumber { get; set; }
}