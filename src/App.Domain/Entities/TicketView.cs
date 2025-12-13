using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace App.Domain.Entities;

/// <summary>
/// Saved filter configuration for ticket lists.
/// </summary>
public class TicketView : BaseAuditableEntity
{
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description for the view.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Owner of this view. Null for system-wide default views.
    /// </summary>
    public Guid? OwnerStaffId { get; set; }
    public virtual User? OwnerStaff { get; set; }

    /// <summary>
    /// When true, this is a default view shown to all users.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// When true, this is a system-provided view that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// JSON object containing AND/OR filter conditions.
    /// </summary>
    public string? ConditionsJson { get; set; }

    /// <summary>
    /// Legacy: Primary sort field. Prefer SortLevelsJson for multi-level sorting.
    /// </summary>
    public string? SortPrimaryField { get; set; }
    public string? SortPrimaryDirection { get; set; }
    public string? SortSecondaryField { get; set; }
    public string? SortSecondaryDirection { get; set; }

    /// <summary>
    /// JSON array of sort levels for multi-level sorting.
    /// Each level contains: Order, Field, Direction (asc/desc).
    /// </summary>
    public string? SortLevelsJson { get; set; }

    /// <summary>
    /// JSON array of visible column names in display order.
    /// </summary>
    public string? VisibleColumnsJson { get; set; }

    /// <summary>
    /// Multi-level sort configuration. Falls back to legacy sort fields if SortLevelsJson is empty.
    /// </summary>
    [NotMapped]
    public List<ViewSortLevel> SortLevels
    {
        get
        {
            if (!string.IsNullOrEmpty(SortLevelsJson))
            {
                return JsonSerializer.Deserialize<List<ViewSortLevel>>(SortLevelsJson) ?? new List<ViewSortLevel>();
            }
            // Legacy fallback: migrate from SortPrimaryField/SortSecondaryField
            return MigrateLegacySort();
        }
        set => SortLevelsJson = JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Migrates legacy sort fields to multi-level sort structure.
    /// </summary>
    private List<ViewSortLevel> MigrateLegacySort()
    {
        var levels = new List<ViewSortLevel>();
        if (!string.IsNullOrEmpty(SortPrimaryField))
        {
            levels.Add(new ViewSortLevel
            {
                Order = 0,
                Field = SortPrimaryField,
                Direction = SortPrimaryDirection ?? "asc"
            });
        }
        if (!string.IsNullOrEmpty(SortSecondaryField))
        {
            levels.Add(new ViewSortLevel
            {
                Order = 1,
                Field = SortSecondaryField,
                Direction = SortSecondaryDirection ?? "asc"
            });
        }
        return levels;
    }

    [NotMapped]
    public List<string> VisibleColumns
    {
        get => string.IsNullOrEmpty(VisibleColumnsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(VisibleColumnsJson) ?? new List<string>();
        set => VisibleColumnsJson = JsonSerializer.Serialize(value);
    }
}

/// <summary>
/// Represents a single level in multi-level sorting.
/// </summary>
public record ViewSortLevel
{
    /// <summary>
    /// Sort order (0 = primary, 1 = secondary, etc.)
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public string Field { get; init; } = null!;

    /// <summary>
    /// Sort direction: "asc" or "desc".
    /// </summary>
    public string Direction { get; init; } = "asc";
}

