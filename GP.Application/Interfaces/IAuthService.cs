using GP.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, AuthResponse? Data)> RegisterAsync(
            RegisterRequest request,
            string? ipAddress = null);

        Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(
            LoginRequest request,
            string? ipAddress = null);

        Task<(bool Success, string Message, AuthResponse? Data)> RefreshTokenAsync(
            string token,
            string? ipAddress = null);

        Task<(bool Success, string Message)> RevokeTokenAsync(
            string token,
            string? ipAddress = null);

        Task<(bool Success, string Message)> RevokeAllUserTokensAsync(int userId);

        // Email verification methods 
        Task<(bool Success, string Message)> SendVerificationEmailAsync(string email);
        Task<(bool Success, string Message)> VerifyEmailAsync(string userId, string token);
        Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
        Task<(bool Success, string Message)> ResetPasswordAsync(
            string email,
            string token,
            string newPassword);
        Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);


    }
}
