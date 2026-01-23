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
/// Sends email and in-app notification when a comment is added to a ticket.
/// Notifies:
/// - Ticket assignee and creator (respects their notification preferences)
/// - Ticket followers (bypasses notification preferences)
/// - Mentioned users and team members (bypasses notification preferences)
/// All notifications are deduplicated and the commenter is never notified.
/// </summary>
public class TicketCommentAddedEventHandler_SendNotification
    : INotificationHandler<TicketCommentAddedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly INotificationSuppressionService _notificationSuppressionService;
    private readonly ILogger<TicketCommentAddedEventHandler_SendNotification> _logger;

    public TicketCommentAddedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger<TicketCommentAddedEventHandler_SendNotification> logger
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
        TicketCommentAddedEvent notification,
        CancellationToken cancellationToken
    )
    {
        // Check if notifications should be suppressed
        if (_notificationSuppressionService.ShouldSuppressNotifications())
        {
            _logger.LogDebug(
                "Notifications suppressed for comment added event on ticket {TicketId}",
                notification.Ticket.Id
            );
            return;
        }

        try
        {
            var ticket = notification.Ticket;
            var comment = notification.Comment;
            var commenterId = comment.AuthorStaffId;

            // === COLLECT RECIPIENTS ===

            // 1. Standard recipients (assignee, creator) - respect preferences
            var standardRecipients = new HashSet<Guid>();
            if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != commenterId)
                standardRecipients.Add(ticket.AssigneeId.Value);
            if (ticket.CreatedByStaffId.HasValue && ticket.CreatedByStaffId.Value != commenterId)
                standardRecipients.Add(ticket.CreatedByStaffId.Value);

            // 2. Forced recipients (followers, mentions) - bypass preferences
            var forcedRecipients = new HashSet<Guid>();

            // Add ticket followers
            var followerIds = await _db
                .TicketFollowers.AsNoTracking()
                .Where(f => f.TicketId == ticket.Id && f.StaffAdminId != commenterId)
                .Select(f => f.StaffAdminId)
                .ToListAsync(cancellationToken);
            foreach (var followerId in followerIds)
                forcedRecipients.Add(followerId);

            // Add mentioned users
            foreach (var userId in notification.MentionedUserIds.Where(id => id != commenterId))
                forcedRecipients.Add(userId);

            // Add members of mentioned teams
            if (notification.MentionedTeamIds.Any())
            {
                var teamMemberIds = await _db
                    .TeamMemberships.AsNoTracking()
                    .Where(m =>
                        notification.MentionedTeamIds.Contains(m.TeamId)
                        && m.StaffAdminId != commenterId
                    )
                    .Select(m => m.StaffAdminId)
                    .ToListAsync(cancellationToken);
                foreach (var memberId in teamMemberIds)
                    forcedRecipients.Add(memberId);
            }

            // === COMBINE ALL RECIPIENTS ===
            // All recipients (both forced and standard) should have notifications recorded
            var allRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var id in standardRecipients)
                allRecipients.Add(id);

            if (!allRecipients.Any())
            {
                _logger.LogDebug(
                    "No recipients for comment notification on ticket {TicketId}",
                    ticket.Id
                );
                return;
            }

            var commenter = await _db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == commenterId, cancellationToken);

            var truncatedBody =
                comment.Body.Length > 100
                    ? comment.Body.Substring(0, 100) + "..."
                    : comment.Body;

            // === ALWAYS RECORD TO MY NOTIFICATIONS (database) ===
            // InAppNotificationService handles the SignalR popup preference check internally
            await _inAppNotificationService.SendToUsersAsync(
                allRecipients,
                NotificationType.CommentAdded,
                $"New comment on #{ticket.Id}",
                $"{commenter?.FullName ?? "Someone"}: {truncatedBody}",
                _relativeUrlBuilder.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken
            );

            _logger.LogInformation(
                "Recorded comment notification for ticket {TicketId} to {Count} users",
                ticket.Id,
                allRecipients.Count
            );

            // === SEND EMAIL NOTIFICATIONS (preference-based) ===

            // Filter standard recipients by their email preferences
            var standardRecipientsFiltered = standardRecipients.Any()
                ? await _notificationPreferenceService.FilterUsersWithEmailEnabledAsync(
                    standardRecipients,
                    NotificationEventType.COMMENT_ADDED,
                    cancellationToken
                )
                : new List<Guid>();

            // Combine: forced recipients always get email, standard only if preferences allow
            var emailRecipients = new HashSet<Guid>(forcedRecipients);
            foreach (var id in standardRecipientsFiltered)
                emailRecipients.Add(id);

            if (!emailRecipients.Any())
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketCommentAddedEmail.DeveloperName
            );

            if (renderTemplate == null)
            {
                _logger.LogWarning(
                    "Email template {TemplateName} not found for comment notification",
                    BuiltInEmailTemplate.TicketCommentAddedEmail.DeveloperName
                );
                return;
            }

            var users = await _db
                .Users.AsNoTracking()
                .Where(u => emailRecipients.Contains(u.Id) && u.IsActive && !string.IsNullOrEmpty(u.EmailAddress))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                var renderModel = new TicketCommentAdded_RenderModel
                {
                    TicketId = ticket.Id,
                    Title = ticket.Title,
                    CommentAuthor = commenter?.FullName ?? "Unknown",
                    CommentBody = comment.Body,
                    RecipientName = user.FullName,
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
                    To = new List<string> { user.EmailAddress },
                    Subject = subject,
                };

                await _emailerService.SendEmailAsync(emailMessage, cancellationToken);

                _logger.LogInformation(
                    "Sent comment email notification for ticket {TicketId} to {Email}",
                    ticket.Id,
                    user.EmailAddress
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket comment notification");
        }
    }
}
