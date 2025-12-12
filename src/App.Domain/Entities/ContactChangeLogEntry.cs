namespace App.Domain.Entities;

/// <summary>
/// Immutable audit record of contact modifications.
/// Captures field changes with old and new values.
/// </summary>
public class ContactChangeLogEntry : BaseEntity, IHasCreationTime
{
    public long ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The staff member who made the change.
    /// </summary>
    public Guid? ActorStaffId { get; set; }
    public virtual User? ActorStaff { get; set; }

    /// <summary>
    /// JSON object containing field changes: { "FieldName": { "OldValue": "...", "NewValue": "..." } }
    /// </summary>
    public string? FieldChangesJson { get; set; }

    /// <summary>
    /// Optional descriptive message for the change.
    /// </summary>
    public string? Message { get; set; }
}

