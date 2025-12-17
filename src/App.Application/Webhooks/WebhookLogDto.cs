using System.Linq.Expressions;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Application.Webhooks;

/// <summary>
/// Data transfer object for webhook delivery logs.
/// </summary>
public record WebhookLogDto : BaseEntityDto
{
    public ShortGuid WebhookId { get; init; }
    public string WebhookName { get; init; } = null!;
    public long? TicketId { get; init; }
    public string TriggerType { get; init; } = null!;
    public string TriggerTypeLabel { get; init; } = null!;
    public string PayloadJson { get; init; } = null!;
    public int AttemptCount { get; init; }
    public bool Success { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ResponseBody { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan? Duration { get; init; }

    public static WebhookLogDto MapFrom(WebhookLog log)
    {
        return new WebhookLogDto
        {
            Id = log.Id,
            WebhookId = log.WebhookId,
            WebhookName = log.Webhook?.Name ?? "Unknown",
            TicketId = log.TicketId,
            TriggerType = log.TriggerType,
            TriggerTypeLabel = WebhookTriggerType.From(log.TriggerType).Label,
            PayloadJson = log.PayloadJson,
            AttemptCount = log.AttemptCount,
            Success = log.Success,
            HttpStatusCode = log.HttpStatusCode,
            ErrorMessage = log.ErrorMessage,
            ResponseBody = log.ResponseBody,
            CreatedAt = log.CreatedAt,
            CompletedAt = log.CompletedAt,
            Duration = log.Duration,
        };
    }

    public static Expression<Func<WebhookLog, WebhookLogDto>> GetProjection()
    {
        return log => new WebhookLogDto
        {
            Id = log.Id,
            WebhookId = log.WebhookId,
            WebhookName = log.Webhook != null ? log.Webhook.Name : "Unknown",
            TicketId = log.TicketId,
            TriggerType = log.TriggerType,
            TriggerTypeLabel = WebhookTriggerType.From(log.TriggerType).Label,
            PayloadJson = log.PayloadJson,
            AttemptCount = log.AttemptCount,
            Success = log.Success,
            HttpStatusCode = log.HttpStatusCode,
            ErrorMessage = log.ErrorMessage,
            ResponseBody = log.ResponseBody,
            CreatedAt = log.CreatedAt,
            CompletedAt = log.CompletedAt,
            Duration = log.Duration,
        };
    }
}
