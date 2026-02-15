using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.SlaRules;
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
/// Sends email and in-app notification when an SLA is breached.
/// </summary>
public class SlaBreachedEventHandler_SendNotification
    : BaseTicketNotificationHandler,
        INotificationHandler<SlaBreachedEvent>
{
    public SlaBreachedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<SlaBreachedEventHandler_SendNotification> logger
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
        SlaBreachedEvent notification,
        CancellationToken cancellationToken
    )
    {
        if (ShouldSuppressNotifications(notification.Ticket.Id, "SLA breached event"))
            return;

        var ticket = notification.Ticket;

        try
        {
            var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.SlaBreachedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var slaRule = ticket.SlaRuleId.HasValue
                ? await Db
                    .SlaRules.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == ticket.SlaRuleId.Value, cancellationToken)
                : null;

            // Parse breach behavior to get notification settings
            BreachBehavior? breachBehavior = null;
            if (!string.IsNullOrEmpty(slaRule?.BreachBehaviorJson))
            {
                try
                {
                    breachBehavior = JsonSerializer.Deserialize<BreachBehavior>(
                        slaRule.BreachBehaviorJson
                    );
                }
                catch
                {
                    // Ignore deserialization errors
                }
            }

            // Get assignee info for the render model
            User? assignee = null;
            if (ticket.AssigneeId.HasValue)
            {
                assignee = await Db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == ticket.AssigneeId.Value, cancellationToken);
            }

            var renderModel = new SlaBreach_RenderModel
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                AssigneeName = assignee?.FullName ?? "Unassigned",
                Priority = ticket.Priority,
                SlaDueAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    ticket.SlaDueAt
                ),
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

            // === ALWAYS RECORD TO MY NOTIFICATIONS (database) for assignee ===
            // InAppNotificationService handles the SignalR popup preference check internally
            if (ticket.AssigneeId.HasValue && (breachBehavior?.NotifyAssignee ?? true))
            {
                await InAppNotificationService.SendToUserAsync(
                    ticket.AssigneeId.Value,
                    NotificationType.SlaBreach,
                    $"SLA Breached: #{ticket.Id}",
                    $"{ticket.Title} - SLA has been breached",
                    RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
                    ticket.Id,
                    cancellationToken
                );
            }

            // === SEND EMAIL NOTIFICATIONS (preference-based) ===

            // Send to assignee if enabled
            if (
                ticket.AssigneeId.HasValue
                && assignee != null
                && !string.IsNullOrEmpty(assignee.EmailAddress)
                && (breachBehavior?.NotifyAssignee ?? true)
            )
            {
                var emailEnabled = await NotificationPreferenceService.IsEmailEnabledAsync(
                    ticket.AssigneeId.Value,
                    NotificationEventType.SLA_BREACHED,
                    cancellationToken
                );

                if (emailEnabled)
                {
                    var emailMessage = new EmailMessage
                    {
                        Content = content,
                        To = new List<string> { assignee.EmailAddress },
                        Subject = subject,
                    };

                    await EmailerService.SendEmailAsync(emailMessage, cancellationToken);

                    Logger.LogInformation(
                        "Sent SLA breach email notification for ticket {TicketId} to assignee {Email}",
                        ticket.Id,
                        assignee.EmailAddress
                    );
                }
            }

            // Send to additional notification emails (these are external addresses, always send)
            if (!string.IsNullOrWhiteSpace(breachBehavior?.AdditionalNotificationEmails))
            {
                var additionalEmails = breachBehavior
                    .AdditionalNotificationEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                foreach (var email in additionalEmails)
                {
                    var emailMessage = new EmailMessage
                    {
                        Content = content,
                        To = new List<string> { email },
                        Subject = subject,
                    };

                    await EmailerService.SendEmailAsync(emailMessage, cancellationToken);

                    Logger.LogInformation(
                        "Sent SLA breach email notification for ticket {TicketId} to additional recipient {Email}",
                        ticket.Id,
                        email
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send SLA breach notification");
        }
    }
}
