using App.Application.Common.Interfaces;
using App.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Services;

/// <summary>
/// Service for broadcasting real-time activity events to the activity stream via SignalR.
/// </summary>
public class ActivityStreamService : IActivityStreamService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public ActivityStreamService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastActivityAsync(
        ActivityEvent activityEvent,
        CancellationToken cancellationToken = default
    )
    {
        await _hubContext
            .Clients.Group(NotificationHub.ActivityStreamGroup)
            .SendAsync("ActivityReceived", activityEvent, cancellationToken);
    }
}
