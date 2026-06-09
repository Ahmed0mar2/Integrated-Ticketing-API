using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Notifications;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value, limit, cancellationToken);
            return Ok(ApiResponse<List<NotificationDto>>.SuccessResponse(notifications, "Notifications retrieved successfully."));
        }

        [HttpPatch("{id:int}/read")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAsRead([FromRoute] int id, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));
            }

            await _notificationService.MarkAsReadAsync(id, userId.Value, cancellationToken);
            return Ok(ApiResponse.Ok("Notification marked as read."));
        }

        [HttpPatch("read-all")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));
            }

            await _notificationService.MarkAllAsReadAsync(userId.Value, cancellationToken);
            return Ok(ApiResponse.Ok("All notifications marked as read."));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNotification(int id, CancellationToken cancellationToken)
        {
            var userId = User.GetDomainUserId();

            if (userId == null)
            {
                return Unauthorized(ApiResponse.ErrorResponse("User is not authenticated."));
            }

            var success = await _notificationService.DeleteNotificationAsync(userId.Value, id, cancellationToken);

            if (!success)
            {
                return NotFound(ApiResponse.ErrorResponse("Notification not found."));
            }

            return Ok(ApiResponse.SuccessResponse("Notification deleted successfully."));
        }
    }
}