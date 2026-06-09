using GP.Application.DTOs.Notifications;

namespace GP.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(
            int userId,
            string titleEn,
            string messageEn,
            string titleAr,
            string messageAr,
            string type,
            CancellationToken cancellationToken = default);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int limit = 50, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteNotificationAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
    }
}