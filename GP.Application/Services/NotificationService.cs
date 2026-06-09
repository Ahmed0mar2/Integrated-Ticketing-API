using GP.Application.DTOs.Notifications;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Infrastructure.Data;
using GP.Infrastructure.Hubs;
using GP.Infrastructure.Interfaces;
using FcmMessage = FirebaseAdmin.Messaging.Message;
using FcmNotification = FirebaseAdmin.Messaging.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GP.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public NotificationService(
            ApplicationDbContext dbContext,
            IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(
            int userId,
            string titleEn,
            string messageEn,
            string titleAr,
            string messageAr,
            string type,
            CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = titleEn,
                Message = messageEn,
                TitleAr = titleAr,
                MessageAr = messageAr,
                Type = type,
                IsRead = false
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.User(userId.ToString())
                .ReceiveNotification(titleEn, messageEn, type);

            var preferredLanguage = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.PreferredLanguage)
                .FirstOrDefaultAsync(cancellationToken);

            var isArabic = preferredLanguage == "ar";

            var deviceTokens = await _dbContext.UserDeviceTokens
                .Where(t => t.UserId == userId)
                .ToListAsync(cancellationToken);

            if (deviceTokens.Count == 0)
            {
                return;
            }

            var tokensToRemove = new List<UserDeviceToken>();

            foreach (var deviceToken in deviceTokens)
            {
                var fcmMessage = new FcmMessage
                {
                    Token = deviceToken.FcmToken,
                    Notification = new FcmNotification
                    {
                        Title = isArabic ? titleAr : titleEn,
                        Body = isArabic ? messageAr : messageEn
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["type"] = type,
                        ["title_ar"] = titleAr ?? string.Empty,
                        ["body_ar"] = messageAr ?? string.Empty
                    }
                };

                try
                {
                    await FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance
                        .SendAsync(fcmMessage, cancellationToken: cancellationToken);
                }
                catch (FirebaseAdmin.Messaging.FirebaseMessagingException ex) when (
                    ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered ||
                    ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.InvalidArgument)
                {
                    tokensToRemove.Add(deviceToken);
                }
            }

            if (tokensToRemove.Count > 0)
            {
                _dbContext.UserDeviceTokens.RemoveRange(tokensToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(
            int userId,
            int limit = 50,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
            {
                limit = 50;
            }

            return await _dbContext.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    TitleAr = n.TitleAr,
                    MessageAr = n.MessageAr,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
        {
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

            if (notification == null || notification.IsRead)
            {
                return;
            }

            notification.IsRead = true;
            notification.UpdatedAt = Common.AppTime.GetScheduleNow();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var unreadNotifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            if (unreadNotifications.Count == 0)
            {
                return;
            }

            var now = Common.AppTime.GetScheduleNow();
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        public async Task<bool> DeleteNotificationAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

            if (notification == null)
            {
                return false;
            }

            _dbContext.Notifications.Remove(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}