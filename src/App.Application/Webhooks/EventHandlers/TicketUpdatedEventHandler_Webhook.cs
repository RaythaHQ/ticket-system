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
/// Handles TicketUpdatedEvent by enqueueing webhook deliveries for active webhooks.
/// </summary>
public class TicketUpdatedEventHandler_Webhook : INotificationHandler<TicketUpdatedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IWebhookPayloadBuilder _payloadBuilder;
    private readonly ILogger<TicketUpdatedEventHandler_Webhook> _logger;

    public TicketUpdatedEventHandler_Webhook(
        IAppDbContext db,
        IBackgroundTaskQueue taskQueue,
        IWebhookPayloadBuilder payloadBuilder,
        ILogger<TicketUpdatedEventHandler_Webhook> logger
    )
    {
        _db = db;
        _taskQueue = taskQueue;
        _payloadBuilder = payloadBuilder;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketUpdatedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Only process if there are actual changes
            if (!notification.HasChanges)
            {
                return;
            }

            var activeWebhooks = await _db
                .Webhooks.AsNoTracking()
                .Where(w => w.IsActive && w.TriggerType == WebhookTriggerType.TICKET_UPDATED)
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

            var payload = _payloadBuilder.BuildTicketUpdatedPayload(
                ticket,
                notification.OldTitle,
                notification.OldDescription,
                notification.OldPriority
            );
            var payloadJson = JsonSerializer.Serialize(payload);

            foreach (var webhook in activeWebhooks)
            {
                var args = new WebhookDeliveryArgs
                {
                    WebhookId = webhook.Id,
                    TicketId = ticket.Id,
                    TriggerType = WebhookTriggerType.TICKET_UPDATED,
                    PayloadJson = payloadJson,
                    Url = webhook.Url,
                    IsTest = false,
                };

                await _taskQueue.EnqueueAsync<WebhookDeliveryJob>(args, cancellationToken);

                _logger.LogInformation(
                    "Enqueued webhook delivery for ticket updated event. Webhook: {WebhookName}, Ticket: {TicketId}",
                    webhook.Name,
                    ticket.Id
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling TicketUpdatedEvent for webhooks. Ticket: {TicketId}",
                notification.Ticket.Id
            );
        }
    }
}
