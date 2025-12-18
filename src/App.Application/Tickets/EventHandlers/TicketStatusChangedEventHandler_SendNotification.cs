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
/// Sends email and in-app notification when a ticket's status changes.
/// Notifies:
/// - Ticket assignee (respects their notification preferences)
/// - Ticket followers (bypasses notification preferences)
/// All notifications are deduplicated and the user who made the change is never notified.
/// </summary>
public class TicketStatusChangedEventHandler_SendNotification
    : INotificationHandler<TicketStatusChangedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly ILogger<TicketStatusChangedEventHandler_SendNotification> _logger;

    public TicketStatusChangedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        ILogger<TicketStatusChangedEventHandler_SendNotification> logger
    )
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _relativeUrlBuilder = relativeUrlBuilder;
        _currentOrganization = currentOrganization;
        _notificationPreferenceService = notificationPreferenceService;
        _inAppNotificationService = inAppNotificationService;
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
            var changedByUserId = notification.ChangedByUserId;

            // === COLLECT RECIPIENTS ===

            // 1. Standard recipients (assignee) - respect preferences
            var standardRecipients = new HashSet<Guid>();
            if (
                ticket.AssigneeId.HasValue
                && (!changedByUserId.HasValue || ticket.AssigneeId.Value != changedByUserId.Value)
            )
            {
                standardRecipients.Add(ticket.AssigneeId.Value);
            }

            // 2. Forced recipients (followers) - bypass preferences
            var followerIds = await _db
                .TicketFollowers.AsNoTracking()
                .Where(f =>
                    f.TicketId == ticket.Id
                    && (!changedByUserId.HasValue || f.StaffAdminId != changedByUserId.Value)
                )
                .Select(f => f.StaffAdminId)
                .ToListAsync(cancellationToken);

            var forcedRecipients = new HashSet<Guid>(followerIds);

            // === FILTER AND DEDUPLICATE ===

            // Filter standard recipients by their notification preferences
            var standardRecipientsFiltered = standardRecipients.Any()
                ? await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                    standardRecipients,
                    NotificationEventType.STATUS_CHANGED,
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
                p.DeveloperName == BuiltInEmailTemplate.TicketStatusChangedEmail.DeveloperName
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
                var renderModel = new TicketStatusChanged_RenderModel
                {
                    TicketId = ticket.Id,
                    Title = ticket.Title,
                    OldStatus = notification.OldStatus,
                    NewStatus = notification.NewStatus,
                    ChangedBy = "System",
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
                    "Sent status change notification for ticket {TicketId} to {Email}",
                    ticket.Id,
                    recipient.EmailAddress
                );
            }

            // === SEND IN-APP NOTIFICATIONS ===

            // For standard recipients, check their in-app preferences
            var standardRecipientsInApp = standardRecipients.Any()
                ? await _notificationPreferenceService.FilterUsersWithInAppEnabledAsync(
                    standardRecipients,
                    NotificationEventType.STATUS_CHANGED,
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
                    NotificationType.StatusChanged,
                    $"Status changed: #{ticket.Id}",
                    $"{notification.OldStatus} → {notification.NewStatus}",
                    _relativeUrlBuilder.StaffTicketUrl(ticket.Id),
                    ticket.Id,
                    cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket status change notification");
        }
    }
}
