namespace App.Domain.Entities;

/// <summary>
/// Junction entity linking staff to teams with assignment eligibility.
/// </summary>
public class TeamMembership : BaseAuditableEntity
{
    public Guid TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    public Guid StaffAdminId { get; set; }
    public virtual User StaffAdmin { get; set; } = null!;

    /// <summary>
    /// Controls round-robin eligibility. When false, this member is skipped during auto-assignment.
    /// </summary>
    public bool IsAssignable { get; set; } = true;

    /// <summary>
    /// Tracks when this member was last assigned a ticket for round-robin ordering.
    /// </summary>
    public DateTime? LastAssignedAt { get; set; }
}

