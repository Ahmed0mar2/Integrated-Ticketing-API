using GP.Application.DTOs.Notifications;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Infrastructure.Data;
using GP.Infrastructure.Hubs;
using GP.Infrastructure.Interfaces;
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
            string title,
            string message,
            string type,
            CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.User(userId.ToString())
                .ReceiveNotification(title, message, type);
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
    }
}