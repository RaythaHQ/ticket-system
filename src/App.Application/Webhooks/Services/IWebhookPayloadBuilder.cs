using App.Domain.Entities;

namespace App.Application.Webhooks.Services;

/// <summary>
/// Service for building webhook payloads from domain events.
/// </summary>
public interface IWebhookPayloadBuilder
{
    /// <summary>
    /// Builds a payload for a ticket created event.
    /// </summary>
    WebhookPayloadDto BuildTicketCreatedPayload(Ticket ticket);

    /// <summary>
    /// Builds a payload for a ticket updated event (title/description/priority changes).
    /// </summary>
    WebhookPayloadDto BuildTicketUpdatedPayload(
        Ticket ticket,
        string? oldTitle,
        string? oldDescription,
        string? oldPriority
    );

    /// <summary>
    /// Builds a payload for a ticket status changed event.
    /// </summary>
    WebhookPayloadDto BuildTicketStatusChangedPayload(Ticket ticket, string oldStatus);

    /// <summary>
    /// Builds a payload for a ticket assignee changed event.
    /// </summary>
    WebhookPayloadDto BuildTicketAssigneeChangedPayload(
        Ticket ticket,
        Guid? oldAssigneeId,
        string? oldAssigneeName,
        Guid? oldTeamId,
        string? oldTeamName
    );

    /// <summary>
    /// Builds a payload for a comment added event.
    /// </summary>
    WebhookCommentPayloadDto BuildCommentAddedPayload(Ticket ticket, TicketComment comment);

    /// <summary>
    /// Builds a test payload for testing webhook delivery.
    /// </summary>
    WebhookPayloadDto BuildTestPayload(string triggerType);
}
