using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Application.TicketConfig;

/// <summary>
/// Data transfer object for ticket status configuration.
/// </summary>
public record TicketStatusConfigDto : BaseAuditableEntityDto
{
    public string Label { get; init; } = null!;
    public string DeveloperName { get; init; } = null!;
    public string ColorName { get; init; } = "secondary";
    public int SortOrder { get; init; }
    public string StatusType { get; init; } = TicketStatusType.OPEN;
    public bool IsBuiltIn { get; init; }
    public bool IsActive { get; init; }

    /// <summary>
    /// True if this status represents an open (not closed) ticket.
    /// </summary>
    public bool IsOpenType => StatusType == TicketStatusType.OPEN;

    /// <summary>
    /// True if this status represents a closed ticket.
    /// </summary>
    public bool IsClosedType => StatusType == TicketStatusType.CLOSED;

    /// <summary>
    /// Human-readable status type label ("Open" or "Closed").
    /// </summary>
    public string StatusTypeLabel => IsOpenType ? "Open" : "Closed";

    /// <summary>
    /// Returns the Bootstrap badge CSS class for this status.
    /// </summary>
    public string BadgeClass => $"bg-{ColorName}";

    /// <summary>
    /// Returns the Staff area badge CSS class for this status.
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

    public static TicketStatusConfigDto MapFrom(TicketStatusConfig entity)
    {
        return new TicketStatusConfigDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            ColorName = entity.ColorName,
            SortOrder = entity.SortOrder,
            StatusType = entity.StatusType,
            IsBuiltIn = entity.IsBuiltIn,
            IsActive = entity.IsActive,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime
        };
    }
}

