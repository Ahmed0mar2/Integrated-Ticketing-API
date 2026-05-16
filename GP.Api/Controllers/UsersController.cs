using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Profile;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UsersController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
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

        var result = await _userProfileService.UploadProfilePictureAsync(userId.Value, file, cancellationToken);

        if (result.NotFound)
            return NotFound(ApiResponse.ErrorResponse(result.Message));

        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message));

        return Ok(ApiResponse<object>.SuccessResponse(
            new { profilePictureUrl = result.NewImageUrl },
            result.Message));
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
}