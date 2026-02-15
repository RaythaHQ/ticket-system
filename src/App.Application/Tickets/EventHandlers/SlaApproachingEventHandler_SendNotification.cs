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
/// Sends email and in-app notification when an SLA is approaching breach.
/// </summary>
public class SlaApproachingEventHandler_SendNotification
    : BaseTicketNotificationHandler,
        INotificationHandler<SlaApproachingBreachEvent>
{
    public SlaApproachingEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<SlaApproachingEventHandler_SendNotification> logger
    )
        : base(
            db,
            emailerService,
            renderEngineService,
            relativeUrlBuilder,
            currentOrganization,
            notificationPreferenceService,
            inAppNotificationService,
            notificationSuppressionService,
            logger
        ) { }

    public async ValueTask Handle(
        SlaApproachingBreachEvent notification,
        CancellationToken cancellationToken
    )
    {
        if (ShouldSuppressNotifications(notification.Ticket.Id, "SLA approaching event"))
            return;

        var ticket = notification.Ticket;

        if (!ticket.AssigneeId.HasValue)
            return;

        try
        {
            var timeRemaining = ticket.SlaDueAt.HasValue
                ? FormatTimeRemaining(ticket.SlaDueAt.Value - DateTime.UtcNow)
                : "Unknown";

            // === ALWAYS RECORD TO MY NOTIFICATIONS (database) ===
            // InAppNotificationService handles the SignalR popup preference check internally
            await InAppNotificationService.SendToUserAsync(
                ticket.AssigneeId.Value,
                NotificationType.SlaApproaching,
                $"SLA Warning: #{ticket.Id}",
                $"{ticket.Title} - {timeRemaining} remaining",
                RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken
            );

            // === SEND EMAIL NOTIFICATION (preference-based) ===
            var emailEnabled = await NotificationPreferenceService.IsEmailEnabledAsync(
                ticket.AssigneeId.Value,
                NotificationEventType.SLA_APPROACHING,
                cancellationToken
            );

            if (!emailEnabled)
                return;

            var assignee = await Db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ticket.AssigneeId.Value, cancellationToken);

            if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress))
                return;

            var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.SlaApproachingEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var slaRule = ticket.SlaRuleId.HasValue
                ? await Db
                    .SlaRules.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == ticket.SlaRuleId.Value, cancellationToken)
                : null;

            var renderModel = new SlaApproaching_RenderModel
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                AssigneeName = assignee.FullName,
                Priority = ticket.Priority,
                SlaDueAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    ticket.SlaDueAt
                ),
                TimeRemaining = timeRemaining,
                SlaRuleName = slaRule?.Name ?? "Unknown",
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
                "Sent SLA approaching email notification for ticket {TicketId} to {Email}",
                ticket.Id,
                assignee.EmailAddress
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send SLA approaching notification");
        }
    }

    private static string FormatTimeRemaining(TimeSpan remaining)
    {
        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays} day{((int)remaining.TotalDays > 1 ? "s" : "")}";
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours} hour{((int)remaining.TotalHours > 1 ? "s" : "")}";
        if (remaining.TotalMinutes >= 1)
            return $"{(int)remaining.TotalMinutes} minute{((int)remaining.TotalMinutes > 1 ? "s" : "")}";
        return "Less than a minute";
    }
}
