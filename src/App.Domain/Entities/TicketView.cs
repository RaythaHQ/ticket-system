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

    public string? SortPrimaryField { get; set; }
    public string? SortPrimaryDirection { get; set; }
    public string? SortSecondaryField { get; set; }
    public string? SortSecondaryDirection { get; set; }

    /// <summary>
    /// JSON array of visible column names in display order.
    /// </summary>
    public string? VisibleColumnsJson { get; set; }

    [NotMapped]
    public List<string> VisibleColumns
    {
        get => string.IsNullOrEmpty(VisibleColumnsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(VisibleColumnsJson) ?? new List<string>();
        set => VisibleColumnsJson = JsonSerializer.Serialize(value);
    }
}

