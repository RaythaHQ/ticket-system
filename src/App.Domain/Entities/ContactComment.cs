namespace App.Domain.Entities;

/// <summary>
/// User-added note on a contact.
/// </summary>
public class ContactComment : BaseAuditableEntity
{
    public long ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;

    public Guid AuthorStaffId { get; set; }
    public virtual User AuthorStaff { get; set; } = null!;

    /// <summary>
    /// Comment body, supports rich text.
    /// </summary>
    public string Body { get; set; } = null!;
}

