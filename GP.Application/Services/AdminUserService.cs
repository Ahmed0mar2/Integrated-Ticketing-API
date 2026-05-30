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
        // Load domain users with their country in one query
        var users = await _userRepository.GetAllAsNoTrackingAsync(cancellationToken, user => user.Country);

        var userList = users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToList();

        if (userList.Count == 0) return Array.Empty<UserDto>();

        // Collect domain ids and batch-load identity users that map to them
        var domainIds = userList.Select(u => u.UserId).ToList();

        var identityUsers = await _userManager.Users
            .AsNoTracking()
            .Where(au => au.DomainUserId != null && domainIds.Contains(au.DomainUserId.Value))
            .ToListAsync(cancellationToken);

        // Build mapping: identityUserId -> domainUserId
        var identityByDomainId = identityUsers
            .Where(iu => iu.DomainUserId.HasValue)
            .ToDictionary(iu => iu.DomainUserId!.Value, iu => iu);

        // Batch load user-role entries and role names for involved identity user ids
        var identityIds = identityUsers.Select(iu => iu.Id).ToList();

        var userRoles = await _context.Set<IdentityUserRole<int>>()
            .Where(ur => identityIds.Contains(ur.UserId))
            .ToListAsync(cancellationToken);

        var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();

        var roles = await _context.Set<IdentityRole<int>>()
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var roleNameById = roles.ToDictionary(r => r.Id, r => r.Name ?? string.Empty);

        // Map identity user id -> role names
        var rolesByIdentityId = userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ur => roleNameById.TryGetValue(ur.RoleId, out var rn) ? rn : string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToArray());

        // Now build DTOs with roles mapped by DomainUserId
        var result = new List<UserDto>(userList.Count);

        foreach (var user in userList)
        {
            var dto = MapToUserDto(user);

            if (identityByDomainId.TryGetValue(user.UserId, out var identityUser))
            {
                if (rolesByIdentityId.TryGetValue(identityUser.Id, out var userRoleNames))
                {
                    dto = dto with { Roles = userRoleNames };
                }
            }

            result.Add(dto);
        }

        return result;
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

        // get roles
        var roles = await _userManager.GetRolesAsync(identityUser);

        return new AdminUserDetailDto
        {
            UserId = domainUser.UserId,
            FullName = $"{domainUser.FirstName} {domainUser.FamilyName} {domainUser.LastName}".Trim(),
            Email = domainUser.Email,
            Phone = domainUser.Phone,
            IdType = domainUser.IdType,
            IdNumber = domainUser.IdNumber,
            TotalTripsCount = domainUser.TotalTripsCount,
            CreatedAt = AppTime.AsSchedule(domainUser.CreatedAt),
            LastLoginAt = identityUser.LastLoginAt.HasValue
                ? AppTime.AsSchedule(identityUser.LastLoginAt.Value)
                : null,
            IsActive = identityUser.IsActive,
            CountryCode = domainUser.Country?.CountryCode ?? string.Empty,
            CountryName = domainUser.Country?.CountryName ?? string.Empty,
            Roles = [.. roles]
        };
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
