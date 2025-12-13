namespace App.Domain.Entities;

/// <summary>
/// File attached to a contact.
/// Links to a MediaItem for actual file storage.
/// </summary>
public class ContactAttachment : BaseAuditableEntity
{
    public long ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;

    /// <summary>
    /// Reference to the MediaItem containing the actual file.
    /// </summary>
    public Guid MediaItemId { get; set; }
    public virtual MediaItem MediaItem { get; set; } = null!;

    /// <summary>
    /// Display name for the attachment (defaults to original filename).
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Optional description or notes about the attachment.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Who uploaded this attachment.
    /// </summary>
    public Guid UploadedByUserId { get; set; }
    public virtual User UploadedByUser { get; set; } = null!;
}

