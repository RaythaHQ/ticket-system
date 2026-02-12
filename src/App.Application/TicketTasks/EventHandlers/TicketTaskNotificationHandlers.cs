using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.TicketTasks.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.TicketTasks.EventHandlers;

/// <summary>
/// Sends notifications when a task is completed.
/// Notifies: task assignee + ticket followers.
/// </summary>
public class TicketTaskCompletedHandler_Notification : INotificationHandler<TicketTaskCompletedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailer;
    private readonly IRenderEngine _renderEngine;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly INotificationSuppressionService _notificationSuppressionService;
    private readonly ILogger<TicketTaskCompletedHandler_Notification> _logger;

    public TicketTaskCompletedHandler_Notification(
        IAppDbContext db,
        IEmailer emailer,
        IRenderEngine renderEngine,
        IInAppNotificationService inAppNotificationService,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<TicketTaskCompletedHandler_Notification> logger)
    {
        _db = db;
        _emailer = emailer;
        _renderEngine = renderEngine;
        _inAppNotificationService = inAppNotificationService;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
        _notificationPreferenceService = notificationPreferenceService;
        _notificationSuppressionService = notificationSuppressionService;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (_notificationSuppressionService.ShouldSuppressNotifications()) return;

        try
        {
            var task = notification.Task;
            var ticket = await _db.Tickets.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == task.TicketId, cancellationToken);

            if (ticket == null) return;

            var recipientIds = new HashSet<Guid>();

            // Task assignee
            if (task.AssigneeId.HasValue && task.AssigneeId.Value != _currentUser.UserIdAsGuid)
                recipientIds.Add(task.AssigneeId.Value);

            // Ticket followers
            var followerIds = await _db.TicketFollowers.AsNoTracking()
                .Where(f => f.TicketId == task.TicketId && f.StaffAdminId != _currentUser.UserIdAsGuid)
                .Select(f => f.StaffAdminId)
                .ToListAsync(cancellationToken);
            foreach (var id in followerIds) recipientIds.Add(id);

            if (!recipientIds.Any()) return;

            // In-app notifications
            await _inAppNotificationService.SendToUsersAsync(
                recipientIds,
                NotificationType.TaskCompleted,
                $"Task completed on #{ticket.Id}",
                task.Title,
                _urlBuilder.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken);

            // Email notifications
            var usersWithEmailEnabled = await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                recipientIds, NotificationEventType.TASK_COMPLETED, cancellationToken);

            if (!usersWithEmailEnabled.Any()) return;

            var recipients = await _db.Users.AsNoTracking()
                .Where(u => usersWithEmailEnabled.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress))
                .ToListAsync(cancellationToken);

            if (!recipients.Any()) return;

            var renderTemplate = _db.EmailTemplates
                .FirstOrDefault(p => p.DeveloperName == BuiltInEmailTemplate.TaskCompletedEmail.DeveloperName);

            if (renderTemplate == null) return;

            foreach (var recipient in recipients)
            {
                var renderModel = new TaskCompleted_RenderModel
                {
                    TicketId = ticket.Id,
                    TicketTitle = ticket.Title,
                    TaskTitle = task.Title,
                    RecipientName = recipient.FullName,
                    CompletedBy = _currentUser.FullName ?? "Unknown",
                    TicketUrl = _urlBuilder.StaffTicketUrl(ticket.Id),
                };

                var wrappedModel = new Wrapper_RenderModel
                {
                    CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(_currentOrganization),
                    Target = renderModel,
                };

                var subject = _renderEngine.RenderAsHtml(renderTemplate.Subject, wrappedModel);
                var content = _renderEngine.RenderAsHtml(renderTemplate.Content, wrappedModel);

                await _emailer.SendEmailAsync(new EmailMessage
                {
                    Content = content,
                    To = new List<string> { recipient.EmailAddress },
                    Subject = subject,
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send task completed notification");
        }
    }
}

