using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Webhooks.Commands;
using App.Application.Webhooks.Services;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Webhooks.EventHandlers;

/// <summary>
/// Handles TicketCommentAddedEvent by enqueueing webhook deliveries for active webhooks.
/// </summary>
public class TicketCommentAddedEventHandler_Webhook : INotificationHandler<TicketCommentAddedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IWebhookPayloadBuilder _payloadBuilder;
    private readonly ILogger<TicketCommentAddedEventHandler_Webhook> _logger;

    public TicketCommentAddedEventHandler_Webhook(
        IAppDbContext db,
        IBackgroundTaskQueue taskQueue,
        IWebhookPayloadBuilder payloadBuilder,
        ILogger<TicketCommentAddedEventHandler_Webhook> logger
    )
    {
        _db = db;
        _taskQueue = taskQueue;
        _payloadBuilder = payloadBuilder;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketCommentAddedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var activeWebhooks = await _db
                .Webhooks.AsNoTracking()
                .Where(w => w.IsActive && w.TriggerType == WebhookTriggerType.COMMENT_ADDED)
                .ToListAsync(cancellationToken);

            if (!activeWebhooks.Any())
            {
                return;
            }

            // Reload ticket with navigation properties for complete payload
            var ticket = await _db
                .Tickets.AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.Contact)
                .FirstOrDefaultAsync(t => t.Id == notification.Ticket.Id, cancellationToken);

            if (ticket == null)
            {
                _logger.LogWarning(
                    "Ticket {TicketId} not found when building webhook payload",
                    notification.Ticket.Id
                );
                return;
            }

            // Reload comment with author
            var comment = await _db
                .TicketComments.AsNoTracking()
                .Include(c => c.AuthorStaff)
                .FirstOrDefaultAsync(c => c.Id == notification.Comment.Id, cancellationToken);

            if (comment == null)
            {
                _logger.LogWarning(
                    "Comment {CommentId} not found when building webhook payload",
                    notification.Comment.Id
                );
                return;
            }

            var payload = _payloadBuilder.BuildCommentAddedPayload(ticket, comment);
            var payloadJson = JsonSerializer.Serialize(payload);

            foreach (var webhook in activeWebhooks)
            {
                var args = new WebhookDeliveryArgs
                {
                    WebhookId = webhook.Id,
                    TicketId = ticket.Id,
                    TriggerType = WebhookTriggerType.COMMENT_ADDED,
                    PayloadJson = payloadJson,
                    Url = webhook.Url,
                    IsTest = false,
                };

                await _taskQueue.EnqueueAsync<WebhookDeliveryJob>(args, cancellationToken);

                _logger.LogInformation(
                    "Enqueued webhook delivery for comment added event. Webhook: {WebhookName}, Ticket: {TicketId}, Comment: {CommentId}",
                    webhook.Name,
                    ticket.Id,
                    comment.Id
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling TicketCommentAddedEvent for webhooks. Ticket: {TicketId}",
                notification.Ticket.Id
            );
        }
    }
}
