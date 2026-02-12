using App.Application.Common.Interfaces;
using App.Domain.Entities;
using App.Domain.Events;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.TicketTasks.EventHandlers;

/// <summary>
/// Logs task created to activity stream and change log.
/// </summary>
public class TicketTaskCreatedHandler_Activity : INotificationHandler<TicketTaskCreatedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskCreatedHandler_Activity> _logger;

    public TicketTaskCreatedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskCreatedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var task = notification.Task;
            var actorName = _currentUser.FullName ?? "Unknown";

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = task.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Task created: {task.Title}",
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskCreated,
                Message = $"Task created on ticket #{task.TicketId}",
                Details = task.Title,
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = task.TicketId,
                Url = _urlBuilder.StaffTicketUrl(task.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task created activity");
        }
    }
}

/// <summary>
/// Logs task completed to activity stream and change log.
/// </summary>
public class TicketTaskCompletedHandler_Activity : INotificationHandler<TicketTaskCompletedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskCompletedHandler_Activity> _logger;

    public TicketTaskCompletedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskCompletedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var task = notification.Task;
            var actorName = _currentUser.FullName ?? "Unknown";

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = task.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Task completed: {task.Title}",
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskCompleted,
                Message = $"Task completed on ticket #{task.TicketId}",
                Details = task.Title,
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = task.TicketId,
                Url = _urlBuilder.StaffTicketUrl(task.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task completed activity");
        }
    }
}

/// <summary>
/// Logs task reopened to activity stream and change log.
/// </summary>
public class TicketTaskReopenedHandler_Activity : INotificationHandler<TicketTaskReopenedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskReopenedHandler_Activity> _logger;

    public TicketTaskReopenedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskReopenedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskReopenedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var task = notification.Task;
            var actorName = _currentUser.FullName ?? "Unknown";

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = task.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Task reopened: {task.Title}",
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskReopened,
                Message = $"Task reopened on ticket #{task.TicketId}",
                Details = task.Title,
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = task.TicketId,
                Url = _urlBuilder.StaffTicketUrl(task.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task reopened activity");
        }
    }
}

/// <summary>
/// Logs task assigned/reassigned to activity stream and change log.
/// Includes previous/new assignee info.
/// </summary>
public class TicketTaskAssignedHandler_Activity : INotificationHandler<TicketTaskAssignedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskAssignedHandler_Activity> _logger;

    public TicketTaskAssignedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskAssignedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var task = notification.Task;
            var actorName = _currentUser.FullName ?? "Unknown";

            // Resolve new assignee name
            string newAssigneeName;
            if (task.AssigneeId.HasValue)
            {
                var assignee = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == task.AssigneeId.Value, cancellationToken);
                newAssigneeName = assignee?.FullName ?? "Unknown";
            }
            else if (task.OwningTeamId.HasValue)
            {
                var team = await _db.Teams.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == task.OwningTeamId.Value, cancellationToken);
                newAssigneeName = $"Team: {team?.Name ?? "Unknown"}";
            }
            else
            {
                newAssigneeName = "Unassigned";
            }

            // Resolve previous assignee name
            string prevAssigneeName = "Unassigned";
            if (notification.PreviousAssigneeId.HasValue)
            {
                var prevAssignee = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == notification.PreviousAssigneeId.Value, cancellationToken);
                prevAssigneeName = prevAssignee?.FullName ?? "Unknown";
            }
            else if (notification.PreviousTeamId.HasValue)
            {
                var prevTeam = await _db.Teams.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == notification.PreviousTeamId.Value, cancellationToken);
                prevAssigneeName = $"Team: {prevTeam?.Name ?? "Unknown"}";
            }

            var message = notification.PreviousAssigneeId.HasValue || notification.PreviousTeamId.HasValue
                ? $"Task reassigned from {prevAssigneeName} to {newAssigneeName}: {task.Title}"
                : $"Task assigned to {newAssigneeName}: {task.Title}";

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = task.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = message,
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskAssigned,
                Message = $"Task assignment changed on ticket #{task.TicketId}",
                Details = $"{task.Title} → {newAssigneeName}",
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = task.TicketId,
                Url = _urlBuilder.StaffTicketUrl(task.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task assigned activity");
        }
    }
}

