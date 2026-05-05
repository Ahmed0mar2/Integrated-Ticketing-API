using GP.Application.DTOs.Profile;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using GP.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GP.Application.Services;

public class UserProfileService : IUserProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        ApplicationDbContext context,
        IFileService fileService,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<UserProfileService> logger)
    {
        _context = context;
        _fileService = fileService;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, UserProfileDto? Data, string Message)> GetUserProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserChallenges.Where(uc => !uc.IsCompleted))
                .ThenInclude(uc => uc.Challenge)
            .Include(u => u.Country)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
            return (false, null, "User not found.");

        var availablePoints = await _context.PointTransactions
            .AsNoTracking()
            .Where(pt => pt.UserId == userId
                         && pt.AvailableAmount > 0
                         && pt.Status == PointTransactionStatus.Available
                         && !pt.IsExpired
                         && pt.ExpiresAt.HasValue)
            .ToListAsync(cancellationToken);

        DateTime? nextExpiry = null;
        int expiringAmount = 0;

        if (availablePoints.Count > 0)
        {
            nextExpiry = availablePoints.Min(pt => pt.ExpiresAt);

            if (nextExpiry.HasValue)
            {
                expiringAmount = availablePoints
                    .Where(pt => pt.ExpiresAt!.Value.Date == nextExpiry.Value.Date)
                    .Sum(pt => pt.AvailableAmount);
            }
        }

        var dto = new UserProfileDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            FamilyName = user.FamilyName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.Phone,
            Gender = user.Gender.ToString(),
            ProfilePictureUrl = user.ProfilePictureUrl,
            CountryCode = user.Country?.CountryCode ?? string.Empty,
            CountryName = user.Country?.CountryName ?? string.Empty,
            TotalTripsCount = user.TotalTripsCount,
            LoyaltyPointsBalance = user.LoyaltyPointsBalance,
            ActiveChallenges = user.UserChallenges.Select(uc => new ActiveChallengeDto
            {
                ChallengeId = uc.ChallengeId,
                Title = uc.Challenge.Title,
                Type = (int)uc.Challenge.Type,
                CurrentProgress = uc.CurrentProgress,
                GoalValue = uc.Challenge.GoalValue,
                RewardPoints = uc.Challenge.RewardPoints
            }).ToList(),
            ExpiringPointsAmount = expiringAmount,
            NextExpiryDate = nextExpiry,
            WalletBalance = user.WalletBalance
        };

        return (true, dto, "Profile retrieved successfully.");
    }

    public async Task<(bool Success, bool NotFound, string Message)> UpdateUserProfileAsync(
        int userId,
        UpdateUserProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        (bool Success, bool NotFound, string Message) result = default;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                if (user == null)
                {
                    result = (false, true, "User not found.");
                    return;
                }

                // Domain-level uniqueness checks
                if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailInUse = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Email == dto.Email && u.UserId != userId, cancellationToken);

                    if (emailInUse)
                    {
                        result = (false, false, "Email is already in use by another account.");
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != user.Phone)
                {
                    // Optional: check domain-level phone uniqueness if required
                    var phoneInUse = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Phone == dto.PhoneNumber && u.UserId != userId, cancellationToken);

                    if (phoneInUse)
                    {
                        result = (false, false, "Phone number is already in use by another account.");
                        return;
                    }
                }

                // Find associated identity user (if any)
                var identityUser = await _userManager.Users.FirstOrDefaultAsync(u => u.DomainUserId == userId, cancellationToken);

                // Track if email changed so we can send verification after commit
                var emailChanged = false;
                var newEmail = dto.Email?.Trim();

                // Update basic domain fields
                user.FirstName = dto.FirstName;
                user.FamilyName = dto.FamilyName;
                user.LastName = dto.LastName;

                // Phone number handling
                if (dto.PhoneNumber != null && dto.PhoneNumber != user.Phone)
                {
                    // Check uniqueness via Identity - allow if belongs to same user
                    if (identityUser != null)
                    {
                        var existingByPhone = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber, cancellationToken);

                        if (existingByPhone != null && existingByPhone.DomainUserId != userId)
                        {
                            result = (false, false, "Phone number is already in use.");
                            return;
                        }

                        var phoneResult = await _userManager.SetPhoneNumberAsync(identityUser, dto.PhoneNumber);
                        if (!phoneResult.Succeeded)
                        {
                            var errors = string.Join(", ", phoneResult.Errors.Select(e => e.Description));
                            _logger.LogWarning("Failed to set phone for user {UserId}: {Errors}", userId, errors);
                            result = (false, false, "Failed to update phone number.");
                            return;
                        }
                    }

                    user.Phone = dto.PhoneNumber;
                }

                // Email handling: ensure uniqueness and update Identity store atomically
                if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
                {
                    // Check identity store for email; allow if the found identity belongs to current user
                    var existingIdentityUser = await _userManager.FindByEmailAsync(dto.Email);
                    if (existingIdentityUser != null && existingIdentityUser.DomainUserId != userId)
                    {
                        result = (false, false, "Email is already in use.");
                        return;
                    }

                    if (identityUser != null)
                    {
                        // Use proper UserManager APIs to set email and username so normalization occurs
                        var setEmailRes = await _userManager.SetEmailAsync(identityUser, dto.Email);
                        if (!setEmailRes.Succeeded)
                        {
                            var errors = string.Join(", ", setEmailRes.Errors.Select(e => e.Description));
                            _logger.LogWarning("Failed to set email for user {UserId}: {Errors}", userId, errors);
                            result = (false, false, "Failed to update email.");
                            return;
                        }

                        var setUserNameRes = await _userManager.SetUserNameAsync(identityUser, dto.Email);
                        if (!setUserNameRes.Succeeded)
                        {
                            var errors = string.Join(", ", setUserNameRes.Errors.Select(e => e.Description));
                            _logger.LogWarning("Failed to set username for user {UserId}: {Errors}", userId, errors);
                            result = (false, false, "Failed to update username.");
                            return;
                        }

                        // Optionally, when email changes, mark email as unconfirmed
                        if (identityUser.EmailConfirmed)
                        {
                            identityUser.EmailConfirmed = false;
                            var updateRes = await _userManager.UpdateAsync(identityUser);
                            if (!updateRes.Succeeded)
                            {
                                var errors = string.Join(", ", updateRes.Errors.Select(e => e.Description));
                                _logger.LogWarning("Failed to update identity user after email change for {UserId}: {Errors}", userId, errors);
                                result = (false, false, "Failed to update identity after email change.");
                                return;
                            }
                        }

                        emailChanged = true;
                    }

                    user.Email = dto.Email;
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                // Commit transaction only after all updates succeeded
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("User profile updated for DomainUserId={UserId}", userId);
                result = (true, false, "Profile updated successfully.");

                // After commit, send verification email if email changed and identity user exists
                if (emailChanged && identityUser != null)
                {
                    try
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                        var verificationLink = $"http://localhost:44399/verify-email?userId={identityUser.Id}&token={Uri.EscapeDataString(token)}";
                        var sent = await _emailService.SendVerificationEmailAsync(identityUser.Email ?? dto.Email!, verificationLink);
                        if (!sent)
                        {
                            _logger.LogWarning("Failed to send verification email to user {UserId}", userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending verification email after profile update for DomainUserId={UserId}", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for DomainUserId={UserId}", userId);
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch { }

                result = (false, false, "Failed to update profile.");
            }
        });

        return result;
    }

    public async Task<(bool Success, bool NotFound, string Message, string? NewImageUrl)> UploadProfilePictureAsync(
        int userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return (false, true, "User not found.", null);

        try
        {
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            var newImageUrl = await _fileService.UploadFileAsync(file, "images/profiles", allowedExtensions, cancellationToken);

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                _fileService.DeleteFile(user.ProfilePictureUrl);
            }

            user.ProfilePictureUrl = newImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Profile picture updated for DomainUserId={UserId}", userId);

            return (true, false, "Profile picture uploaded successfully.", newImageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload profile picture for DomainUserId={UserId}", userId);
            return (false, false, "An unexpected error occurred while uploading the image.", null);
        }
    }
}