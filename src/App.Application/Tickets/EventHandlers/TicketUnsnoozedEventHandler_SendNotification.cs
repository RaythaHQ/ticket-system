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
/// Sends email and in-app notification when a ticket is unsnoozed.
/// Notification rules:
/// - Auto-unsnooze (snooze duration expired): notify assignee + all followers
/// - Manual unsnooze: notify others (not the person who unsnoozed)
///   - If assignee manually unsnoozes their own ticket: no notification to them
///   - If a follower manually unsnoozes: notify assignee + other followers (not the actor)
/// - Skip notification entirely if ticket is closed
/// </summary>
public class TicketUnsnoozedEventHandler_SendNotification : INotificationHandler<TicketUnsnoozedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly INotificationSuppressionService _notificationSuppressionService;
    private readonly ILogger<TicketUnsnoozedEventHandler_SendNotification> _logger;

    public TicketUnsnoozedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<TicketUnsnoozedEventHandler_SendNotification> logger
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
        TicketUnsnoozedEvent notification,
        CancellationToken cancellationToken
    )
    {
        // Check if notifications should be suppressed
        if (_notificationSuppressionService.ShouldSuppressNotifications())
        {
            _logger.LogDebug(
                "Notifications suppressed for ticket unsnoozed event on ticket {TicketId}",
                notification.Ticket.Id
            );
            return;
        }

        try
        {
            var ticket = notification.Ticket;
            var unsnoozedById = notification.UnsnoozedById;
            var wasAutoUnsnooze = notification.WasAutoUnsnooze;

            // Skip if ticket is closed (per spec: no snooze notification if closed while snoozed)
            var statusConfig = await _db
                .TicketStatusConfigs.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.DeveloperName == ticket.Status,
                    cancellationToken
                );

            if (statusConfig?.IsClosedType == true)
            {
                _logger.LogDebug(
                    "Skipping unsnoozed notification for ticket {TicketId} - ticket is closed",
                    ticket.Id
                );
                return;
            }

            // === COLLECT RECIPIENTS ===

            // 1. Standard recipients (assignee) - respect preferences
            // Always exclude the person who triggered the unsnooze (whether manual or via side effect)
            var standardRecipients = new HashSet<Guid>();
            if (ticket.AssigneeId.HasValue)
            {
                // Only notify assignee if they are NOT the one who triggered the unsnooze
                if (!unsnoozedById.HasValue || ticket.AssigneeId.Value != unsnoozedById.Value)
                {
                    standardRecipients.Add(ticket.AssigneeId.Value);
                }
            }

            // 2. Forced recipients (followers) - bypass preferences
            var followerQuery = _db
                .TicketFollowers.AsNoTracking()
                .Where(f => f.TicketId == ticket.Id);

            // Always exclude the person who triggered the unsnooze (whether manual or via side effect)
            if (unsnoozedById.HasValue)
            {
                followerQuery = followerQuery.Where(f => f.StaffAdminId != unsnoozedById.Value);
            }

            var followerIds = await followerQuery
                .Select(f => f.StaffAdminId)
                .ToListAsync(cancellationToken);

            var forcedRecipients = new HashSet<Guid>(followerIds);

            // === COMBINE ALL RECIPIENTS ===
            var allRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var recipientId in standardRecipients)
                allRecipients.Add(recipientId);

            if (!allRecipients.Any())
            {
                _logger.LogDebug(
                    "No recipients for unsnoozed notification on ticket {TicketId}",
                    ticket.Id
                );
                return;
            }

            // === ALWAYS RECORD TO MY NOTIFICATIONS (database) ===
            var unsnoozedMessage = wasAutoUnsnooze
                ? $"Ticket #{ticket.Id} snooze expired"
                : $"Ticket #{ticket.Id} unsnoozed";

            await _inAppNotificationService.SendToUsersAsync(
                allRecipients,
                NotificationType.TicketUnsnoozed,
                unsnoozedMessage,
                ticket.Title,
                _relativeUrlBuilder.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken
            );

            // === SEND EMAIL NOTIFICATIONS (preference-based) ===

            // Filter standard recipients by their email preferences
            var standardRecipientsFiltered = standardRecipients.Any()
                ? await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                    standardRecipients,
                    NotificationEventType.TICKET_UNSNOOZED,
                    cancellationToken
                )
                : new List<Guid>();

            // Combine: forced recipients always get email, standard only if preferences allow
            var emailRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var recipientId in standardRecipientsFiltered)
                emailRecipients.Add(recipientId);

            if (!emailRecipients.Any())
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketUnsnoozedEmail.DeveloperName
            );

            if (renderTemplate == null)
            {
                _logger.LogWarning(
                    "Email template {TemplateName} not found for unsnoozed notification",
                    BuiltInEmailTemplate.TicketUnsnoozedEmail.DeveloperName
                );
                return;
            }

            // Get unsnoozed by user name for template
            string unsnoozedByName = "System";
            if (!wasAutoUnsnooze && unsnoozedById.HasValue)
            {
                var unsnoozedByUser = await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == unsnoozedById.Value, cancellationToken);
                unsnoozedByName = unsnoozedByUser?.FullName ?? "Unknown";
            }

            // Get assignee name for template
            string assigneeName = "Unassigned";
            if (ticket.AssigneeId.HasValue)
            {
                var assignee = await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == ticket.AssigneeId.Value, cancellationToken);
                assigneeName = assignee?.FullName ?? "Unknown";
            }

            var recipients = await _db
                .Users.AsNoTracking()
                .Where(u => emailRecipients.Contains(u.Id) && !string.IsNullOrEmpty(u.EmailAddress))
                .ToListAsync(cancellationToken);

            foreach (var recipient in recipients)
            {
                var renderModel = new
                {
                    TicketId = ticket.Id,
                    TicketTitle = ticket.Title,
                    WasAutoUnsnooze = wasAutoUnsnooze,
                    UnsnoozedByName = unsnoozedByName,
                    Status = ticket.Status,
                    AssigneeName = assigneeName,
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
                    "Sent ticket unsnoozed email notification for ticket {TicketId} to {Email}",
                    ticket.Id,
                    recipient.EmailAddress
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket unsnoozed notification");
        }
    }
}
