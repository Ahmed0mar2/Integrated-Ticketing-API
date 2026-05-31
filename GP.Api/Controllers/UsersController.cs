using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Profile;
using GP.Application.Interfaces;
using GP.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GP.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UsersController(
        IUserProfileService userProfileService,
        ApplicationDbContext dbContext,
        IWebHostEnvironment webHostEnvironment)
    {
        _userProfileService = userProfileService;
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
    }

    /// <summary>
    /// Gets the logged-in user's profile, stats, and wallet balance
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetDomainUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));

        var result = await _userProfileService.GetUserProfileAsync(userId.Value, cancellationToken);

        if (!result.Success)
            return NotFound(ApiResponse.ErrorResponse(result.Message));

        return Ok(ApiResponse<UserProfileDto>.SuccessResponse(result.Data!, result.Message));
    }

    /// <summary>
    /// Updates the logged-in user's basic information
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateUserProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetDomainUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));

        var result = await _userProfileService.UpdateUserProfileAsync(userId.Value, dto, cancellationToken);

        if (result.NotFound)
            return NotFound(ApiResponse.ErrorResponse(result.Message));

        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message));

        return Ok(ApiResponse.Ok(result.Message));
    }

    /// <summary>
    /// Uploads or updates the user's profile picture
    /// </summary>
    [HttpPost("me/profile-picture")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProfilePicture(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.ErrorResponse("No file was uploaded."));

        var userId = User.GetDomainUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId.Value, cancellationToken);

        if (user == null)
            return NotFound(ApiResponse.ErrorResponse("User not found."));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            return BadRequest(ApiResponse.ErrorResponse(
                $"Invalid file extension. Allowed extensions are: {string.Join(", ", allowedExtensions)}"));

        if (string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("Web root path is not configured."));
        }

        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            var oldRelative = user.ProfilePictureUrl.TrimStart('/');
            var oldPath = Path.Combine(_webHostEnvironment.WebRootPath,
                oldRelative.Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var newImageUrl = $"/images/profiles/{fileName}";
        user.ProfilePictureUrl = newImageUrl;
        user.UpdatedAt = AppTime.GetScheduleNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(
            new { profilePictureUrl = newImageUrl },
            "Profile picture uploaded successfully."));
    }

    /// <summary>
    /// Registers or updates the user's FCM token for offline notifications.
    /// </summary>
    [HttpPost("fcm-token")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateFcmToken(
        [FromBody] FcmTokenRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetDomainUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));

        await _userProfileService.UpdateFcmTokenAsync(userId.Value, dto.Token, dto.DeviceType, cancellationToken);

        return Ok(ApiResponse.Ok("FCM token updated successfully."));
    }

    /// <summary>
    /// Updates the logged-in user's preferred language.
    /// </summary>
    [HttpPut("language")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferredLanguage(
        [FromBody] UpdateLanguageRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetDomainUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId.Value, cancellationToken);

        if (user == null)
            return NotFound(ApiResponse.ErrorResponse("User not found."));

        var requested = dto.Language?.Trim();
        user.PreferredLanguage = requested == "ar" ? "ar" : "en";
        user.UpdatedAt = AppTime.GetScheduleNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Language updated successfully."));
    }
}