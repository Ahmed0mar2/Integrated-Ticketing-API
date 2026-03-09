using GP.Application.Common;
using GP.Application.DTOs.Admin;
using GP.Application.DTOs.Auth;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Infrastructure.Data;
using GP.Infrastructure.Identity;
using GP.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GP.Application.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly ApplicationDbContext _context;

    public AdminUserService(
        IGenericRepository<User> userRepository,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        ApplicationDbContext context)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsNoTrackingAsync(cancellationToken, user => user.Country);

        return users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(MapToUserDto)
            .ToList();
    }

    public async Task<(bool Success, bool NotFound, bool Conflict, string Message)> DeleteUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        (bool Success, bool NotFound, bool Conflict, string Message) result = default;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var domainUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (domainUser is null)
            {
                result = (false, true, false, "User not found.");
                return;
            }

            var hasBookings = await _context.Bookings
                .AsNoTracking()
                .AnyAsync(booking => booking.UserId == userId, cancellationToken);

            if (hasBookings)
            {
                result = (false, false, true, "User cannot be deleted because related bookings exist.");
                return;
            }

            var applicationUser = await _userManager.Users
                .FirstOrDefaultAsync(user => user.DomainUserId == userId, cancellationToken);

            if (applicationUser is not null)
            {
                var identityResult = await _userManager.DeleteAsync(applicationUser);
                if (!identityResult.Succeeded)
                {
                    result = (
                        false,
                        false,
                        false,
                        string.Join(", ", identityResult.Errors.Select(error => error.Description))
                    );
                    return;
                }
            }

            await _userRepository.DeleteAsync(domainUser, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result = (true, false, false, "User deleted successfully.");
        });

        return result;
    }

    public async Task<AdminUserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var domainUser = await _userRepository.FirstOrDefaultAsNoTrackingAsync(
            user => user.UserId == id,
            cancellationToken,
            user => user.Country);

        if (domainUser is null)
        {
            return null;
        }

        var identityUser = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.DomainUserId == id, cancellationToken);

        if (identityUser is null)
        {
            return null;
        }

        return new AdminUserDetailDto(
            domainUser.UserId,
            $"{domainUser.FirstName} {domainUser.FamilyName} {domainUser.LastName}".Trim(),
            domainUser.Email,
            domainUser.Phone,
            domainUser.NationalIdNumber,
            domainUser.TotalTripsCount,
            domainUser.TotalDistanceTraveled,
            domainUser.CreatedAt,
            identityUser.LastLoginAt,
            identityUser.IsActive
        );
    }

    public async Task<(bool Success, bool NotFound, string Message)> ToggleUserStatusAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var identityUser = await _userManager.Users
            .FirstOrDefaultAsync(user => user.DomainUserId == id, cancellationToken);

        if (identityUser is null)
        {
            return (false, true, "User not found.");
        }

        var deactivatingUser = identityUser.IsActive;
        if (deactivatingUser && await _userManager.IsInRoleAsync(identityUser, Roles.Admin))
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var otherActiveAdmins = adminUsers.Count(user => user.Id != identityUser.Id && user.IsActive);

            if (otherActiveAdmins == 0)
            {
                return (false, false, "Cannot deactivate the last active Admin account.");
            }
        }

        identityUser.IsActive = !identityUser.IsActive;
        var result = await _userManager.UpdateAsync(identityUser);

        if (!result.Succeeded)
        {
            return (false, false, string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        return (true, false, identityUser.IsActive ? "User enabled successfully." : "User disabled successfully.");
    }

    public async Task<(bool Success, bool NotFound, string Message)> AssignRoleAsync(
        int id,
        string role,
        CancellationToken cancellationToken = default)
    {
        var identityUser = await _userManager.Users
            .FirstOrDefaultAsync(user => user.DomainUserId == id, cancellationToken);

        if (identityUser is null)
        {
            return (false, true, "User not found.");
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return (false, true, "Role does not exist.");
        }

        if (await _userManager.IsInRoleAsync(identityUser, role))
        {
            return (true, false, "User already in role.");
        }

        var result = await _userManager.AddToRoleAsync(identityUser, role);
        if (!result.Succeeded)
        {
            return (false, false, string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        return (true, false, "Role assigned successfully.");
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.FamilyName} {user.LastName}".Trim(),
            PhoneNumber = user.Phone,
            Gender = user.Gender.ToString(),
            CountryCode = user.Country?.CountryCode ?? string.Empty,
            CountryName = user.Country?.CountryName ?? string.Empty,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}
