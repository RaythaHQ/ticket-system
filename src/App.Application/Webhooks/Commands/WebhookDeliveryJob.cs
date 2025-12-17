using System.Text.Json;
using App.Application.Common.Interfaces;

namespace App.Application.Webhooks.Commands;

/// <summary>
/// Background job interface for webhook delivery.
/// Implemented in Infrastructure layer with Polly retry logic.
/// </summary>
public abstract class WebhookDeliveryJob : IExecuteBackgroundTask
{
    public abstract Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken);
}

/// <summary>
/// Arguments for enqueueing a webhook delivery job.
/// </summary>
public record WebhookDeliveryArgs
{
    /// <summary>
    /// The webhook configuration ID.
    /// </summary>
    public Guid WebhookId { get; init; }

    /// <summary>
    /// The ticket ID that triggered this webhook.
    /// </summary>
    public long? TicketId { get; init; }

    /// <summary>
    /// The trigger type for this delivery.
    /// </summary>
    public string TriggerType { get; init; } = null!;

    /// <summary>
    /// The JSON payload to send.
    /// </summary>
    public string PayloadJson { get; init; } = null!;

    /// <summary>
    /// The target URL to POST to.
    /// </summary>
    public string Url { get; init; } = null!;

    /// <summary>
    /// Whether this is a test delivery (doesn't count toward real metrics).
    /// </summary>
    public bool IsTest { get; init; }
}
