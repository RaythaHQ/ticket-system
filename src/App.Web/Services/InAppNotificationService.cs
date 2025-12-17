using App.Application.Common.Interfaces;
using App.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Services;

/// <summary>
/// Service for sending real-time in-app notifications via SignalR.
/// </summary>
public class InAppNotificationService : IInAppNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IAppDbContext _db;

    public InAppNotificationService(IHubContext<NotificationHub> hubContext, IAppDbContext db)
    {
        _hubContext = hubContext;
        _db = db;
    }

    public async Task SendToUserAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? url = null,
        long? ticketId = null,
        CancellationToken cancellationToken = default
    )
    {
        // Check if user wants sound
        var user = await _db
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var notification = new InAppNotification
        {
            Type = type,
            Title = title,
            Message = message,
            Url = url,
            TicketId = ticketId,
            PlaySound = user?.PlaySoundOnNotification ?? true,
        };

        await _hubContext
            .Clients.Group($"user:{userId}")
            .SendAsync("ReceiveNotification", notification, cancellationToken);
    }

    public async Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string message,
        string? url = null,
        long? ticketId = null,
        CancellationToken cancellationToken = default
    )
    {
        var userIdList = userIds.ToList();
        if (!userIdList.Any())
            return;

        // Get sound preferences for all users
        var users = await _db
            .Users.AsNoTracking()
            .Where(u => userIdList.Contains(u.Id))
            .Select(u => new { u.Id, u.PlaySoundOnNotification })
            .ToListAsync(cancellationToken);

        // Send individual notifications with correct sound preference
        foreach (var user in users)
        {
            var notification = new InAppNotification
            {
                Type = type,
                Title = title,
                Message = message,
                Url = url,
                TicketId = ticketId,
                PlaySound = user.PlaySoundOnNotification,
            };

            await _hubContext
                .Clients.Group($"user:{user.Id}")
                .SendAsync("ReceiveNotification", notification, cancellationToken);
        }
    }
}
