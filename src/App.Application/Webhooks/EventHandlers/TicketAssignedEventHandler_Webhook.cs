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
/// Handles TicketAssignedEvent by enqueueing webhook deliveries for active webhooks.
/// </summary>
public class TicketAssignedEventHandler_Webhook : INotificationHandler<TicketAssignedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IWebhookPayloadBuilder _payloadBuilder;
    private readonly ILogger<TicketAssignedEventHandler_Webhook> _logger;

    public TicketAssignedEventHandler_Webhook(
        IAppDbContext db,
        IBackgroundTaskQueue taskQueue,
        IWebhookPayloadBuilder payloadBuilder,
        ILogger<TicketAssignedEventHandler_Webhook> logger
    )
    {
        _db = db;
        _taskQueue = taskQueue;
        _payloadBuilder = payloadBuilder;
        _logger = logger;
    }

    public async ValueTask Handle(
        TicketAssignedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var activeWebhooks = await _db
                .Webhooks.AsNoTracking()
                .Where(w =>
                    w.IsActive && w.TriggerType == WebhookTriggerType.TICKET_ASSIGNEE_CHANGED
                )
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

            // We need to look up the old assignee/team names
            string? oldAssigneeName = null;
            string? oldTeamName = null;

            if (notification.OldAssigneeId.HasValue)
            {
                oldAssigneeName = await _db
                    .Users.AsNoTracking()
                    .Where(u => u.Id == notification.OldAssigneeId.Value)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (notification.OldTeamId.HasValue)
            {
                oldTeamName = await _db
                    .Teams.AsNoTracking()
                    .Where(t => t.Id == notification.OldTeamId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var payload = _payloadBuilder.BuildTicketAssigneeChangedPayload(
                ticket,
                notification.OldAssigneeId,
                oldAssigneeName,
                notification.OldTeamId,
                oldTeamName
            );
            var payloadJson = JsonSerializer.Serialize(payload);

            foreach (var webhook in activeWebhooks)
            {
                var args = new WebhookDeliveryArgs
                {
                    WebhookId = webhook.Id,
                    TicketId = ticket.Id,
                    TriggerType = WebhookTriggerType.TICKET_ASSIGNEE_CHANGED,
                    PayloadJson = payloadJson,
                    Url = webhook.Url,
                    IsTest = false,
                };

                await _taskQueue.EnqueueAsync<WebhookDeliveryJob>(args, cancellationToken);

                _logger.LogInformation(
                    "Enqueued webhook delivery for ticket assignee changed event. Webhook: {WebhookName}, Ticket: {TicketId}",
                    webhook.Name,
                    ticket.Id
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling TicketAssignedEvent for webhooks. Ticket: {TicketId}",
                notification.Ticket.Id
            );
        }
    }
}
