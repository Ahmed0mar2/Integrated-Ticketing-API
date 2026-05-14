using GP.Application.DTOs.Profile;
using Microsoft.AspNetCore.Http;

namespace GP.Application.Interfaces;

public interface IUserProfileService
{
    Task<(bool Success, UserProfileDto? Data, string Message)> GetUserProfileAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, bool NotFound, string Message)> UpdateUserProfileAsync(
        int userId,
        UpdateUserProfileDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateFcmTokenAsync(
        int userId,
        string fcmToken,
        string deviceType,
        CancellationToken cancellationToken = default);

    Task<(bool Success, bool NotFound, string Message, string? NewImageUrl)> UploadProfilePictureAsync(
        int userId,
        IFormFile file,
        CancellationToken cancellationToken = default);
}