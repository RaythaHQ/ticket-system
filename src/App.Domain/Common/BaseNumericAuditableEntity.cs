namespace App.Domain.Common;

/// <summary>
/// Base auditable entity class for entities with numeric (long) IDs.
/// Implements creation and modification auditing interfaces.
/// </summary>
public abstract class BaseNumericAuditableEntity : BaseNumericEntity, ICreationAuditable, IModificationAuditable
{
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastModificationTime { get; set; }

    public Guid? CreatorUserId { get; set; }
    public virtual User? CreatorUser { get; set; }

    public Guid? LastModifierUserId { get; set; }
    public virtual User? LastModifierUser { get; set; }
}

