namespace App.Web.Areas.Staff.Pages.Shared.Models;

/// <summary>
/// Configuration options for the FileAttachments partial view.
/// </summary>
public class FileAttachmentOptions
{
    /// <summary>
    /// The type of entity to attach files to ("ticket" or "contact").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity to attach files to.
    /// For tickets, this is the numeric ticket ID.
    /// For contacts, this is the numeric contact ID.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Optional unique identifier for this instance of the partial.
    /// Used when multiple file attachment components are on the same page.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether the component is read-only (no upload or delete actions).
    /// </summary>
    public bool ReadOnly { get; set; }
}