/// <summary>
/// Logs task deleted to activity stream and change log.
/// </summary>
public class TicketTaskDeletedHandler_Activity : INotificationHandler<TicketTaskDeletedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskDeletedHandler_Activity> _logger;

    public TicketTaskDeletedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskDeletedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var actorName = _currentUser.FullName ?? "Unknown";

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = notification.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Task deleted: {notification.TaskTitle}",
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskDeleted,
                Message = $"Task deleted on ticket #{notification.TicketId}",
                Details = notification.TaskTitle,
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = notification.TicketId,
                Url = _urlBuilder.StaffTicketUrl(notification.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task deleted activity");
        }
    }
}

/// <summary>
/// Logs task due date changed to change log.
/// </summary>
public class TicketTaskDueDateChangedHandler_Activity : INotificationHandler<TicketTaskDueDateChangedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskDueDateChangedHandler_Activity> _logger;

    public TicketTaskDueDateChangedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskDueDateChangedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskDueDateChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var task = notification.Task;
            var actorName = _currentUser.FullName ?? "Unknown";
            var prevStr = notification.PreviousDueAt?.ToString("MMM dd, yyyy h:mm tt") ?? "none";
            var newStr = task.DueAt?.ToString("MMM dd, yyyy h:mm tt") ?? "none";

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = task.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Task due date changed from {prevStr} to {newStr}: {task.Title}",
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskDueDateChanged,
                Message = $"Task due date changed on ticket #{task.TicketId}",
                Details = $"{task.Title}: {prevStr} → {newStr}",
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = task.TicketId,
                Url = _urlBuilder.StaffTicketUrl(task.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task due date change");
        }
    }
}

/// <summary>
/// Logs task dependency changed to change log.
/// </summary>
public class TicketTaskDependencyChangedHandler_Activity : INotificationHandler<TicketTaskDependencyChangedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskDependencyChangedHandler_Activity> _logger;

    public TicketTaskDependencyChangedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskDependencyChangedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskDependencyChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var task = notification.Task;
            var actorName = _currentUser.FullName ?? "Unknown";

            // Resolve dependency task titles
            string prevDep = "none";
            if (notification.PreviousDependsOnTaskId.HasValue)
            {
                var prevTask = await _db.TicketTasks.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == notification.PreviousDependsOnTaskId.Value, cancellationToken);
                prevDep = prevTask?.Title ?? "deleted task";
            }

            string newDep = "none";
            if (task.DependsOnTaskId.HasValue)
            {
                var newTask = await _db.TicketTasks.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == task.DependsOnTaskId.Value, cancellationToken);
                newDep = newTask?.Title ?? "unknown task";
            }

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = task.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Task dependency changed from \"{prevDep}\" to \"{newDep}\": {task.Title}",
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskDependencyChanged,
                Message = $"Task dependency changed on ticket #{task.TicketId}",
                Details = $"{task.Title}: {prevDep} → {newDep}",
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = task.TicketId,
                Url = _urlBuilder.StaffTicketUrl(task.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task dependency change");
        }
    }
}

/// <summary>
/// Logs task unblocked to change log.
/// </summary>
public class TicketTaskUnblockedHandler_Activity : INotificationHandler<TicketTaskUnblockedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IActivityStreamService _activityStream;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TicketTaskUnblockedHandler_Activity> _logger;

    public TicketTaskUnblockedHandler_Activity(
        IAppDbContext db,
        IActivityStreamService activityStream,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ILogger<TicketTaskUnblockedHandler_Activity> logger)
    {
        _db = db;
        _activityStream = activityStream;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskUnblockedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var task = notification.Task;
            var actorName = _currentUser.FullName ?? "Unknown";

            _db.TicketChangeLogEntries.Add(new TicketChangeLogEntry
            {
                TicketId = task.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Task unblocked: {task.Title}",
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _activityStream.BroadcastActivityAsync(new ActivityEvent
            {
                Type = ActivityEventType.TaskUnblocked,
                Message = $"Task unblocked on ticket #{task.TicketId}",
                Details = task.Title,
                ActorId = _currentUser.UserIdAsGuid,
                ActorName = actorName,
                TicketId = task.TicketId,
                Url = _urlBuilder.StaffTicketUrl(task.TicketId),
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log task unblocked activity");
        }
    }
}
