using App.Application.Common.Interfaces;
using App.Domain.Events;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Broadcasts ticket created events to the activity stream.
/// </summary>
public class TicketCreatedEventHandler_ActivityStream : INotificationHandler<TicketCreatedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketCreatedEventHandler_ActivityStream> _logger;

    public TicketCreatedEventHandler_ActivityStream(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketCreatedEventHandler_ActivityStream> logger
    )
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketCreatedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;
            var actorName = _currentUser.FullName ?? "Unknown";

            await _activityStream.BroadcastActivityAsync(
                new ActivityEvent
                {
                    Type = ActivityEventType.TicketCreated,
                    Message = $"Ticket #{ticket.Id} created",
                    Details = ticket.Title,
                    ActorId = _currentUser.UserIdAsGuid,
                    ActorName = actorName,
                    TicketId = ticket.Id,
                    ContactId = ticket.ContactId,
                    Url = _urlBuilder.StaffTicketUrl(ticket.Id),
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ticket created activity");
        }
    }
}

/// <summary>
/// Broadcasts ticket assigned events to the activity stream.
/// </summary>
public class TicketAssignedEventHandler_ActivityStream : INotificationHandler<TicketAssignedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketAssignedEventHandler_ActivityStream> _logger;

    public TicketAssignedEventHandler_ActivityStream(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketAssignedEventHandler_ActivityStream> logger
    )
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketAssignedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;
            var actorName = _currentUser.FullName ?? "Unknown";

            string details;
            if (notification.NewAssigneeId.HasValue)
            {
                var assignee = await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.Id == notification.NewAssigneeId.Value,
                        cancellationToken
                    );
                details = $"Assigned to {assignee?.FullName ?? "Unknown"}";
            }
            else if (notification.NewTeamId.HasValue)
            {
                var team = await _db
                    .Teams.AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.Id == notification.NewTeamId.Value,
                        cancellationToken
                    );
                details = $"Assigned to team {team?.Name ?? "Unknown"}";
            }
            else
            {
                details = "Unassigned";
            }

            await _activityStream.BroadcastActivityAsync(
                new ActivityEvent
                {
                    Type = ActivityEventType.TicketAssigned,
                    Message = $"Ticket #{ticket.Id} assignment changed",
                    Details = details,
                    ActorId = _currentUser.UserIdAsGuid,
                    ActorName = actorName,
                    TicketId = ticket.Id,
                    ContactId = ticket.ContactId,
                    Url = _urlBuilder.StaffTicketUrl(ticket.Id),
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ticket assigned activity");
        }
    }
}

/// <summary>
/// Broadcasts ticket status changed events to the activity stream.
/// </summary>
public class TicketStatusChangedEventHandler_ActivityStream
    : INotificationHandler<TicketStatusChangedEvent>
{
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketStatusChangedEventHandler_ActivityStream> _logger;

    public TicketStatusChangedEventHandler_ActivityStream(
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketStatusChangedEventHandler_ActivityStream> logger
    )
    {
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketStatusChangedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;
            var actorName = _currentUser.FullName ?? "Unknown";

            await _activityStream.BroadcastActivityAsync(
                new ActivityEvent
                {
                    Type = ActivityEventType.TicketStatusChanged,
                    Message = $"Ticket #{ticket.Id} status changed",
                    Details = $"{notification.OldStatus} → {notification.NewStatus}",
                    ActorId = _currentUser.UserIdAsGuid,
                    ActorName = actorName,
                    TicketId = ticket.Id,
                    ContactId = ticket.ContactId,
                    Url = _urlBuilder.StaffTicketUrl(ticket.Id),
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ticket status changed activity");
        }
    }
}

/// <summary>
/// Broadcasts ticket comment added events to the activity stream.
/// </summary>
public class TicketCommentAddedEventHandler_ActivityStream
    : INotificationHandler<TicketCommentAddedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ILogger<TicketCommentAddedEventHandler_ActivityStream> _logger;

    public TicketCommentAddedEventHandler_ActivityStream(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ILogger<TicketCommentAddedEventHandler_ActivityStream> logger
    )
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketCommentAddedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;
            var comment = notification.Comment;

            var commenter = await _db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == comment.AuthorStaffId, cancellationToken);

            var truncatedBody =
                comment.Body.Length > 80 ? comment.Body.Substring(0, 80) + "..." : comment.Body;

            await _activityStream.BroadcastActivityAsync(
                new ActivityEvent
                {
                    Type = ActivityEventType.CommentAdded,
                    Message = $"Comment added to #{ticket.Id}",
                    Details = truncatedBody,
                    ActorId = comment.AuthorStaffId,
                    ActorName = commenter?.FullName ?? "Unknown",
                    TicketId = ticket.Id,
                    ContactId = ticket.ContactId,
                    Url = _urlBuilder.StaffTicketUrl(ticket.Id),
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast comment added activity");
        }
    }
}