/// <summary>
/// Sends assignment notification when a task is assigned (created or reassigned).
/// Only sends if the task is NOT blocked.
/// </summary>
public class TicketTaskAssignedHandler_Notification : INotificationHandler<TicketTaskAssignedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailer;
    private readonly IRenderEngine _renderEngine;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly INotificationSuppressionService _notificationSuppressionService;
    private readonly ILogger<TicketTaskAssignedHandler_Notification> _logger;

    public TicketTaskAssignedHandler_Notification(
        IAppDbContext db,
        IEmailer emailer,
        IRenderEngine renderEngine,
        IInAppNotificationService inAppNotificationService,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<TicketTaskAssignedHandler_Notification> logger)
    {
        _db = db;
        _emailer = emailer;
        _renderEngine = renderEngine;
        _inAppNotificationService = inAppNotificationService;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
        _notificationPreferenceService = notificationPreferenceService;
        _notificationSuppressionService = notificationSuppressionService;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        if (_notificationSuppressionService.ShouldSuppressNotifications()) return;

        try
        {
            var task = notification.Task;

            // Don't send assignment notifications if the task is blocked
            if (task.DependsOnTaskId != null)
            {
                var depTask = await _db.TicketTasks.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == task.DependsOnTaskId, cancellationToken);
                if (depTask != null && depTask.Status != TicketTaskStatus.CLOSED)
                    return; // Blocked — defer notification until unblocked
            }

            var ticket = await _db.Tickets.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == task.TicketId, cancellationToken);
            if (ticket == null) return;

            // Determine if this is a user or team assignment
            if (task.AssigneeId.HasValue)
            {
                await SendUserAssignmentNotificationAsync(task, ticket, cancellationToken);
            }
            else if (task.OwningTeamId.HasValue)
            {
                await SendTeamAssignmentNotificationAsync(task, ticket, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send task assigned notification");
        }
    }

    private async Task SendUserAssignmentNotificationAsync(
        TicketTask task, Ticket ticket, CancellationToken cancellationToken)
    {
        if (!task.AssigneeId.HasValue || task.AssigneeId.Value == _currentUser.UserIdAsGuid)
            return;

        var assigneeId = task.AssigneeId.Value;

        // In-app notification
        await _inAppNotificationService.SendToUserAsync(
            assigneeId,
            NotificationType.TaskAssignedUser,
            $"Task assigned to you on #{ticket.Id}",
            task.Title,
            _urlBuilder.StaffTicketUrl(ticket.Id),
            ticket.Id,
            cancellationToken);

        // Email notification
        var emailEnabled = await _notificationPreferenceService.IsEmailEnabledAsync(
            assigneeId, NotificationEventType.TASK_ASSIGNED_USER, cancellationToken);
        if (!emailEnabled) return;

        var assignee = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
        if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress)) return;

        var renderTemplate = _db.EmailTemplates
            .FirstOrDefault(p => p.DeveloperName == BuiltInEmailTemplate.TaskAssignedUserEmail.DeveloperName);
        if (renderTemplate == null) return;

        var renderModel = new TaskAssignedUser_RenderModel
        {
            TicketId = ticket.Id,
            TicketTitle = ticket.Title,
            TaskTitle = task.Title,
            RecipientName = assignee.FullName,
            DueAt = task.DueAt?.ToString("MMM dd, yyyy h:mm tt"),
            TicketUrl = _urlBuilder.StaffTicketUrl(ticket.Id),
        };

        var wrappedModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(_currentOrganization),
            Target = renderModel,
        };

        var subject = _renderEngine.RenderAsHtml(renderTemplate.Subject, wrappedModel);
        var content = _renderEngine.RenderAsHtml(renderTemplate.Content, wrappedModel);

        await _emailer.SendEmailAsync(new EmailMessage
        {
            Content = content,
            To = new List<string> { assignee.EmailAddress },
            Subject = subject,
        }, cancellationToken);
    }

    private async Task SendTeamAssignmentNotificationAsync(
        TicketTask task, Ticket ticket, CancellationToken cancellationToken)
    {
        if (!task.OwningTeamId.HasValue) return;

        var team = await _db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == task.OwningTeamId.Value, cancellationToken);
        if (team == null) return;

        var teamMemberIds = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == task.OwningTeamId.Value)
            .Select(m => m.StaffAdminId)
            .ToListAsync(cancellationToken);

        var recipientIds = teamMemberIds
            .Where(id => id != _currentUser.UserIdAsGuid)
            .ToList();

        if (!recipientIds.Any()) return;

        // In-app notifications
        await _inAppNotificationService.SendToUsersAsync(
            recipientIds,
            NotificationType.TaskAssignedTeam,
            $"Task assigned to {team.Name} on #{ticket.Id}",
            task.Title,
            _urlBuilder.StaffTicketUrl(ticket.Id),
            ticket.Id,
            cancellationToken);

        // Email notifications
        var usersWithEmailEnabled = await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
            recipientIds, NotificationEventType.TASK_ASSIGNED_TEAM, cancellationToken);

        if (!usersWithEmailEnabled.Any()) return;

        var teamMembers = await _db.Users.AsNoTracking()
            .Where(u => usersWithEmailEnabled.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress))
            .ToListAsync(cancellationToken);

        if (!teamMembers.Any()) return;

        var renderTemplate = _db.EmailTemplates
            .FirstOrDefault(p => p.DeveloperName == BuiltInEmailTemplate.TaskAssignedTeamEmail.DeveloperName);
        if (renderTemplate == null) return;

        foreach (var member in teamMembers)
        {
            var renderModel = new TaskAssignedTeam_RenderModel
            {
                TicketId = ticket.Id,
                TicketTitle = ticket.Title,
                TaskTitle = task.Title,
                TeamName = team.Name,
                RecipientName = member.FullName,
                DueAt = task.DueAt?.ToString("MMM dd, yyyy h:mm tt"),
                TicketUrl = _urlBuilder.StaffTicketUrl(ticket.Id),
            };

            var wrappedModel = new Wrapper_RenderModel
            {
                CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(_currentOrganization),
                Target = renderModel,
            };

            var subject = _renderEngine.RenderAsHtml(renderTemplate.Subject, wrappedModel);
            var content = _renderEngine.RenderAsHtml(renderTemplate.Content, wrappedModel);

            await _emailer.SendEmailAsync(new EmailMessage
            {
                Content = content,
                To = new List<string> { member.EmailAddress },
                Subject = subject,
            }, cancellationToken);
        }
    }
}

