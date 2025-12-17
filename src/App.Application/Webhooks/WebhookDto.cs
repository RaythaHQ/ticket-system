using System.Linq.Expressions;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Application.Webhooks;

/// <summary>
/// Data transfer object for webhook configurations.
/// </summary>
public record WebhookDto : BaseAuditableEntityDto
{
    public string Name { get; init; } = null!;
    public string Url { get; init; } = null!;
    public string TriggerType { get; init; } = null!;
    public string TriggerTypeLabel { get; init; } = null!;
    public bool IsActive { get; init; }
    public string? Description { get; init; }

    public static WebhookDto MapFrom(Webhook webhook)
    {
        return new WebhookDto
        {
            Id = webhook.Id,
            Name = webhook.Name,
            Url = webhook.Url,
            TriggerType = webhook.TriggerType,
            TriggerTypeLabel = webhook.TriggerTypeValue.Label,
            IsActive = webhook.IsActive,
            Description = webhook.Description,
            CreationTime = webhook.CreationTime,
            CreatorUserId = webhook.CreatorUserId,
            LastModifierUserId = webhook.LastModifierUserId,
            LastModificationTime = webhook.LastModificationTime,
        };
    }

    public static Expression<Func<Webhook, WebhookDto>> GetProjection()
    {
        return webhook => new WebhookDto
        {
            Id = webhook.Id,
            Name = webhook.Name,
            Url = webhook.Url,
            TriggerType = webhook.TriggerType,
            TriggerTypeLabel = WebhookTriggerType.From(webhook.TriggerType).Label,
            IsActive = webhook.IsActive,
            Description = webhook.Description,
            CreationTime = webhook.CreationTime,
            CreatorUserId = webhook.CreatorUserId,
            LastModifierUserId = webhook.LastModifierUserId,
            LastModificationTime = webhook.LastModificationTime,
        };
    }
}
