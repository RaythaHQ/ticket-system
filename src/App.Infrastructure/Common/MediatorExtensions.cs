using App.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Common;

public static class MediatorExtensions
{
    public static async Task DispatchDomainEventsBeforeSaveChanges(
        this IMediator mediator,
        DbContext context
    )
    {
        // Handle GUID-based entities
        var guidEntities = context
            .ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any(p => p is IBeforeSaveChangesNotification))
            .Select(e => e.Entity)
            .ToList();

        // Handle numeric ID entities (Tickets, Contacts, etc.)
        var numericEntities = context
            .ChangeTracker.Entries<BaseNumericEntity>()
            .Where(e => e.Entity.DomainEvents.Any(p => p is IBeforeSaveChangesNotification))
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = guidEntities
            .SelectMany(e => e.DomainEvents)
            .Concat(numericEntities.SelectMany(e => e.DomainEvents))
            .ToList();

        guidEntities.ForEach(e => e.ClearDomainEvents());
        numericEntities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }

    public static async Task DispatchDomainEventsAfterSaveChanges(
        this IMediator mediator,
        DbContext context
    )
    {
        // Handle GUID-based entities
        var guidEntities = context
            .ChangeTracker.Entries<BaseEntity>()
            .Where(e =>
                e.Entity.DomainEvents.Any(p =>
                    p is IAfterSaveChangesNotification
                    || (
                        p is not IAfterSaveChangesNotification
                        && p is not IBeforeSaveChangesNotification
                    )
                )
            )
            .Select(e => e.Entity)
            .ToList();

        // Handle numeric ID entities (Tickets, Contacts, etc.)
        var numericEntities = context
            .ChangeTracker.Entries<BaseNumericEntity>()
            .Where(e =>
                e.Entity.DomainEvents.Any(p =>
                    p is IAfterSaveChangesNotification
                    || (
                        p is not IAfterSaveChangesNotification
                        && p is not IBeforeSaveChangesNotification
                    )
                )
            )
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = guidEntities
            .SelectMany(e => e.DomainEvents)
            .Concat(numericEntities.SelectMany(e => e.DomainEvents))
            .ToList();

        guidEntities.ForEach(e => e.ClearDomainEvents());
        numericEntities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }
}