/// <summary>
/// Broadcasts ticket closed events to the activity stream.
/// </summary>
public class TicketClosedEventHandler_ActivityStream : INotificationHandler<TicketClosedEvent>
{
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketClosedEventHandler_ActivityStream> _logger;

    public TicketClosedEventHandler_ActivityStream(
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketClosedEventHandler_ActivityStream> logger
    )
    {
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketClosedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;
            var actorName = _currentUser.FullName ?? "Unknown";

            await _activityStream.BroadcastActivityAsync(
                new ActivityEvent
                {
                    Type = ActivityEventType.TicketClosed,
                    Message = $"Ticket #{ticket.Id} closed",
                    Details = ticket.Title,
                    ActorId = _currentUser.UserIdAsGuid,
                    ActorName = actorName,
                    TicketId = ticket.Id,
                    ContactId = ticket.ContactId,
                    Url = _urlBuilder.StaffTicketUrl(ticket.Id),
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ticket closed activity");
        }
    }
}

/// <summary>
/// Broadcasts ticket reopened events to the activity stream.
/// </summary>
public class TicketReopenedEventHandler_ActivityStream : INotificationHandler<TicketReopenedEvent>
{
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketReopenedEventHandler_ActivityStream> _logger;

    public TicketReopenedEventHandler_ActivityStream(
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketReopenedEventHandler_ActivityStream> logger
    )
    {
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketReopenedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;
            var actorName = _currentUser.FullName ?? "Unknown";

            await _activityStream.BroadcastActivityAsync(
                new ActivityEvent
                {
                    Type = ActivityEventType.TicketReopened,
                    Message = $"Ticket #{ticket.Id} reopened",
                    Details = ticket.Title,
                    ActorId = _currentUser.UserIdAsGuid,
                    ActorName = actorName,
                    TicketId = ticket.Id,
                    ContactId = ticket.ContactId,
                    Url = _urlBuilder.StaffTicketUrl(ticket.Id),
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ticket reopened activity");
        }
    }
}

/// <summary>
/// Broadcasts ticket updated events to the activity stream.
/// </summary>
public class TicketUpdatedEventHandler_ActivityStream : INotificationHandler<TicketUpdatedEvent>
{
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketUpdatedEventHandler_ActivityStream> _logger;

    public TicketUpdatedEventHandler_ActivityStream(
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketUpdatedEventHandler_ActivityStream> logger
    )
    {
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketUpdatedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;
            var actorName = _currentUser.FullName ?? "Unknown";

            // Build details from changed fields
            var changes = new List<string>();
            if (notification.NewTitle != null)
                changes.Add("Title");
            if (notification.NewDescription != null)
                changes.Add("Description");
            if (notification.NewPriority != null)
                changes.Add($"Priority → {notification.NewPriority}");

            var details = changes.Any() ? string.Join(", ", changes) : "Fields updated";

            await _activityStream.BroadcastActivityAsync(
                new ActivityEvent
                {
                    Type = ActivityEventType.TicketUpdated,
                    Message = $"Ticket #{ticket.Id} updated",
                    Details = details,
                    ActorId = _currentUser.UserIdAsGuid,
                    ActorName = actorName,
                    TicketId = ticket.Id,
                    ContactId = ticket.ContactId,
                    Url = _urlBuilder.StaffTicketUrl(ticket.Id),
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ticket updated activity");
        }
    }
}
