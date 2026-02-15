using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Sends email and in-app notification when a ticket is reopened.
/// Notifies:
/// - Ticket assignee (respects their notification preferences)
/// - Ticket followers (bypasses notification preferences)
/// All notifications are deduplicated and the user who reopened is never notified.
/// </summary>
public class TicketReopenedEventHandler_SendNotification
    : BaseTicketNotificationHandler,
        INotificationHandler<TicketReopenedEvent>
{
    public TicketReopenedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<TicketReopenedEventHandler_SendNotification> logger
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
        TicketReopenedEvent notification,
        CancellationToken cancellationToken
    )
    {
        if (ShouldSuppressNotifications(notification.Ticket.Id, "ticket reopened event"))
            return;

        try
        {
            var ticket = notification.Ticket;
            var reopenedByUserId = notification.ReopenedByUserId;

            // === COLLECT RECIPIENTS ===

            // 1. Standard recipients (assignee) - respect preferences
            var standardRecipients = new HashSet<Guid>();
            if (
                ticket.AssigneeId.HasValue
                && (!reopenedByUserId.HasValue || ticket.AssigneeId.Value != reopenedByUserId.Value)
            )
            {
                standardRecipients.Add(ticket.AssigneeId.Value);
            }

            // 2. Forced recipients (followers) - bypass preferences
            var followerIds = await Db
                .TicketFollowers.AsNoTracking()
                .Where(f =>
                    f.TicketId == ticket.Id
                    && (!reopenedByUserId.HasValue || f.StaffAdminId != reopenedByUserId.Value)
                )
                .Select(f => f.StaffAdminId)
                .ToListAsync(cancellationToken);

            var forcedRecipients = new HashSet<Guid>(followerIds);

            // === COMBINE ALL RECIPIENTS ===
            // All recipients (both forced and standard) should have notifications recorded
            var allRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var recipientId in standardRecipients)
                allRecipients.Add(recipientId);

            if (!allRecipients.Any())
                return;

            // === ALWAYS RECORD TO MY NOTIFICATIONS (database) ===
            // InAppNotificationService handles the SignalR popup preference check internally
            await InAppNotificationService.SendToUsersAsync(
                allRecipients,
                NotificationType.TicketReopened,
                $"Ticket #{ticket.Id} reopened",
                ticket.Title,
                RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken
            );

            // === SEND EMAIL NOTIFICATIONS (preference-based) ===

            // Filter standard recipients by their email preferences
            var standardRecipientsFiltered = standardRecipients.Any()
                ? await NotificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                    standardRecipients,
                    NotificationEventType.TICKET_REOPENED,
                    cancellationToken
                )
                : new List<Guid>();

            // Combine: forced recipients always get email, standard only if preferences allow
            var emailRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var recipientId in standardRecipientsFiltered)
                emailRecipients.Add(recipientId);

            if (!emailRecipients.Any())
                return;

            var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketReopenedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var recipients = await Db
                .Users.AsNoTracking()
                .Where(u => emailRecipients.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress))
                .ToListAsync(cancellationToken);

            foreach (var recipient in recipients)
            {
                var renderModel = new
                {
                    TicketId = ticket.Id,
                    TicketTitle = ticket.Title,
                    ReopenedByName = "System",
                    Status = ticket.Status,
                    RecipientName = recipient.FullName,
                    TicketUrl = RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
                };

                var wrappedModel = new Wrapper_RenderModel
                {
                    CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                        CurrentOrganization
                    ),
                    Target = renderModel,
                };

                var subject = RenderEngineService.RenderAsHtml(
                    renderTemplate.Subject,
                    wrappedModel
                );
                var content = RenderEngineService.RenderAsHtml(
                    renderTemplate.Content,
                    wrappedModel
                );

                var emailMessage = new EmailMessage
                {
                    Content = content,
                    To = new List<string> { recipient.EmailAddress },
                    Subject = subject,
                };

                await EmailerService.SendEmailAsync(emailMessage, cancellationToken);

                Logger.LogInformation(
                    "Sent ticket reopened email notification for ticket {TicketId} to {Email}",
                    ticket.Id,
                    recipient.EmailAddress
                );
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send ticket reopened notification");
        }
    }
}
