using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.SchedulerAdmin.DTOs;

/// <summary>
/// DTO for scheduler email/SMS templates.
/// </summary>
public record SchedulerEmailTemplateDto
{
    public ShortGuid Id { get; init; }
    public string TemplateType { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }

    public static SchedulerEmailTemplateDto MapFrom(SchedulerEmailTemplate template)
    {
        return new SchedulerEmailTemplateDto
        {
            Id = template.Id,
            TemplateType = template.TemplateType,
            Channel = template.Channel,
            Subject = template.Subject,
            Content = template.Content,
            IsActive = template.IsActive,
            CreationTime = template.CreationTime,
            LastModificationTime = template.LastModificationTime,
        };
    }
}
