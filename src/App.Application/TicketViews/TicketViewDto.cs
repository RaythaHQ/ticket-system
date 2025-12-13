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
    
    /// <summary>
    /// Multi-level sort configuration.
    /// </summary>
    public List<ViewSortLevelDto> SortLevels { get; init; } = new();
    
    public List<string> VisibleColumns { get; init; } = new();
    public List<string> Columns => VisibleColumns;
    public int FilterCount => Conditions?.Filters?.Count ?? 0;
    public int ColumnCount => VisibleColumns.Count;
    public int SortLevelCount => SortLevels.Count;
    public List<ViewFilterCondition> Filters => Conditions?.Filters ?? new();
    
    /// <summary>
    /// Formatted sort string for display (e.g., "Priority ↓, Created ↑").
    /// </summary>
    public string SortDisplay => SortLevels.Count > 0
        ? string.Join(", ", SortLevels.OrderBy(s => s.Order).Select(s => $"{s.FieldLabel} {(s.Direction == "desc" ? "↓" : "↑")}"))
        : string.Empty;

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

        // Map sort levels from entity
        var sortLevels = view.SortLevels.Select(s => new ViewSortLevelDto
        {
            Order = s.Order,
            Field = s.Field,
            Direction = s.Direction,
            FieldLabel = GetFieldLabel(s.Field)
        }).ToList();

        return new TicketViewDto
        {
            Id = view.Id,
            Name = view.Name,
            Description = view.Description,
            OwnerStaffId = view.OwnerStaffId,
            OwnerStaffName = view.OwnerStaff?.FullName,
            IsDefault = view.IsDefault,
            IsSystem = view.IsSystem,
            Conditions = conditions,
            SortPrimaryField = view.SortPrimaryField,
            SortPrimaryDirection = view.SortPrimaryDirection,
            SortSecondaryField = view.SortSecondaryField,
            SortSecondaryDirection = view.SortSecondaryDirection,
            SortLevels = sortLevels,
            VisibleColumns = view.VisibleColumns,
            CreationTime = view.CreationTime,
            LastModificationTime = view.LastModificationTime
        };
    }

    private static string GetFieldLabel(string field)
    {
        return field switch
        {
            "Id" => "ID",
            "Title" => "Title",
            "Status" => "Status",
            "Priority" => "Priority",
            "Category" => "Category",
            "CreationTime" => "Created",
            "LastModificationTime" => "Updated",
            "ClosedAt" => "Closed",
            "SlaDueAt" => "SLA Due",
            "AssigneeName" => "Assignee",
            "OwningTeamName" => "Team",
            "ContactName" => "Contact",
            _ => field
        };
    }
}

/// <summary>
/// DTO for a single sort level.
/// </summary>
public record ViewSortLevelDto
{
    public int Order { get; init; }
    public string Field { get; init; } = null!;
    public string Direction { get; init; } = "asc";
    public string FieldLabel { get; init; } = null!;
}

/// <summary>
/// Filter conditions for a ticket view. Supports AND/OR logic.
/// </summary>
public record ViewConditions
{
    public string Logic { get; init; } = "AND"; // Legacy, kept for backward compat
    public List<ViewFilterCondition> Filters { get; init; } = new(); // Legacy

    /// <summary>
    /// Conditions that must ALL match (AND logic).
    /// </summary>
    public List<ViewFilterCondition> AndFilters { get; init; } = new();

    /// <summary>
    /// Conditions where at least ONE must match (OR logic).
    /// Only applied after AndFilters. Result: (ANDs) && (any OR).
    /// </summary>
    public List<ViewFilterCondition> OrFilters { get; init; } = new();
}

/// <summary>
/// Individual filter condition with type-aware operators and values.
/// </summary>
public record ViewFilterCondition
{
    /// <summary>
    /// Field/attribute to filter on (e.g., "Status", "CreationTime", "Title").
    /// </summary>
    public string Field { get; init; } = null!;

    /// <summary>
    /// Operator to apply. Valid operators depend on field type.
    /// String: eq, neq, contains, not_contains, starts_with, not_starts_with, ends_with, not_ends_with, is_empty, is_not_empty
    /// Date: is, is_within, is_before, is_after, is_on_or_before, is_on_or_after, is_empty, is_not_empty
    /// Boolean: is_true, is_false
    /// Numeric: eq, neq, gt, lt, gte, lte, is_empty, is_not_empty
    /// Selection: is, is_not, is_any_of, is_none_of
    /// Priority: is, is_not, gt, lt, gte, lte (based on SortOrder)
    /// User: is, is_not, is_any_of, is_none_of, is_empty, is_not_empty
    /// </summary>
    public string Operator { get; init; } = "eq";

    /// <summary>
    /// Single value for operators like eq, contains, etc.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Multiple values for operators like is_any_of, is_none_of.
    /// </summary>
    public List<string>? Values { get; init; }

    /// <summary>
    /// For date fields: "exact", "relative", or null.
    /// </summary>
    public string? DateType { get; init; }

    /// <summary>
    /// For relative dates: the unit (days, weeks, months).
    /// </summary>
    public string? RelativeDateUnit { get; init; }

    /// <summary>
    /// For relative dates: the value (negative for past, positive for future).
    /// e.g., -7 with unit "days" = 7 days ago.
    /// </summary>
    public int? RelativeDateValue { get; init; }

    /// <summary>
    /// For relative dates: preset like "today", "yesterday", "this_week", "last_week", etc.
    /// </summary>
    public string? RelativeDatePreset { get; init; }
}

