namespace App.Domain.Entities;

/// <summary>
/// A single task item within a TaskTemplate.
/// Defines a title, order, and optional dependency on another item in the same template.
/// </summary>
public class TaskTemplateItem : BaseFullAuditableEntity
{
    /// <summary>
    /// FK to the owning TaskTemplate.
    /// </summary>
    public Guid TaskTemplateId { get; set; }
    public virtual TaskTemplate TaskTemplate { get; set; } = null!;

    /// <summary>
    /// Task title that will be used when creating the actual TicketTask.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Explicit ordering within the template. 1-based.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional self-referencing FK: this item depends on another item in the same template.
    /// SET NULL on delete.
    /// </summary>
    public Guid? DependsOnItemId { get; set; }
    public virtual TaskTemplateItem? DependsOnItem { get; set; }
}
