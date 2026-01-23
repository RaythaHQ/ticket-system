using App.Application.Common.Interfaces;
using App.Domain.Entities;
using App.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Services;

/// <summary>
/// Service for sending real-time in-app notifications via SignalR.
/// Also records all notifications to the database for the notification center.
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
        // Record notification to database (always, regardless of delivery preferences)
        await RecordNotificationAsync(userId, type, title, message, url, ticketId, cancellationToken);

        // Broadcast unread count update
        await BroadcastUnreadCountUpdateAsync(userId, cancellationToken);

        // Check if user wants in-app delivery
        var user = await _db
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        // Check notification preferences
        var preference = await _db
            .NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.StaffAdminId == userId && p.EventType == type.ToLower(), cancellationToken);

        // Default to enabled if no preference exists
        var inAppEnabled = preference?.InAppEnabled ?? true;

        if (inAppEnabled)
        {
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

        // Record notification to database for each user (always, regardless of delivery preferences)
        foreach (var userId in userIdList)
        {
            await RecordNotificationAsync(userId, type, title, message, url, ticketId, cancellationToken);
            await BroadcastUnreadCountUpdateAsync(userId, cancellationToken);
        }

        // Get user preferences and settings
        var users = await _db
            .Users.AsNoTracking()
            .Where(u => userIdList.Contains(u.Id))
            .Select(u => new { u.Id, u.PlaySoundOnNotification })
            .ToListAsync(cancellationToken);

        var preferences = await _db
            .NotificationPreferences.AsNoTracking()
            .Where(p => userIdList.Contains(p.StaffAdminId) && p.EventType == type.ToLower())
            .ToListAsync(cancellationToken);

        // Send individual notifications with correct sound preference (only if in-app enabled)
        foreach (var user in users)
        {
            var preference = preferences.FirstOrDefault(p => p.StaffAdminId == user.Id);
            var inAppEnabled = preference?.InAppEnabled ?? true;

            if (inAppEnabled)
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

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db
            .Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task BroadcastUnreadCountUpdateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await GetUnreadCountAsync(userId, cancellationToken);
        var display = count >= 100 ? "99+" : count.ToString();

        await _hubContext
            .Clients.Group($"user:{userId}")
            .SendAsync("ReceiveUnreadCountUpdate", new { count, display }, cancellationToken);
    }

    private async Task RecordNotificationAsync(
        Guid recipientUserId,
        string type,
        string title,
        string message,
        string? url,
        long? ticketId,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            EventType = type.ToLower(),
            Title = title,
            Message = message,
            Url = url,
            TicketId = ticketId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