/// <summary>
/// Sends assignment notification when a task becomes unblocked (dependency resolved).
/// This is the deferred notification that fires when a blocked task's dependency is completed.
/// </summary>
public class TicketTaskUnblockedHandler_Notification : INotificationHandler<TicketTaskUnblockedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailer;
    private readonly IRenderEngine _renderEngine;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly INotificationSuppressionService _notificationSuppressionService;
    private readonly ILogger<TicketTaskUnblockedHandler_Notification> _logger;

    public TicketTaskUnblockedHandler_Notification(
        IAppDbContext db,
        IEmailer emailer,
        IRenderEngine renderEngine,
        IInAppNotificationService inAppNotificationService,
        IRelativeUrlBuilder urlBuilder,
        ICurrentUser currentUser,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<TicketTaskUnblockedHandler_Notification> logger)
    {
        _db = db;
        _emailer = emailer;
        _renderEngine = renderEngine;
        _inAppNotificationService = inAppNotificationService;
        _urlBuilder = urlBuilder;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
        _notificationPreferenceService = notificationPreferenceService;
        _notificationSuppressionService = notificationSuppressionService;
        _logger = logger;
    }

    public async ValueTask Handle(TicketTaskUnblockedEvent notification, CancellationToken cancellationToken)
    {
        if (_notificationSuppressionService.ShouldSuppressNotifications()) return;

        try
        {
            var task = notification.Task;
            var ticket = await _db.Tickets.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == task.TicketId, cancellationToken);
            if (ticket == null) return;

            // Send the appropriate assignment notification now that the task is unblocked
            if (task.AssigneeId.HasValue)
            {
                await SendUserUnblockedNotificationAsync(task, ticket, cancellationToken);
            }
            else if (task.OwningTeamId.HasValue)
            {
                await SendTeamUnblockedNotificationAsync(task, ticket, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send task unblocked notification");
        }
    }

    private async Task SendUserUnblockedNotificationAsync(
        TicketTask task, Ticket ticket, CancellationToken cancellationToken)
    {
        var assigneeId = task.AssigneeId!.Value;

        await _inAppNotificationService.SendToUserAsync(
            assigneeId,
            NotificationType.TaskAssignedUser,
            $"Task unblocked on #{ticket.Id}",
            $"{task.Title} is now ready to work on",
            _urlBuilder.StaffTicketUrl(ticket.Id),
            ticket.Id,
            cancellationToken);

        var emailEnabled = await _notificationPreferenceService.IsEmailEnabledAsync(
            assigneeId, NotificationEventType.TASK_ASSIGNED_USER, cancellationToken);
        if (!emailEnabled) return;

        var assignee = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
        if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress)) return;

        var renderTemplate = _db.EmailTemplates
            .FirstOrDefault(p => p.DeveloperName == BuiltInEmailTemplate.TaskAssignedUserEmail.DeveloperName);
        if (renderTemplate == null) return;

        var renderModel = new TaskAssignedUser_RenderModel
        {
            TicketId = ticket.Id,
            TicketTitle = ticket.Title,
            TaskTitle = task.Title,
            RecipientName = assignee.FullName,
            DueAt = task.DueAt?.ToString("MMM dd, yyyy h:mm tt"),
            TicketUrl = _urlBuilder.StaffTicketUrl(ticket.Id),
        };

        var wrappedModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(_currentOrganization),
            Target = renderModel,
        };

        var subject = _renderEngine.RenderAsHtml(renderTemplate.Subject, wrappedModel);
        var content = _renderEngine.RenderAsHtml(renderTemplate.Content, wrappedModel);

        await _emailer.SendEmailAsync(new EmailMessage
        {
            Content = content,
            To = new List<string> { assignee.EmailAddress },
            Subject = subject,
        }, cancellationToken);
    }

    private async Task SendTeamUnblockedNotificationAsync(
        TicketTask task, Ticket ticket, CancellationToken cancellationToken)
    {
        var team = await _db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == task.OwningTeamId!.Value, cancellationToken);
        if (team == null) return;

        var teamMemberIds = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == task.OwningTeamId!.Value)
            .Select(m => m.StaffAdminId)
            .ToListAsync(cancellationToken);

        if (!teamMemberIds.Any()) return;

        await _inAppNotificationService.SendToUsersAsync(
            teamMemberIds,
            NotificationType.TaskAssignedTeam,
            $"Task unblocked on #{ticket.Id}",
            $"{task.Title} is now ready for {team.Name}",
            _urlBuilder.StaffTicketUrl(ticket.Id),
            ticket.Id,
            cancellationToken);

        var usersWithEmailEnabled = await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
            teamMemberIds, NotificationEventType.TASK_ASSIGNED_TEAM, cancellationToken);

        if (!usersWithEmailEnabled.Any()) return;

        var teamMembers = await _db.Users.AsNoTracking()
            .Where(u => usersWithEmailEnabled.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress))
            .ToListAsync(cancellationToken);

        if (!teamMembers.Any()) return;

        var renderTemplate = _db.EmailTemplates
            .FirstOrDefault(p => p.DeveloperName == BuiltInEmailTemplate.TaskAssignedTeamEmail.DeveloperName);
        if (renderTemplate == null) return;

        foreach (var member in teamMembers)
        {
            var renderModel = new TaskAssignedTeam_RenderModel
            {
                TicketId = ticket.Id,
                TicketTitle = ticket.Title,
                TaskTitle = task.Title,
                TeamName = team.Name,
                RecipientName = member.FullName,
                DueAt = task.DueAt?.ToString("MMM dd, yyyy h:mm tt"),
                TicketUrl = _urlBuilder.StaffTicketUrl(ticket.Id),
            };

            var wrappedModel = new Wrapper_RenderModel
            {
                CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(_currentOrganization),
                Target = renderModel,
            };

            var subject = _renderEngine.RenderAsHtml(renderTemplate.Subject, wrappedModel);
            var content = _renderEngine.RenderAsHtml(renderTemplate.Content, wrappedModel);

            await _emailer.SendEmailAsync(new EmailMessage
            {
                Content = content,
                To = new List<string> { member.EmailAddress },
                Subject = subject,
            }, cancellationToken);
        }
    }
}
