using App.Domain.ValueObjects;

namespace App.Domain.Entities;

/// <summary>
/// Configuration entity for ticket statuses.
/// Supports ordering, colors, and status type (Open/Closed) categorization.
/// </summary>
public class TicketStatusConfig : BaseAuditableEntity
{
    /// <summary>
    /// Display label shown in UI (e.g., "New", "In Progress", "Pending", "Closed").
    /// </summary>
    public string Label { get; set; } = null!;

    /// <summary>
    /// Unique identifier used in API, database storage, and code references.
    /// Cannot be changed after creation for custom statuses.
    /// </summary>
    public string DeveloperName { get; set; } = null!;

    /// <summary>
    /// Bootstrap contextual color name (primary, secondary, success, danger, warning, info, light, dark).
    /// Used for badges and visual indicators throughout the app.
    /// </summary>
    public string ColorName { get; set; } = "secondary";

    /// <summary>
    /// Order for display in dropdowns and for sorting tickets.
    /// The status with SortOrder = 1 (top) is used as the default for new tickets.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The type/category of this status: "open" or "closed".
    /// Determines business logic like SLA tracking and metrics calculations.
    /// The top status (SortOrder = 1) must be of type "open".
    /// </summary>
    public string StatusType { get; set; } = TicketStatusType.OPEN;

    /// <summary>
    /// When true, this is a system-provided status that cannot be deleted.
    /// Built-in statuses can have their label, color, and type edited, but not developer name.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// When false, this status is hidden from new ticket creation but still valid for existing tickets.
    /// Users editing tickets with inactive statuses will be required to select a new status.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Returns true if this status represents an open (not closed) ticket.
    /// </summary>
    public bool IsOpenType => StatusType == TicketStatusType.OPEN;

    /// <summary>
    /// Returns true if this status represents a closed ticket.
    /// </summary>
    public bool IsClosedType => StatusType == TicketStatusType.CLOSED;
}

