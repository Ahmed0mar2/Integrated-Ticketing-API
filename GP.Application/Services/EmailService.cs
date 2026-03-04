using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Services
{
    using GP.Application.Interfaces;
    using GP.Application.Settings;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Mail;

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network, 
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendVerificationEmailAsync(string toEmail, string verificationLink)
        {
            var subject = "Verify Your Email - Transport Booking";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome to Transport Booking!</h2>
                <p>Please verify your email address by clicking the button below:</p>
                <a href='{verificationLink}' 
                   style='display: inline-block; padding: 12px 24px; background-color: #007bff; 
                          color: white; text-decoration: none; border-radius: 4px; margin: 20px 0;'>
                    Verify Email
                </a>
                <p>Or copy and paste this link into your browser:</p>
                <p>{verificationLink}</p>
                <p>This link will expire in 24 hours.</p>
                <hr>
                <p style='color: #666; font-size: 12px;'>
                    If you didn't create an account, please ignore this email.
                </p>
            </body>
            </html>
        ";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Reset Your Password - Transport Booking";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Password Reset Request</h2>
                <p>We received a request to reset your password. Click the button below to proceed:</p>
                <a href='{resetLink}' 
                   style='display: inline-block; padding: 12px 24px; background-color: #dc3545; 
                          color: white; text-decoration: none; border-radius: 4px; margin: 20px 0;'>
                    Reset Password
                </a>
                <p>Or copy and paste this link into your browser:</p>
                <p>{resetLink}</p>
                <p>This link will expire in 1 hour.</p>
                <hr>
                <p style='color: #666; font-size: 12px;'>
                    If you didn't request this, please ignore this email. Your password will remain unchanged.
                </p>
            </body>
            </html>
        ";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}
