using GP.Application.Common;
using GP.Application.DTOs.Admin;
using GP.Application.DTOs.Auth;
using GP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GP.Api.Controllers
{
    [ApiController]
    [Authorize(Policy = Policies.RequireAdminRole)]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        /// <summary>
        /// Get all users.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
        {
            var users = await _adminUserService.GetAllUsersAsync(cancellationToken);

            return Ok(ApiResponse<IReadOnlyList<UserDto>>.SuccessResponse(
                users,
                "Users retrieved successfully."));
        }

        /// <summary>
        /// Get user details by id (domain + identity fields)
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(int id, CancellationToken cancellationToken)
        {
            var user = await _adminUserService.GetUserByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResponse("User not found."));
            }

            return Ok(ApiResponse<AdminUserDetailDto>.SuccessResponse(user, "User retrieved successfully."));
        }

        /// <summary>
        /// Toggle user active status
        /// </summary>
        [HttpPatch("{id:int}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleUserStatus(int id, CancellationToken cancellationToken)
        {
            var result = await _adminUserService.ToggleUserStatusAsync(id, cancellationToken);

            if (result.NotFound)
            {
                return NotFound(ApiResponse.ErrorResponse(result.Message));
            }

            if (!result.Success)
            {
                return BadRequest(ApiResponse.ErrorResponse(result.Message));
            }

            return Ok(ApiResponse.Ok(result.Message));
        }

        /// <summary>
        /// Assign role to user
        /// </summary>
        [HttpPost("{id:int}/roles")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminUserService.AssignRoleAsync(id, request.Role, cancellationToken);

            if (result.NotFound)
            {
                return NotFound(ApiResponse.ErrorResponse(result.Message));
            }

            if (!result.Success)
            {
                return BadRequest(ApiResponse.ErrorResponse(result.Message));
            }

            return Ok(ApiResponse.Ok(result.Message));
        }

        /// <summary>
        /// Delete a specific user.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
        {
            var result = await _adminUserService.DeleteUserAsync(id, cancellationToken);

            if (result.NotFound)
            {
                return NotFound(ApiResponse.ErrorResponse(result.Message));
            }

            if (result.Conflict)
            {
                return Conflict(ApiResponse.ErrorResponse(result.Message));
            }

            if (!result.Success)
            {
                return BadRequest(ApiResponse.ErrorResponse(result.Message));
            }

            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}
