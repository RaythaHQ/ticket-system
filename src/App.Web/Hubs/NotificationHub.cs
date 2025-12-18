using App.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Hubs;

/// <summary>
/// SignalR hub for real-time notifications, ticket presence tracking, and activity stream.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ICurrentUser _currentUser;
    private readonly IHubContext<NotificationHub> _hubContext;
    private static readonly Dictionary<string, HashSet<string>> _ticketViewers = new();
    private static readonly Dictionary<string, TicketViewerInfo> _connectionInfo = new();
    private static readonly HashSet<string> _activityStreamSubscribers = new();
    private static readonly Dictionary<Guid, CancellationTokenSource> _pendingOffline = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Grace period in seconds before marking a user as offline.
    /// This prevents flashing when navigating between pages.
    /// </summary>
    private const int OfflineGracePeriodSeconds = 3;

    /// <summary>
    /// SignalR group name for activity stream subscribers.
    /// </summary>
    public const string ActivityStreamGroup = "activity-stream";

    public NotificationHub(ICurrentUser currentUser, IHubContext<NotificationHub> hubContext)
    {
        _currentUser = currentUser;
        _hubContext = hubContext;
    }

    public override async Task OnConnectedAsync()
    {
        if (_currentUser.UserId.HasValue)
        {
            var userId = _currentUser.UserId.Value.Guid;

            // Add user to their personal notification group
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));

            bool isNewUser;
            lock (_lock)
            {
                // Cancel any pending offline notification for this user
                if (_pendingOffline.TryGetValue(userId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _pendingOffline.Remove(userId);
                }

                // Check if this user already has connections (not a new online user)
                isNewUser = !_connectionInfo.Values.Any(c => c.UserId == userId);

                _connectionInfo[Context.ConnectionId] = new TicketViewerInfo
                {
                    UserId = userId,
                    UserName = _currentUser.FullName ?? "Unknown",
                    ConnectedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsIdle = false,
                };
            }

            // Notify activity stream subscribers that a new user came online
            if (isNewUser)
            {
                await NotifyOnlineUsersChangedAsync();
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        TicketViewerInfo? viewerInfo;
        bool wasLastConnection = false;
        Guid? userId = null;

        lock (_lock)
        {
            _connectionInfo.TryGetValue(Context.ConnectionId, out viewerInfo);
            _connectionInfo.Remove(Context.ConnectionId);
            _activityStreamSubscribers.Remove(Context.ConnectionId);

            // Check if this was the user's last connection
            if (viewerInfo != null)
            {
                userId = viewerInfo.UserId;
                wasLastConnection = !_connectionInfo.Values.Any(c => c.UserId == viewerInfo.UserId);
            }

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

        // Schedule delayed offline notification (grace period for page navigation)
        if (wasLastConnection && userId.HasValue)
        {
            var cts = new CancellationTokenSource();
            lock (_lock)
            {
                // Cancel any existing pending offline for this user
                if (_pendingOffline.TryGetValue(userId.Value, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                }
                _pendingOffline[userId.Value] = cts;
            }

            // Schedule the offline notification after grace period
            // Capture hubContext for use in background task (Hub's Clients property won't work)
            var hubContext = _hubContext;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(OfflineGracePeriodSeconds), cts.Token);

                    List<OnlineUser>? onlineUsers = null;
                    lock (_lock)
                    {
                        // Check if user is still offline after grace period
                        if (
                            _pendingOffline.TryGetValue(userId.Value, out var pendingCts)
                            && pendingCts == cts
                        )
                        {
                            _pendingOffline.Remove(userId.Value);
                            // Verify user hasn't reconnected
                            if (!_connectionInfo.Values.Any(c => c.UserId == userId.Value))
                            {
                                // Get online users list while holding lock
                                onlineUsers = _connectionInfo
                                    .Values.GroupBy(c => c.UserId)
                                    .Select(g =>
                                    {
                                        var allIdle = g.All(c => c.IsIdle);
                                        var mostRecent = g.OrderByDescending(c => c.LastActivityAt)
                                            .First();
                                        return new OnlineUser
                                        {
                                            UserId = mostRecent.UserId,
                                            UserName = mostRecent.UserName,
                                            IsIdle = allIdle,
                                            LastActivityAt = mostRecent.LastActivityAt,
                                        };
                                    })
                                    .OrderBy(u => u.IsIdle)
                                    .ThenBy(u => u.UserName)
                                    .ToList();
                            }
                        }
                    }

                    if (onlineUsers != null)
                    {
                        // Use hubContext for background task (not Clients property)
                        await hubContext
                            .Clients.Group(ActivityStreamGroup)
                            .SendAsync("OnlineUsersChanged", onlineUsers);
                    }
                }
                catch (TaskCanceledException)
                {
                    // User reconnected, no notification needed
                }
                finally
                {
                    cts.Dispose();
                }
            });
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

    #region Activity Stream

    /// <summary>
    /// Called when a user navigates to the Activity Log page to subscribe to live updates.
    /// </summary>
    public async Task JoinActivityStream()
    {
        lock (_lock)
        {
            _activityStreamSubscribers.Add(Context.ConnectionId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ActivityStreamGroup);

        // Send current online users to the newly joined subscriber
        var onlineUsers = GetOnlineUsersInternal();
        await Clients.Caller.SendAsync("OnlineUsersChanged", onlineUsers);
    }

    /// <summary>
    /// Called when a user leaves the Activity Log page.
    /// </summary>
    public async Task LeaveActivityStream()
    {
        lock (_lock)
        {
            _activityStreamSubscribers.Remove(Context.ConnectionId);
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ActivityStreamGroup);
    }

    /// <summary>
    /// Called by the client to report user activity (mouse/keyboard).
    /// This resets the idle timer for the user.
    /// </summary>
    public async Task ReportActivity()
    {
        bool wasIdle = false;

        lock (_lock)
        {
            if (_connectionInfo.TryGetValue(Context.ConnectionId, out var info))
            {
                wasIdle = info.IsIdle;
                info.LastActivityAt = DateTime.UtcNow;
                info.IsIdle = false;
            }
        }

        // If user was idle and is now active, notify others
        if (wasIdle)
        {
            await NotifyOnlineUsersChangedAsync();
        }
    }

    /// <summary>
    /// Called by the client when the user goes idle (no activity for a while).
    /// </summary>
    public async Task GoIdle()
    {
        bool wasActive = false;

        lock (_lock)
        {
            if (_connectionInfo.TryGetValue(Context.ConnectionId, out var info))
            {
                wasActive = !info.IsIdle;
                info.IsIdle = true;
            }
        }

        // If user was active and is now idle, notify others
        if (wasActive)
        {
            await NotifyOnlineUsersChangedAsync();
        }
    }

    /// <summary>
    /// Gets all currently online users with their status.
    /// </summary>
    public Task<List<OnlineUser>> GetOnlineUsers()
    {
        return Task.FromResult(GetOnlineUsersInternal());
    }

    private List<OnlineUser> GetOnlineUsersInternal()
    {
        lock (_lock)
        {
            // Group by user ID and get the most recent connection per user
            return _connectionInfo
                .Values.GroupBy(c => c.UserId)
                .Select(g =>
                {
                    // A user is idle only if ALL their connections are idle
                    var allIdle = g.All(c => c.IsIdle);
                    var mostRecent = g.OrderByDescending(c => c.LastActivityAt).First();

                    return new OnlineUser
                    {
                        UserId = mostRecent.UserId,
                        UserName = mostRecent.UserName,
                        IsIdle = allIdle,
                        LastActivityAt = mostRecent.LastActivityAt,
                    };
                })
                .OrderBy(u => u.IsIdle)
                .ThenBy(u => u.UserName)
                .ToList();
        }
    }

    private async Task NotifyOnlineUsersChangedAsync()
    {
        var onlineUsers = GetOnlineUsersInternal();
        await Clients.Group(ActivityStreamGroup).SendAsync("OnlineUsersChanged", onlineUsers);
    }

    #endregion
}

/// <summary>
/// Information about a user viewing a ticket.
/// </summary>
public class TicketViewerInfo
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsIdle { get; set; }
}

/// <summary>
/// Information about an online user for the activity stream.
/// </summary>
public class OnlineUser
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsIdle { get; set; }
    public DateTime LastActivityAt { get; set; }
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
