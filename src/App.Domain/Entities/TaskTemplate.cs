namespace App.Domain.Entities;

/// <summary>
/// A reusable template containing a pre-defined set of tasks with dependency relationships.
/// Can be applied to any ticket to quickly create a standard set of tasks.
/// </summary>
public class TaskTemplate : BaseFullAuditableEntity
{
    /// <summary>
    /// Template display name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description explaining when/how to use this template.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When false, the template is hidden from the staff template picker.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ordered set of task items in this template.
    /// </summary>
    public virtual ICollection<TaskTemplateItem> Items { get; set; } = new List<TaskTemplateItem>();
}
