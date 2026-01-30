# Quickstart: Ticket Snooze

**Feature Branch**: `006-ticket-snooze`  
**Date**: 2026-01-30

## Overview

This guide provides a quick reference for implementing the Ticket Snooze feature.

---

## Implementation Order

### Phase 1: Core Infrastructure (P1)

1. **Domain Layer**
   - [ ] Add snooze fields to `Ticket` entity
   - [ ] Add `IsSnoozed` and `IsRecentlyUnsnoozed` computed properties
   - [ ] Create `TicketSnoozedEvent` domain event
   - [ ] Create `TicketUnsnoozedEvent` domain event
   - [ ] Add `TICKET_UNSNOOZED` to `NotificationEventType`
   - [ ] Add `TicketUnsnoozedEmail` to `BuiltInEmailTemplate`

2. **Infrastructure Layer**
   - [ ] Update `TicketConfiguration` with snooze fields and index
   - [ ] Create EF migration for schema changes
   - [ ] Create `SnoozeConfiguration` for env var settings
   - [ ] Create `SnoozeEvaluationJob` background service

3. **Application Layer**
   - [ ] Create `SnoozeTicket` command with validator and handler
   - [ ] Create `UnsnoozeTicket` command with validator and handler
   - [ ] Create `GetSnoozePresets` query
   - [ ] Create `GetTicketSnoozeInfo` query
   - [ ] Update `TicketDto` and `TicketListItemDto` with snooze fields

### Phase 2: Integration (P2-P3)

4. **Notification Integration**
   - [ ] Create `TicketUnsnoozedEventHandler_SendNotification`
   - [ ] Create email template `email_ticket_unsnoozed`
   - [ ] Create data migration for default notification preferences

5. **Assignment Constraint Integration**
   - [ ] Update `AssignTicket` command to auto-unsnooze on unassign
   - [ ] Update `UpdateTicket` command to auto-unsnooze on unassign
   - [ ] Add validation in `SnoozeTicket` for assignee requirement

6. **SLA Integration**
   - [ ] Add `PauseSlaOnSnooze` to `OrganizationSettings`
   - [ ] Update unsnooze logic to extend `SlaDueAt`

### Phase 3: Views & UI (P3-P4)

7. **View Filtering**
   - [ ] Add `IsSnoozed` to `FilterAttributes.All`
   - [ ] Add `issnoozed` case in `ViewFilterBuilder`
   - [ ] Update built-in views to exclude snoozed by default
   - [ ] Add "Snoozed" built-in view

8. **Staff UI**
   - [ ] Add snooze button to ticket detail page
   - [ ] Add snooze modal with presets and custom datetime
   - [ ] Add unsnooze button for snoozed tickets
   - [ ] Add snooze indicator to ticket list
   - [ ] Add "Show snoozed" toggle to view filters
   - [ ] Add recently unsnoozed indicator

9. **Admin UI**
   - [ ] Add `PauseSlaOnSnooze` toggle to organization settings
   - [ ] Add `IsSnoozed` condition to view editor

---

## Key Files to Create/Modify

### Domain Layer (`src/App.Domain/`)

| File | Action |
|------|--------|
| `Entities/Ticket.cs` | Modify - add snooze fields |
| `Events/TicketSnoozedEvent.cs` | Create |
| `Events/TicketUnsnoozedEvent.cs` | Create |
| `ValueObjects/NotificationEventType.cs` | Modify - add TICKET_UNSNOOZED |
| `ValueObjects/BuiltInEmailTemplate.cs` | Modify - add TicketUnsnoozedEmail |

### Application Layer (`src/App.Application/`)

| File | Action |
|------|--------|
| `Tickets/Commands/SnoozeTicket.cs` | Create |
| `Tickets/Commands/UnsnoozeTicket.cs` | Create |
| `Tickets/Queries/GetSnoozePresets.cs` | Create |
| `Tickets/Queries/GetTicketSnoozeInfo.cs` | Create |
| `Tickets/TicketDto.cs` | Modify - add snooze fields |
| `Tickets/TicketListItemDto.cs` | Modify - add snooze fields |
| `Tickets/Commands/AssignTicket.cs` | Modify - add auto-unsnooze |
| `Tickets/Commands/UpdateTicket.cs` | Modify - add auto-unsnooze |
| `Tickets/EventHandlers/TicketUnsnoozedEventHandler_SendNotification.cs` | Create |
| `TicketViews/FilterAttributeDefinition.cs` | Modify - add IsSnoozed |
| `TicketViews/Services/ViewFilterBuilder.cs` | Modify - add issnoozed case |

### Infrastructure Layer (`src/App.Infrastructure/`)

