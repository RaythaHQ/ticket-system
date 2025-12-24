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
public class TicketReopenedEventHandler_SendNotification : INotificationHandler<TicketReopenedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly INotificationSuppressionService _notificationSuppressionService;
    private readonly ILogger<TicketReopenedEventHandler_SendNotification> _logger;

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
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _relativeUrlBuilder = relativeUrlBuilder;
        _currentOrganization = currentOrganization;
        _notificationPreferenceService = notificationPreferenceService;
        _inAppNotificationService = inAppNotificationService;
        _notificationSuppressionService = notificationSuppressionService;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketReopenedEvent notification,
        CancellationToken cancellationToken
    )
    {
        // Check if notifications should be suppressed
        if (_notificationSuppressionService.ShouldSuppressNotifications())
        {
            _logger.LogDebug(
                "Notifications suppressed for ticket reopened event on ticket {TicketId}",
                notification.Ticket.Id
            );
            return;
        }

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
            var followerIds = await _db
                .TicketFollowers.AsNoTracking()
                .Where(f =>
                    f.TicketId == ticket.Id
                    && (!reopenedByUserId.HasValue || f.StaffAdminId != reopenedByUserId.Value)
                )
                .Select(f => f.StaffAdminId)
                .ToListAsync(cancellationToken);

            var forcedRecipients = new HashSet<Guid>(followerIds);

            // === FILTER AND DEDUPLICATE ===

            // Filter standard recipients by their notification preferences
            var standardRecipientsFiltered = standardRecipients.Any()
                ? await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                    standardRecipients,
                    NotificationEventType.TICKET_REOPENED,
                    cancellationToken
                )
                : new List<Guid>();

            // Combine: forced recipients always get notified, standard only if preferences allow
            var finalRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var recipientId in standardRecipientsFiltered)
                finalRecipients.Add(recipientId);

            if (!finalRecipients.Any())
                return;

            // === GET EMAIL TEMPLATE ===

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketReopenedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            // === GET RECIPIENT DETAILS AND SEND EMAILS ===

            var recipients = await _db
                .Users.AsNoTracking()
                .Where(u => finalRecipients.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress))
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
                    TicketUrl = _relativeUrlBuilder.StaffTicketUrl(ticket.Id),
                };

                var wrappedModel = new Wrapper_RenderModel
                {
                    CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                        _currentOrganization
                    ),
                    Target = renderModel,
                };

                var subject = _renderEngineService.RenderAsHtml(
                    renderTemplate.Subject,
                    wrappedModel
                );
                var content = _renderEngineService.RenderAsHtml(
                    renderTemplate.Content,
                    wrappedModel
                );

                var emailMessage = new EmailMessage
                {
                    Content = content,
                    To = new List<string> { recipient.EmailAddress },
                    Subject = subject,
                };

                await _emailerService.SendEmailAsync(emailMessage, cancellationToken);

                _logger.LogInformation(
                    "Sent ticket reopened notification for ticket {TicketId} to {Email}",
                    ticket.Id,
                    recipient.EmailAddress
                );
            }

            // === SEND IN-APP NOTIFICATIONS ===

            // For standard recipients, check their in-app preferences
            var standardRecipientsInApp = standardRecipients.Any()
                ? await _notificationPreferenceService.FilterUsersWithInAppEnabledAsync(
                    standardRecipients,
                    NotificationEventType.TICKET_REOPENED,
                    cancellationToken
                )
                : new List<Guid>();

            // Combine: forced recipients always get in-app, standard only if preferences allow
            var finalInAppRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var recipientId in standardRecipientsInApp)
                finalInAppRecipients.Add(recipientId);

            if (finalInAppRecipients.Any())
            {
                await _inAppNotificationService.SendToUsersAsync(
                    finalInAppRecipients,
                    NotificationType.TicketReopened,
                    $"Ticket #{ticket.Id} reopened",
                    ticket.Title,
                    _relativeUrlBuilder.StaffTicketUrl(ticket.Id),
                    ticket.Id,
                    cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket reopened notification");
        }
    }
}
