namespace App.Domain.Entities;

/// <summary>
/// File attached to a ticket.
/// </summary>
public class TicketAttachment : BaseAuditableEntity
{
    public long TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }

    public Guid UploadedByStaffId { get; set; }
    public virtual User UploadedByStaff { get; set; } = null!;
}

