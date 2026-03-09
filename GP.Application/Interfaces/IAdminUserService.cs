using GP.Application.DTOs.Auth;
using GP.Application.DTOs.Admin;

namespace GP.Application.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    Task<(bool Success, bool NotFound, bool Conflict, string Message)> DeleteUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Success, bool NotFound, string Message)> ToggleUserStatusAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<(bool Success, bool NotFound, string Message)> AssignRoleAsync(
        int id,
        string role,
        CancellationToken cancellationToken = default);
}
