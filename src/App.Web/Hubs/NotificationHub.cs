using App.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Hubs;

/// <summary>
/// SignalR hub for real-time notifications and ticket presence tracking.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ICurrentUser _currentUser;
    private static readonly Dictionary<string, HashSet<string>> _ticketViewers = new();
    private static readonly Dictionary<string, TicketViewerInfo> _connectionInfo = new();
    private static readonly object _lock = new();

    public NotificationHub(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override async Task OnConnectedAsync()
    {
        if (_currentUser.UserId.HasValue)
        {
            // Add user to their personal notification group
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetUserGroup(_currentUser.UserId.Value.Guid)
            );

            // Store connection info
            lock (_lock)
            {
                _connectionInfo[Context.ConnectionId] = new TicketViewerInfo
                {
                    UserId = _currentUser.UserId.Value.Guid,
                    UserName = _currentUser.FullName ?? "Unknown",
                    ConnectedAt = DateTime.UtcNow,
                };
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        TicketViewerInfo? viewerInfo;
        lock (_lock)
        {
            _connectionInfo.TryGetValue(Context.ConnectionId, out viewerInfo);
            _connectionInfo.Remove(Context.ConnectionId);

            // Remove from any ticket viewing groups
            foreach (var ticketGroup in _ticketViewers)
            {
                if (ticketGroup.Value.Remove(Context.ConnectionId))
                {
                    // Notify others that this user left
                    _ = NotifyTicketViewersChangedAsync(ticketGroup.Key);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called when a user starts viewing a ticket.
    /// </summary>
    public async Task JoinTicketView(long ticketId)
    {
        var ticketGroup = GetTicketGroup(ticketId);

        lock (_lock)
        {
            if (!_ticketViewers.ContainsKey(ticketGroup))
            {
                _ticketViewers[ticketGroup] = new HashSet<string>();
            }

            _ticketViewers[ticketGroup].Add(Context.ConnectionId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ticketGroup);
        await NotifyTicketViewersChangedAsync(ticketGroup);
    }

    /// <summary>
    /// Called when a user leaves a ticket view.
    /// </summary>
    public async Task LeaveTicketView(long ticketId)
    {
        var ticketGroup = GetTicketGroup(ticketId);

        lock (_lock)
        {
            if (_ticketViewers.TryGetValue(ticketGroup, out var viewers))
            {
                viewers.Remove(Context.ConnectionId);
                if (viewers.Count == 0)
                {
                    _ticketViewers.Remove(ticketGroup);
                }
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ticketGroup);
        await NotifyTicketViewersChangedAsync(ticketGroup);
    }

    /// <summary>
    /// Gets the current viewers of a ticket (excluding the caller).
    /// </summary>
    public Task<List<TicketViewerInfo>> GetTicketViewers(long ticketId)
    {
        var ticketGroup = GetTicketGroup(ticketId);
        var viewers = new List<TicketViewerInfo>();

        lock (_lock)
        {
            if (_ticketViewers.TryGetValue(ticketGroup, out var connectionIds))
            {
                foreach (var connId in connectionIds)
                {
                    if (
                        connId != Context.ConnectionId
                        && _connectionInfo.TryGetValue(connId, out var info)
                    )
                    {
                        viewers.Add(info);
                    }
                }
            }
        }

        // Deduplicate by user ID (same user might have multiple tabs)
        var uniqueViewers = viewers.GroupBy(v => v.UserId).Select(g => g.First()).ToList();

        return Task.FromResult(uniqueViewers);
    }

    private async Task NotifyTicketViewersChangedAsync(string ticketGroup)
    {
        var viewers = new List<TicketViewerInfo>();

        lock (_lock)
        {
            if (_ticketViewers.TryGetValue(ticketGroup, out var connectionIds))
            {
                foreach (var connId in connectionIds)
                {
                    if (_connectionInfo.TryGetValue(connId, out var info))
                    {
                        viewers.Add(info);
                    }
                }
            }
        }

        // Deduplicate by user ID
        var uniqueViewers = viewers.GroupBy(v => v.UserId).Select(g => g.First()).ToList();

        await Clients.Group(ticketGroup).SendAsync("TicketViewersChanged", uniqueViewers);
    }

    private static string GetUserGroup(Guid userId) => $"user:{userId}";

    private static string GetTicketGroup(long ticketId) => $"ticket:{ticketId}";
}

/// <summary>
/// Information about a user viewing a ticket.
/// </summary>
public class TicketViewerInfo
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
}

/// <summary>
/// Represents an in-app notification to be sent via SignalR.
/// </summary>
public class InAppNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Url { get; set; }
    public long? TicketId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool PlaySound { get; set; }
}