| File | Action |
|------|--------|
| `Persistence/Configurations/TicketConfiguration.cs` | Modify |
| `Persistence/Migrations/YYYYMMDD_AddTicketSnooze.cs` | Create (via EF) |
| `BackgroundTasks/SnoozeEvaluationJob.cs` | Create |
| `Configurations/SnoozeConfiguration.cs` | Create |
| `ConfigureServices.cs` | Modify - register services |

### Web Layer (`src/App.Web/`)

| File | Action |
|------|--------|
| `Areas/Staff/Pages/Tickets/Index.cshtml.cs` | Modify - built-in views |
| `Areas/Staff/Pages/Tickets/Index.cshtml` | Modify - snooze toggle |
| `Areas/Staff/Pages/Tickets/Details.cshtml.cs` | Modify - snooze actions |
| `Areas/Staff/Pages/Tickets/Details.cshtml` | Modify - snooze UI |
| `Areas/Staff/Pages/Tickets/_SnoozeModal.cshtml` | Create |
| `Areas/Admin/Pages/Settings/Organization.cshtml.cs` | Modify |
| `Areas/Admin/Pages/Settings/Organization.cshtml` | Modify |

---

## Code Snippets

### Snooze Fields on Ticket

```csharp
// In Ticket.cs
public DateTime? SnoozedUntil { get; set; }
public DateTime? SnoozedAt { get; set; }
public Guid? SnoozedById { get; set; }
public virtual User? SnoozedBy { get; set; }
public string? SnoozedReason { get; set; }
public DateTime? UnsnoozedAt { get; set; }

[NotMapped]
public bool IsSnoozed => SnoozedUntil != null && SnoozedUntil > DateTime.UtcNow;

[NotMapped]
public bool IsRecentlyUnsnoozed => UnsnoozedAt != null 
    && DateTime.UtcNow - UnsnoozedAt.Value < TimeSpan.FromMinutes(30);
```

### View Filter Condition

```csharp
// In FilterAttributeDefinition.cs - add to FilterAttributes.All
new()
{
    Field = "IsSnoozed",
    Label = "Is Snoozed",
    Type = "boolean",
    Operators = OperatorDefinitions.BooleanOperators,
}

// In ViewFilterBuilder.cs - add to BuildFilterBody switch
"issnoozed" => BuildBooleanExpression(param, filter, 
    t => t.SnoozedUntil != null && t.SnoozedUntil > DateTime.UtcNow),
```

### Background Job Pattern

```csharp
public class SnoozeEvaluationJob : BackgroundService
{
    private readonly TimeSpan _evaluationInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 100;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessDueSnoozes(stoppingToken);
            await Task.Delay(_evaluationInterval, stoppingToken);
        }
    }
    
    private async Task ProcessDueSnoozes(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var now = DateTime.UtcNow;
        
        var dueTicketIds = await db.Tickets
            .Where(t => t.SnoozedUntil != null && t.SnoozedUntil <= now)
            .Select(t => t.Id)
            .Take(BatchSize)
            .ToListAsync(ct);
            
        // Process each ticket...
    }
}
```

### Notification Event Type

```csharp
// In NotificationEventType.cs
public const string TICKET_UNSNOOZED = "ticket_unsnoozed";

public static NotificationEventType TicketUnsnoozed => 
    new("Ticket Unsnoozed", TICKET_UNSNOOZED);

// Add to SupportedTypes:
yield return TicketUnsnoozed;
```

---

## Testing Checklist

### Unit Tests

- [ ] `SnoozeTicket.Validator` - validates future time, max duration, assignee
- [ ] `SnoozeTicket.Handler` - sets fields correctly, raises event
- [ ] `UnsnoozeTicket.Validator` - validates ticket is snoozed
- [ ] `UnsnoozeTicket.Handler` - clears fields, extends SLA, raises event
- [ ] `GetSnoozePresets.Handler` - calculates presets correctly
- [ ] `TicketUnsnoozedEventHandler` - notification logic

### Integration Tests

- [ ] Snooze/unsnooze round trip
- [ ] Auto-unsnooze via background job
- [ ] Auto-unsnooze on assignment removal
- [ ] View filtering with IsSnoozed condition
- [ ] SLA extension on unsnooze

---

## Environment Variables

```bash
# .env.example
SNOOZE_MAX_DURATION_DAYS=90
```

---

## Common Gotchas

1. **Timezone handling**: Store all times as UTC, convert to org timezone for display
2. **Partial index**: Ensure migration creates filtered index on `SnoozedUntil`
3. **Notification deduplication**: Don't notify the actor who performed the action
4. **Re-snooze**: Snoozing an already-snoozed ticket updates the existing snooze
5. **Closed tickets**: Auto-cancel snooze when ticket is closed (no notification)
