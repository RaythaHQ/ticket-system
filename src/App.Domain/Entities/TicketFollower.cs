namespace App.Domain.Entities;

/// <summary>
/// Tracks users who are following a ticket's comment thread.
/// Followers receive notifications when comments are added, regardless of their notification preferences.
/// </summary>
public class TicketFollower : BaseAuditableEntity
{
    public long TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    public Guid StaffAdminId { get; set; }
    public virtual User StaffAdmin { get; set; } = null!;
}
