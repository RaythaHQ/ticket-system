namespace App.Domain.Entities;

/// <summary>
/// Represents a functional group for ticket assignment, round-robin distribution, and reporting.
/// </summary>
public class Team : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool RoundRobinEnabled { get; set; }

    // Collections
    public virtual ICollection<TeamMembership> Memberships { get; set; } = new List<TeamMembership>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

