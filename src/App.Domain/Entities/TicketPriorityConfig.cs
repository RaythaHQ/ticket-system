namespace App.Domain.Entities;

/// <summary>
/// Configuration entity for ticket priorities.
/// Supports ordering, colors, and default selection.
/// </summary>
public class TicketPriorityConfig : BaseAuditableEntity
{
    /// <summary>
    /// Display label shown in UI (e.g., "Urgent", "High", "Normal", "Low").
    /// </summary>
    public string Label { get; set; } = null!;

    /// <summary>
    /// Unique identifier used in API, database storage, and code references.
    /// Cannot be changed after creation for custom priorities.
    /// </summary>
    public string DeveloperName { get; set; } = null!;

    /// <summary>
    /// Bootstrap contextual color name (primary, secondary, success, danger, warning, info, light, dark).
    /// Used for badges and visual indicators throughout the app.
    /// </summary>
    public string ColorName { get; set; } = "secondary";

    /// <summary>
    /// Order for display in dropdowns and for sorting tickets.
    /// Lower numbers appear first. Higher priority items typically have lower sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// When true, this priority is used as the default for new tickets.
    /// Only one priority can be the default at a time.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// When true, this is a system-provided priority that cannot be deleted.
    /// Built-in priorities can have their label and color edited, but not developer name.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// When false, this priority is hidden from new ticket creation but still valid for existing tickets.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

