namespace App.Domain.Entities;

/// <summary>
/// User-added note on a ticket.
/// </summary>
public class TicketComment : BaseAuditableEntity
{
    public long TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    public Guid AuthorStaffId { get; set; }
    public virtual User AuthorStaff { get; set; } = null!;

    /// <summary>
    /// Comment body, supports rich text.
    /// </summary>
    public string Body { get; set; } = null!;
}

