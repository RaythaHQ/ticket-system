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
public class TicketAssignedEventHandler_SendNotification
    : BaseTicketNotificationHandler, INotificationHandler<TicketAssignedEvent>
{
    public TicketAssignedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilderService,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<TicketAssignedEventHandler_SendNotification> logger
    )
        : base(
            db, emailerService, renderEngineService, relativeUrlBuilderService,
            currentOrganization, notificationPreferenceService, inAppNotificationService,
            notificationSuppressionService, logger
        )
    { }

    public async ValueTask Handle(
        TicketAssignedEvent notification,
        CancellationToken cancellationToken
    )
    {
        if (ShouldSuppressNotifications(notification.Ticket.Id, "ticket assigned event"))
            return;

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
            Logger.LogError(ex, "Failed to send ticket assignment notification");
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

        // ALWAYS record to My Notifications (database) regardless of delivery preferences
        // The InAppNotificationService handles the preference check for the SignalR popup
        await InAppNotificationService.SendToUserAsync(
            assigneeId,
            NotificationType.TicketAssigned,
            $"Ticket #{ticket.Id} assigned to you",
            $"{ticket.Title}",
            RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
            ticket.Id,
            cancellationToken
        );

        // Check email notification preferences
        var emailEnabled = await NotificationPreferenceService.IsEmailEnabledAsync(
            assigneeId,
            NotificationEventType.TICKET_ASSIGNED,
            cancellationToken
        );

        if (!emailEnabled)
            return;

        var assignee = await Db
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);

        if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress))
            return;

        var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
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
            TicketUrl = RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
        };

        var wrappedModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                CurrentOrganization
            ),
            Target = renderModel,
        };

        var subject = RenderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
        var content = RenderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

        var emailMessage = new EmailMessage
        {
            Content = content,
            To = new List<string> { assignee.EmailAddress },
            Subject = subject,
        };

        await EmailerService.SendEmailAsync(emailMessage, cancellationToken);

        Logger.LogInformation(
            "Sent ticket assignment notification for ticket {TicketId} to {Email}",
            ticket.Id,
            assignee.EmailAddress
        );
    }

    private async Task SendTeamAssignmentNotificationAsync(
        Ticket ticket,
        Guid teamId,
        Guid? assignedByUserId,
        CancellationToken cancellationToken
    )
    {
        var team = await Db
            .Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team == null)
            return;

        // Get all team members (excluding the person who made the assignment)
        var teamMemberIds = await Db
            .TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .Select(m => m.StaffAdminId)
            .ToListAsync(cancellationToken);

        if (!teamMemberIds.Any())
            return;

        // Filter out the person who made the assignment
        var recipientIds = teamMemberIds
            .Where(id => !assignedByUserId.HasValue || id != assignedByUserId.Value)
            .ToList();

        if (!recipientIds.Any())
            return;

        // ALWAYS record to My Notifications (database) for all team members
        // The InAppNotificationService handles the preference check for the SignalR popup
        await InAppNotificationService.SendToUsersAsync(
            recipientIds,
            NotificationType.TicketAssigned,
            $"Ticket #{ticket.Id} assigned to {team.Name}",
            $"{ticket.Title}",
            RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
            ticket.Id,
            cancellationToken
        );

        // Now handle email notifications (preference-based)
        var usersWithEmailEnabled =
            await NotificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                recipientIds,
                NotificationEventType.TICKET_ASSIGNED_TEAM,
                cancellationToken
            );

        if (!usersWithEmailEnabled.Any())
            return;

        // Get users with email addresses
        var teamMembers = await Db
            .Users.AsNoTracking()
            .Where(u =>
                usersWithEmailEnabled.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress)
            )
            .ToListAsync(cancellationToken);

        if (!teamMembers.Any())
            return;

        var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
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
            TicketUrl = RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
        };

        var wrappedModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                CurrentOrganization
            ),
            Target = renderModel,
        };

        var subject = RenderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
        var content = RenderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

        foreach (var member in teamMembers)
        {
            var emailMessage = new EmailMessage
            {
                Content = content,
                To = new List<string> { member.EmailAddress },
                Subject = subject,
            };

            await EmailerService.SendEmailAsync(emailMessage, cancellationToken);

            Logger.LogInformation(
                "Sent team assignment notification for ticket {TicketId} to team member {Email}",
                ticket.Id,
                member.EmailAddress
            );
        }
    }
}
