using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.ValueObjects;

namespace App.Domain.Entities;

/// <summary>
/// Represents a discrete piece of work subordinate to a Ticket.
/// Tasks have two statuses (Open/Closed) with a derived Blocked state
/// based on dependency relationships.
/// </summary>
public class TicketTask : BaseFullAuditableEntity
{
    /// <summary>
    /// Parent ticket ID. Tasks cannot exist independently of a ticket.
    /// </summary>
    public long TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    /// <summary>
    /// Task title — the only content field. Max 500 chars.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Stored as DeveloperName from TicketTaskStatus ("open" or "closed").
    /// </summary>
    public string Status { get; set; } = TicketTaskStatus.OPEN;

    /// <summary>
    /// Assigned user. When set, OwningTeamId is inferred from membership.
    /// </summary>
    public Guid? AssigneeId { get; set; }
    public virtual User? Assignee { get; set; }

    /// <summary>
    /// Assigned team. Auto-inferred when AssigneeId is set.
    /// </summary>
    public Guid? OwningTeamId { get; set; }
    public virtual Team? OwningTeam { get; set; }

    /// <summary>
    /// Absolute due date/time in UTC.
    /// </summary>
    public DateTime? DueAt { get; set; }

    /// <summary>
    /// Single dependency on another task (self-reference).
    /// Task is Blocked if this dependency is not Closed.
    /// </summary>
    public Guid? DependsOnTaskId { get; set; }
    public virtual TicketTask? DependsOnTask { get; set; }

    /// <summary>
    /// Tasks that depend on this task (inverse navigation).
    /// </summary>
    public virtual ICollection<TicketTask> DependentTasks { get; set; } = new List<TicketTask>();

    /// <summary>
    /// Explicit ordering within the ticket. 1-based.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// When the task was last marked Closed.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Who closed the task.
    /// </summary>
    public Guid? ClosedByStaffId { get; set; }
    public virtual User? ClosedByStaff { get; set; }

    /// <summary>
    /// Who created the task.
    /// </summary>
    public Guid? CreatedByStaffId { get; set; }
    public virtual User? CreatedByStaff { get; set; }

    // Computed properties (not persisted)

    /// <summary>
    /// Typed status value object.
    /// </summary>
    [NotMapped]
    public TicketTaskStatus StatusValue => TicketTaskStatus.From(Status);

    /// <summary>
    /// A task is Blocked if it has a dependency that is not yet Closed.
    /// Blocked is a derived concept, not a stored status.
    /// </summary>
    [NotMapped]
    public bool IsBlocked =>
        DependsOnTaskId != null && DependsOnTask?.Status != TicketTaskStatus.CLOSED;

    /// <summary>
    /// A task is overdue if it is Open and its due date has passed.
    /// </summary>
    [NotMapped]
    public bool IsOverdue =>
        Status == TicketTaskStatus.OPEN && DueAt.HasValue && DueAt.Value < DateTime.UtcNow;
}
