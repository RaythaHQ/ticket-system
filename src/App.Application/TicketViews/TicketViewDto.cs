using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using System.Text.Json;

namespace App.Application.TicketViews;

/// <summary>
/// Ticket view data transfer object.
/// </summary>
public record TicketViewDto : BaseAuditableEntityDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Guid? OwnerUserId { get; init; }
    public ShortGuid? OwnerStaffId { get; init; }
    public string? OwnerStaffName { get; init; }
    public bool IsDefault { get; init; }
    public bool IsSystem { get; init; }
    public bool IsSystemView => IsSystem;
    public ViewConditions? Conditions { get; init; }
    public string? SortPrimaryField { get; init; }
    public string? SortField => SortPrimaryField;
    public string? SortPrimaryDirection { get; init; }
    public string? SortDirection => SortPrimaryDirection;
    public string? SortSecondaryField { get; init; }
    public string? SortSecondaryDirection { get; init; }
    public List<string> VisibleColumns { get; init; } = new();
    public List<string> Columns => VisibleColumns;
    public int FilterCount => Conditions?.Filters?.Count ?? 0;
    public int ColumnCount => VisibleColumns.Count;
    public List<ViewFilterCondition> Filters => Conditions?.Filters ?? new();

    public static TicketViewDto MapFrom(TicketView view)
    {
        ViewConditions? conditions = null;
        if (!string.IsNullOrEmpty(view.ConditionsJson))
        {
            try
            {
                conditions = JsonSerializer.Deserialize<ViewConditions>(view.ConditionsJson);
            }
            catch { }
        }

        return new TicketViewDto
        {
            Id = view.Id,
            Name = view.Name,
            Description = view.Description,
            OwnerUserId = view.OwnerStaffId,
            OwnerStaffId = view.OwnerStaffId,
            OwnerStaffName = view.OwnerStaff?.FullName,
            IsDefault = view.IsDefault,
            IsSystem = view.IsSystem,
            Conditions = conditions,
            SortPrimaryField = view.SortPrimaryField,
            SortPrimaryDirection = view.SortPrimaryDirection,
            SortSecondaryField = view.SortSecondaryField,
            SortSecondaryDirection = view.SortSecondaryDirection,
            VisibleColumns = view.VisibleColumns,
            CreationTime = view.CreationTime,
            LastModificationTime = view.LastModificationTime
        };
    }
}

/// <summary>
/// Filter conditions for a ticket view. Supports AND/OR logic.
/// </summary>
public record ViewConditions
{
    public string Logic { get; init; } = "AND"; // AND or OR
    public List<ViewFilterCondition> Filters { get; init; } = new();
}

/// <summary>
/// Individual filter condition.
/// </summary>
public record ViewFilterCondition
{
    public string Field { get; init; } = null!;
    public string Operator { get; init; } = "equals"; // equals, contains, gt, lt, gte, lte, in, notin, isnull, isnotnull
    public string? Value { get; init; }
    public List<string>? Values { get; init; }
}

