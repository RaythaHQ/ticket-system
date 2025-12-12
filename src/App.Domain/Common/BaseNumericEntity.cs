using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Common;

/// <summary>
/// Interface for entities that use a numeric (long) ID instead of GUID.
/// Used for Tickets and Contacts where human-readable IDs are preferred.
/// </summary>
public interface IBaseNumericEntity
{
    long Id { get; set; }
}

/// <summary>
/// Base entity class for entities that use numeric (long) IDs.
/// Provides domain event support identical to BaseEntity but with long ID.
/// </summary>
public abstract class BaseNumericEntity : IBaseNumericEntity
{
    public long Id { get; set; }

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

