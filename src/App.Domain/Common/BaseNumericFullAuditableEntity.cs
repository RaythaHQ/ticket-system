namespace App.Domain.Common;

/// <summary>
/// Base full auditable entity class for entities with numeric (long) IDs.
/// Includes soft delete support in addition to creation/modification auditing.
/// </summary>
public abstract class BaseNumericFullAuditableEntity : BaseNumericAuditableEntity, ISoftDelete
{
    public Guid? DeleterUserId { get; set; }
    public DateTime? DeletionTime { get; set; }
    public bool IsDeleted { get; set; } = false;
}

