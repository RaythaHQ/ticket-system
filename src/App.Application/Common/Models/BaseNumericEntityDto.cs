using CSharpVitamins;

namespace App.Application.Common.Models;

/// <summary>
/// Interface for DTOs that use numeric (long) IDs.
/// Used for Ticket and Contact DTOs.
/// </summary>
public interface IBaseNumericEntityDto
{
    long Id { get; init; }
}

/// <summary>
/// Base DTO class for entities with numeric (long) IDs.
/// </summary>
public record BaseNumericEntityDto : IBaseNumericEntityDto
{
    public long Id { get; init; }
}

/// <summary>
/// Base auditable DTO class for entities with numeric (long) IDs.
/// Includes creation and modification timestamps and user references.
/// </summary>
public record BaseNumericAuditableEntityDto : BaseNumericEntityDto
{
    public DateTime CreationTime { get; init; }
    public ShortGuid? CreatorUserId { get; init; }
    public ShortGuid? LastModifierUserId { get; init; }
    public DateTime? LastModificationTime { get; init; }
}

