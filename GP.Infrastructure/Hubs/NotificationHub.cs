using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using GP.Infrastructure.Interfaces;

namespace GP.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} connected to NotificationHub. ConnectionId: {ConnectionId}",
            userId ?? "Unknown", Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from NotificationHub. ConnectionId: {ConnectionId}",
            userId ?? "Unknown", Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with exception", userId ?? "Unknown");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
