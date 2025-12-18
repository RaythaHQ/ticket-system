using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.Tickets.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Sends email and in-app notification when a ticket is assigned to an individual or team.
/// </summary>
public class TicketAssignedEventHandler_SendNotification : INotificationHandler<TicketAssignedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilderService;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly ILogger<TicketAssignedEventHandler_SendNotification> _logger;

    public TicketAssignedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilderService,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        ILogger<TicketAssignedEventHandler_SendNotification> logger
    )
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _relativeUrlBuilderService = relativeUrlBuilderService;
        _currentOrganization = currentOrganization;
        _notificationPreferenceService = notificationPreferenceService;
        _inAppNotificationService = inAppNotificationService;
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

            // Handle individual assignment notification
            if (
                notification.NewAssigneeId.HasValue
                && notification.OldAssigneeId != notification.NewAssigneeId
            )
            {
                await SendIndividualAssignmentNotificationAsync(
                    ticket,
                    notification.NewAssigneeId.Value,
                    notification.AssignedByUserId,
                    cancellationToken
                );
            }

            // Handle team assignment notification (when team changes and no individual is assigned)
            if (
                notification.NewTeamId.HasValue
                && notification.OldTeamId != notification.NewTeamId
                && !notification.NewAssigneeId.HasValue
            )
            {
                await SendTeamAssignmentNotificationAsync(
                    ticket,
                    notification.NewTeamId.Value,
                    notification.AssignedByUserId,
                    cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket assignment notification");
        }
    }

    private async Task SendIndividualAssignmentNotificationAsync(
        Ticket ticket,
        Guid assigneeId,
        Guid? assignedByUserId,
        CancellationToken cancellationToken
    )
    {
        // Don't notify if the assignee is the one who made the assignment
        if (assignedByUserId.HasValue && assigneeId == assignedByUserId.Value)
            return;

        // Check notification preferences
        var emailEnabled = await _notificationPreferenceService.IsEmailEnabledAsync(
            assigneeId,
            NotificationEventType.TICKET_ASSIGNED,
            cancellationToken
        );

        if (!emailEnabled)
            return;

        var assignee = await _db
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);

        if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress))
            return;

        var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
            p.DeveloperName == BuiltInEmailTemplate.TicketAssignedEmail.DeveloperName
        );

        if (renderTemplate == null)
            return;

        var renderModel = new TicketAssigned_RenderModel
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            Priority = ticket.Priority,
            Status = ticket.Status,
            Category = ticket.Category,
            AssigneeName = assignee.FullName,
            ContactName = ticket.Contact?.FullName,
            TeamName = ticket.OwningTeam?.Name,
            SlaDueAt = ticket.SlaDueAt?.ToString("MMM dd, yyyy h:mm tt"),
            TicketUrl = _relativeUrlBuilderService.StaffTicketUrl(ticket.Id),
        };

        var wrappedModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                _currentOrganization
            ),
            Target = renderModel,
        };

        var subject = _renderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
        var content = _renderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

        var emailMessage = new EmailMessage
        {
            Content = content,
            To = new List<string> { assignee.EmailAddress },
            Subject = subject,
        };

        await _emailerService.SendEmailAsync(emailMessage, cancellationToken);

        _logger.LogInformation(
            "Sent ticket assignment notification for ticket {TicketId} to {Email}",
            ticket.Id,
            assignee.EmailAddress
        );

        // Send in-app notification
        var inAppEnabled = await _notificationPreferenceService.IsInAppEnabledAsync(
            assigneeId,
            NotificationEventType.TICKET_ASSIGNED,
            cancellationToken
        );

        if (inAppEnabled)
        {
            await _inAppNotificationService.SendToUserAsync(
                assigneeId,
                NotificationType.TicketAssigned,
                $"Ticket #{ticket.Id} assigned to you",
                $"{ticket.Title}",
                _relativeUrlBuilderService.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken
            );
        }
    }

    private async Task SendTeamAssignmentNotificationAsync(
        Ticket ticket,
        Guid teamId,
        Guid? assignedByUserId,
        CancellationToken cancellationToken
    )
    {
        var team = await _db
            .Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team == null)
            return;

        // Get all team members
        var teamMemberIds = await _db
            .TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .Select(m => m.StaffAdminId)
            .ToListAsync(cancellationToken);

        if (!teamMemberIds.Any())
            return;

        // Filter by notification preferences
        var usersWithEmailEnabled =
            await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                teamMemberIds,
                NotificationEventType.TICKET_ASSIGNED_TEAM,
                cancellationToken
            );

        if (!usersWithEmailEnabled.Any())
            return;

        // Get users with email addresses
        var teamMembers = await _db
            .Users.AsNoTracking()
            .Where(u =>
                usersWithEmailEnabled.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress)
            )
            .ToListAsync(cancellationToken);

        if (!teamMembers.Any())
            return;

        var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
            p.DeveloperName == BuiltInEmailTemplate.TicketAssignedToTeamEmail.DeveloperName
        );

        if (renderTemplate == null)
            return;

        var renderModel = new TicketAssignedToTeam_RenderModel
        {
            TicketId = ticket.Id,
            TicketTitle = ticket.Title,
            Priority = ticket.Priority,
            TeamName = team.Name,
            AssigneeName = null, // Team assignment with no individual
            TicketUrl = _relativeUrlBuilderService.StaffTicketUrl(ticket.Id),
        };

        var wrappedModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                _currentOrganization
            ),
            Target = renderModel,
        };

        var subject = _renderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
        var content = _renderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

        foreach (var member in teamMembers)
        {
            // Don't notify the person who made the assignment
            if (assignedByUserId.HasValue && member.Id == assignedByUserId.Value)
                continue;

            var emailMessage = new EmailMessage
            {
                Content = content,
                To = new List<string> { member.EmailAddress },
                Subject = subject,
            };

            await _emailerService.SendEmailAsync(emailMessage, cancellationToken);

            _logger.LogInformation(
                "Sent team assignment notification for ticket {TicketId} to team member {Email}",
                ticket.Id,
                member.EmailAddress
            );
        }

        // Send in-app notifications to team members
        var usersWithInAppEnabled =
            await _notificationPreferenceService.FilterUsersWithInAppEnabledAsync(
                teamMemberIds.Where(id =>
                    !assignedByUserId.HasValue || id != assignedByUserId.Value
                ),
                NotificationEventType.TICKET_ASSIGNED_TEAM,
                cancellationToken
            );

        if (usersWithInAppEnabled.Any())
        {
            await _inAppNotificationService.SendToUsersAsync(
                usersWithInAppEnabled,
                NotificationType.TicketAssigned,
                $"Ticket #{ticket.Id} assigned to {team.Name}",
                $"{ticket.Title}",
                _relativeUrlBuilderService.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken
            );
        }
    }
}
