using Microsoft.AspNetCore.SignalR;

namespace GP.Infrastructure.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("domain_user_id")?.Value;
    }
}
