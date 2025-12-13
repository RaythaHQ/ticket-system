using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.TicketConfig;

/// <summary>
/// Data transfer object for ticket priority configuration.
/// </summary>
public record TicketPriorityConfigDto : BaseAuditableEntityDto
{
    public string Label { get; init; } = null!;
    public string DeveloperName { get; init; } = null!;
    public string ColorName { get; init; } = "secondary";
    public int SortOrder { get; init; }
    public bool IsDefault { get; init; }
    public bool IsBuiltIn { get; init; }
    public bool IsActive { get; init; }

    /// <summary>
    /// Returns the Bootstrap badge CSS class for this priority.
    /// </summary>
    public string BadgeClass => $"bg-{ColorName}";

    /// <summary>
    /// Returns the Staff area badge CSS class for this priority.
    /// </summary>
    public string StaffBadgeClass => ColorName switch
    {
        "danger" => "staff-badge-danger",
        "warning" => "staff-badge-warning",
        "primary" => "staff-badge-primary",
        "success" => "staff-badge-success",
        "info" => "staff-badge-primary",
        "secondary" => "staff-badge-secondary",
        "light" => "staff-badge-secondary",
        "dark" => "staff-badge-secondary",
        _ => "staff-badge-secondary"
    };

    public static TicketPriorityConfigDto MapFrom(TicketPriorityConfig entity)
    {
        return new TicketPriorityConfigDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            ColorName = entity.ColorName,
            SortOrder = entity.SortOrder,
            IsDefault = entity.IsDefault,
            IsBuiltIn = entity.IsBuiltIn,
            IsActive = entity.IsActive,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime
        };
    }
}

