namespace GP.Infrastructure.Interfaces;

public interface INotificationClient
{
    Task ReceiveNotification(string title, string message, string type);
}
