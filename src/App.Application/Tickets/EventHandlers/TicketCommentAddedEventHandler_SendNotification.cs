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
/// - Ticket assignee (respects their notification preferences)
/// - Ticket followers (bypasses notification preferences)
/// - Mentioned users and team members (bypasses notification preferences)
/// All notifications are deduplicated and the commenter is never notified.
/// </summary>
public class TicketCommentAddedEventHandler_SendNotification
    : BaseTicketNotificationHandler,
        INotificationHandler<TicketCommentAddedEvent>
{
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
        TicketCommentAddedEvent notification,
        CancellationToken cancellationToken
    )
    {
        if (ShouldSuppressNotifications(notification.Ticket.Id, "comment added event"))
            return;

        try
        {
            var ticket = notification.Ticket;
            var comment = notification.Comment;
            var commenterId = comment.AuthorStaffId;

            // === COLLECT RECIPIENTS ===

            // 1. Standard recipients (assignee) - respect preferences
            var standardRecipients = new HashSet<Guid>();
            if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != commenterId)
                standardRecipients.Add(ticket.AssigneeId.Value);

            // 2. Forced recipients (followers, mentions) - bypass preferences
            var forcedRecipients = new HashSet<Guid>();

            // Add ticket followers
            var followerIds = await Db
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
                var teamMemberIds = await Db
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
                Logger.LogDebug(
                    "No recipients for comment notification on ticket {TicketId}",
                    ticket.Id
                );
                return;
            }

            var commenter = await Db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == commenterId, cancellationToken);

            var truncatedBody =
                comment.Body.Length > 100 ? comment.Body.Substring(0, 100) + "..." : comment.Body;

            // === ALWAYS RECORD TO MY NOTIFICATIONS (database) ===
            // InAppNotificationService handles the SignalR popup preference check internally
            await InAppNotificationService.SendToUsersAsync(
                allRecipients,
                NotificationType.CommentAdded,
                $"New comment on #{ticket.Id}",
                $"{commenter?.FullName ?? "Someone"}: {truncatedBody}",
                RelativeUrlBuilder.StaffTicketUrl(ticket.Id),
                ticket.Id,
                cancellationToken
            );

            Logger.LogInformation(
                "Recorded comment notification for ticket {TicketId} to {Count} users",
                ticket.Id,
                allRecipients.Count
            );

            // === SEND EMAIL NOTIFICATIONS (preference-based) ===

            // Filter standard recipients by their email preferences
            var standardRecipientsFiltered = standardRecipients.Any()
                ? await NotificationPreferenceService.FilterUsersWithEmailEnabledAsync(
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

            var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketCommentAddedEmail.DeveloperName
            );

            if (renderTemplate == null)
            {
                Logger.LogWarning(
                    "Email template {TemplateName} not found for comment notification",
                    BuiltInEmailTemplate.TicketCommentAddedEmail.DeveloperName
                );
                return;
            }

            var users = await Db
                .Users.AsNoTracking()
                .Where(u =>
                    emailRecipients.Contains(u.Id)
                    && u.IsActive
                    && !string.IsNullOrEmpty(u.EmailAddress)
                )
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
                    To = new List<string> { user.EmailAddress },
                    Subject = subject,
                };

                await EmailerService.SendEmailAsync(emailMessage, cancellationToken);

                Logger.LogInformation(
                    "Sent comment email notification for ticket {TicketId} to {Email}",
                    ticket.Id,
                    user.EmailAddress
                );
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send ticket comment notification");
        }
    }
}
