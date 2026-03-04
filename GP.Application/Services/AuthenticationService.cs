using GP.Application.DTOs.Auth;
using GP.Application.Settings;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using GP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GP.Application.Interfaces;
using GP.Application.Common;

namespace GP.Application.Services
{
    public class AuthenticationService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
        }

        public async Task<(bool Success, string Message, AuthResponse? Data)> RegisterAsync(
            RegisterRequest request,
            string? ipAddress = null)
        {
            if (request.Password != request.ConfirmPassword)
            {
                return (false, "Passwords do not match", null);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            (bool Success, string Message, AuthResponse? Data) result = default;

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // VALIDATE Email uniqueness
                    if (await _userManager.FindByEmailAsync(request.Email) != null)
                    {
                        result = (false, "Email already registered", null);
                        return;
                    }

                    // VALIDATE National ID number uniqueness
                    if (!string.IsNullOrWhiteSpace(request.NationalIdNumber))
                    {
                        if (await _context.Users.AnyAsync(u =>
                            u.NationalIdNumber == request.NationalIdNumber))
                        {
                            result = (false, "National ID number already registered", null);
                            return;
                        }
                    }

                    // VALIDATE Country exists
                    var country = await _context.Countries
                        .FirstOrDefaultAsync(c => c.CountryCode == request.CountryCode);

                    if (country == null)
                    {
                        result = (false, "Invalid country", null);
                        return;
                    }

                    var applicationUser = new ApplicationUser
                    {
                        UserName = request.Email,
                        Email = request.Email,
                        PhoneNumber = request.PhoneNumber,
                        EmailConfirmed = false
                    };

                    var identityResult =
                        await _userManager.CreateAsync(applicationUser, request.Password);

                    if (!identityResult.Succeeded)
                    {
                        result = (
                            false,
                            string.Join(", ", identityResult.Errors.Select(e => e.Description)),
                            null
                        );
                        return;
                    }

                    // Assign role
                    var roleResult = await _userManager.AddToRoleAsync(applicationUser, Roles.User);
                    if (!roleResult.Succeeded)
                    {
                        result = (
                            false,
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)),
                            null
                        );
                        return;
                    }

                    var domainUser = new User
                    {
                        Email = request.Email,
                        Phone = request.PhoneNumber,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        FamilyName = request.FamilyName,
                        Gender = request.Gender,
                        DateOfBirth = request.DateOfBirth,
                        NationalIdNumber = request.NationalIdNumber,
                        IsNationalIdVerified = !string.IsNullOrWhiteSpace(request.NationalIdNumber),
                        CountryId = country.CountryId,
                        Nationality = country.NationalityName, 
                        Country = country, 
                        TotalTripsCount = 0,
                        TotalDistanceTraveled = 0,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(domainUser);
                    await _context.SaveChangesAsync();

                    applicationUser.DomainUserId = domainUser.UserId;
                    applicationUser.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(applicationUser);

                    var accessToken = await GenerateAccessTokenAsync(applicationUser, domainUser);
                    var refreshToken = await GenerateRefreshTokenAsync(
                        applicationUser.Id,
                        ipAddress,
                        request.Email);

                    await transaction.CommitAsync();

                    result = (
                        true,
                        "Registration successful",
                        new AuthResponse
                        {
                            AccessToken = accessToken.Token,
                            RefreshToken = refreshToken,
                            ExpiresAt = accessToken.ExpiresAt,
                            User = MapToUserDto(domainUser)
                        }
                    );
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    result = (false, $"Registration failed: {ex.Message}", null);
                }
            });

            return result;
        }



        public async Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(
            LoginRequest request,
            string? ipAddress = null)
        {
            try
            {
                // Find user by email
                var applicationUser = await _userManager.FindByEmailAsync(request.Email);

                if (applicationUser == null)
                {
                    return (false, "Invalid email or password", null);
                }

                // Check if user is active
                if (!applicationUser.IsActive)
                {
                    return (false, "Account is deactivated", null);
                }

                // Verify password
                var isValidPassword = await _userManager.CheckPasswordAsync(applicationUser, request.Password);

                if (!isValidPassword)
                {
                    return (false, "Invalid email or password", null);
                }

                // Get domain user 
                var domainUser = await _context.Users
                    .Include(u => u.Country)
                    .FirstOrDefaultAsync(u => u.UserId == applicationUser.DomainUserId);

                if (domainUser == null)
                {
                    return (false, "User profile not found", null);
                }

                // Update last login
                applicationUser.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(applicationUser);

                // Generate tokens
                var accessToken = await GenerateAccessTokenAsync(applicationUser, domainUser);
                var refreshToken = await GenerateRefreshTokenAsync(
                    applicationUser.Id,
                    ipAddress,
                    request.DeviceInfo);

                var authResponse = new AuthResponse
                {
                    AccessToken = accessToken.Token,
                    RefreshToken = refreshToken,
                    ExpiresAt = accessToken.ExpiresAt,
                    User = MapToUserDto(domainUser)
                };

                return (true, "Login successful", authResponse);
            }
            catch (Exception ex)
            {
                return (false, $"Login failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, AuthResponse? Data)> RefreshTokenAsync(
            string token,
            string? ipAddress = null)
        {
            try
            {
                var tokenHash = ComputeSha256Hash(token);

                // Find refresh token
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.ApplicationUser)
                    .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

                if (refreshToken == null)
                {
                    return (false, "Invalid token", null);
                }

                // Validate token
                if (!refreshToken.IsActive)
                {
                    return (false, "Token is expired or revoked", null);
                }

                // Get domain user
                var domainUser = await _context.Users
                    .Include(u => u.Country)
                    .FirstOrDefaultAsync(u => u.UserId == refreshToken.ApplicationUser.DomainUserId);

                if (domainUser == null)
                {
                    return (false, "User not found", null);
                }

                // Revoke old refresh token
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;

                // Generate new tokens
                var accessToken = await GenerateAccessTokenAsync(refreshToken.ApplicationUser, domainUser);
                var newRefreshToken = await GenerateRefreshTokenAsync(
                    refreshToken.ApplicationUserId,
                    ipAddress);

                await _context.SaveChangesAsync();

                var authResponse = new AuthResponse
                {
                    AccessToken = accessToken.Token,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = accessToken.ExpiresAt,
                    User = MapToUserDto(domainUser)
                };

                return (true, "Token refreshed successfully", authResponse);
            }
            catch (Exception ex)
            {
                return (false, $"Token refresh failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> RevokeTokenAsync(
            string token,
            string? ipAddress = null)
        {
            try
            {
                var tokenHash = ComputeSha256Hash(token);

                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

                if (refreshToken == null || !refreshToken.IsActive)
                {
                    return (false, "Invalid token");
                }

                // Revoke token
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;

                await _context.SaveChangesAsync();

                return (true, "Token revoked successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Token revocation failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RevokeAllUserTokensAsync(int userId)
        {
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.ApplicationUserId == userId && rt.IsActive)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return (true, $"Revoked {tokens.Count} token(s)");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to revoke tokens: {ex.Message}");
            }
        }

        // PRIVATE HELPER METHODS

        private async Task<(string Token, DateTime ExpiresAt)> GenerateAccessTokenAsync(ApplicationUser applicationUser,User domainUser)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

            // Get user roles
            var roles = await _userManager.GetRolesAsync(applicationUser);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, applicationUser.Id.ToString()),
                new(ClaimTypes.Email, applicationUser.Email!),
                new(ClaimTypes.Name, $"{domainUser.FirstName} {domainUser.LastName}"),
                new("domain_user_id", domainUser.UserId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return (tokenString, expiresAt);
        }

        private async Task<string> GenerateRefreshTokenAsync(
            int userId,
            string? ipAddress = null,
            string? deviceInfo = null)
        {
            // Generate random token
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);

            // Ensure token is unique
            var tokenHash = ComputeSha256Hash(token);
            var exists = await _context.RefreshTokens.AnyAsync(rt => rt.TokenHash == tokenHash);

            if (exists)
            {
                return await GenerateRefreshTokenAsync(userId, ipAddress, deviceInfo);
            }

            // Create refresh token record
            var refreshToken = new RefreshToken
            {
                ApplicationUserId = userId,
                TokenHash = tokenHash,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return token;
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256Hash = SHA256.Create();
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();

            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.FamilyName} {user.LastName}",
                PhoneNumber = user.Phone,
                Gender = user.Gender.ToString(),
                CountryCode = user.Country?.CountryCode ?? string.Empty,
                CountryName = user.Country?.NationalityName ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public async Task<(bool Success, string Message)> SendVerificationEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return (false, "User not found");
                }

                if (user.EmailConfirmed)
                {
                    return (false, "Email already verified");
                }

                // Generate token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                //Todo: Create verification link
                var verificationLink = $"http://localhost:44399/verify-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

                // Send email
                var emailSent = await _emailService.SendVerificationEmailAsync(user.Email!, verificationLink);

                if (!emailSent)
                {
                    return (false, "Failed to send verification email");
                }

                return (true, "Verification email sent successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to send verification email: {ex.Message}");
            }
        }


        public async Task<(bool Success, string Message)> VerifyEmailAsync(string userId, string token)
        {
            try
            {
                if (!int.TryParse(userId, out var id))
                {
                    return (false, "Invalid user ID");
                }

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return (false, "User not found");
                }

                if (user.EmailConfirmed)
                {
                    return (false, "Email already verified");
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (!result.Succeeded)
                {
                    return (false, "Email verification failed");
                }

                return (true, "Email verified successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Email verification failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return (true, "If your email is registered, you will receive a password reset link");
                }

                // Generate token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                //Todo: Create reset link
                var resetLink = $"http://localhost:44399/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

                // Send email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);

                if (!emailSent)
                {
                    return (false, "Failed to send password reset email");
                }

                return (true, "If your email is registered, you will receive a password reset link");
            }
            catch (Exception ex)
            {
                return (false, $"Password reset request failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(
            string email,
            string token,
            string newPassword)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return (false, "Invalid request");
                }

                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return (true, "Password reset successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Password reset failed: {ex.Message}");
            }
        }
        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
        {
            try
            {
                var applicationUser = await _userManager.FindByIdAsync(userId.ToString());

                if (applicationUser == null)
                {
                    return (false, "User not found");
                }

                var result = await _userManager.ChangePasswordAsync(
                    applicationUser,
                    currentPassword,
                    newPassword);

                if (!result.Succeeded)
                {
                    return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Password change failed: {ex.Message}");
            }
        }
    }
}
