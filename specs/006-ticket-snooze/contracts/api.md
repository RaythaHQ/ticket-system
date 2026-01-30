# API Contracts: Ticket Snooze

**Feature Branch**: `006-ticket-snooze`  
**Date**: 2026-01-30

## Overview

This document defines the API contracts for the Ticket Snooze feature. Following the existing CQRS pattern, each operation is implemented as a Command or Query.

---

## Commands

### SnoozeTicket

Snoozes a ticket until a specified datetime.

**Command**: `App.Application.Tickets.Commands.SnoozeTicket`

```csharp
public record Command : ICommand<CommandResponseDto<SnoozeTicketResponseDto>>
{
    public ShortGuid TicketId { get; init; }
    public DateTime SnoozeUntil { get; init; }  // In organization timezone
    public string? Reason { get; init; }
}

public record SnoozeTicketResponseDto
{
    public ShortGuid TicketId { get; init; }
    public DateTime SnoozedUntil { get; init; }  // UTC
    public DateTime SnoozedAt { get; init; }     // UTC
}
```

**Validation Rules**:
- `TicketId` must exist
- Ticket must not be closed or resolved
- Ticket must have an individual assignee (not unassigned or team-only)
- `SnoozeUntil` must be in the future
- `SnoozeUntil` must not exceed max duration (default 90 days)
- `Reason` max length: 500 characters

**Behavior**:
1. Convert `SnoozeUntil` from org timezone to UTC
2. Set `SnoozedUntil`, `SnoozedAt`, `SnoozedById`, `SnoozedReason`
3. Clear `UnsnoozedAt` (in case of re-snooze)
4. Add changelog entry
5. Raise `TicketSnoozedEvent`

**Error Responses**:
- `404`: Ticket not found
- `400`: Ticket is closed/resolved
- `400`: Ticket has no individual assignee
- `400`: Snooze time is in the past
- `400`: Snooze duration exceeds maximum

---

### UnsnoozeTicket

Manually unsnoozes a ticket before its scheduled time.

**Command**: `App.Application.Tickets.Commands.UnsnoozeTicket`

```csharp
public record Command : ICommand<CommandResponseDto<UnsnoozeTicketResponseDto>>
{
    public ShortGuid TicketId { get; init; }
}

public record UnsnoozeTicketResponseDto
{
    public ShortGuid TicketId { get; init; }
    public TimeSpan SnoozeDuration { get; init; }
}
```

**Validation Rules**:
- `TicketId` must exist
- Ticket must be currently snoozed (`SnoozedUntil` is set and in future)

**Behavior**:
1. Calculate snooze duration: `now - SnoozedAt`
2. If org setting `PauseSlaOnSnooze` is true, extend `SlaDueAt` by snooze duration
3. Set `UnsnoozedAt = now`
4. Clear `SnoozedUntil`, `SnoozedAt`, `SnoozedById`, `SnoozedReason`
5. Add changelog entry
6. Raise `TicketUnsnoozedEvent` (with `WasAutoUnsnooze = false`)

**Error Responses**:
- `404`: Ticket not found
- `400`: Ticket is not snoozed

---

## Queries

### GetSnoozePresets

Returns available snooze preset options with calculated times.

**Query**: `App.Application.Tickets.Queries.GetSnoozePresets`

```csharp
public record Query : IQuery<IQueryResponseDto<SnoozePresetsDto>>
{
    // No parameters - uses current time and org timezone
}

public record SnoozePresetsDto
{
    public List<SnoozePresetDto> Presets { get; init; } = new();
    public DateTime MaxAllowedDate { get; init; }  // Based on max duration
    public string OrganizationTimezone { get; init; } = null!;
}

public record SnoozePresetDto
{
    public string Key { get; init; } = null!;      // "later_today", "tomorrow", etc.
    public string Label { get; init; } = null!;    // "Later Today", "Tomorrow", etc.
    public DateTime CalculatedTime { get; init; }  // In org timezone
    public string RelativeDisplay { get; init; } = null!;  // "in 3 hours", "tomorrow at 9am"
}
```

**Presets Returned**:
| Key | Label | Calculation |
|-----|-------|-------------|
| `later_today` | Later Today | Now + 3 hours, or 9am tomorrow if <3h to 5pm |
| `tomorrow` | Tomorrow | 9am tomorrow |
| `in_3_days` | In 3 Days | 9am in 3 days |
| `next_week` | Next Week | 9am next Monday |

---

### GetTicketSnoozeInfo

Returns snooze information for a specific ticket.

**Query**: `App.Application.Tickets.Queries.GetTicketSnoozeInfo`

```csharp
public record Query : IQuery<IQueryResponseDto<TicketSnoozeInfoDto>>
{
    public ShortGuid TicketId { get; init; }
}

public record TicketSnoozeInfoDto
{
    public ShortGuid TicketId { get; init; }
    public bool IsSnoozed { get; init; }
    public DateTime? SnoozedUntil { get; init; }      // In org timezone
    public DateTime? SnoozedAt { get; init; }         // In org timezone
    public ShortGuid? SnoozedById { get; init; }
    public string? SnoozedByName { get; init; }
    public string? SnoozedReason { get; init; }
    public string? TimeRemaining { get; init; }       // "2 hours", "3 days", etc.
    public bool IsRecentlyUnsnoozed { get; init; }
    public DateTime? UnsnoozedAt { get; init; }       // In org timezone
    public bool CanSnooze { get; init; }              // Has assignee, not closed
    public string? CannotSnoozeReason { get; init; }  // Why snooze is blocked
}
```

---

## DTOs

### TicketListItemDto (Extended)

Add snooze fields to existing `TicketListItemDto`:

```csharp
// Existing fields...

// New snooze fields
public bool IsSnoozed { get; init; }
public DateTime? SnoozedUntil { get; init; }
public bool IsRecentlyUnsnoozed { get; init; }
```

### TicketDto (Extended)

Add full snooze info to existing `TicketDto`:

```csharp
// Existing fields...

// New snooze fields
public bool IsSnoozed { get; init; }
public DateTime? SnoozedUntil { get; init; }
public DateTime? SnoozedAt { get; init; }
public ShortGuid? SnoozedById { get; init; }
public string? SnoozedByName { get; init; }
public string? SnoozedReason { get; init; }
public bool IsRecentlyUnsnoozed { get; init; }
public DateTime? UnsnoozedAt { get; init; }
public bool CanSnooze { get; init; }
public string? CannotSnoozeReason { get; init; }
```

---

## View Filter Condition

### IsSnoozed Filter

Add to `FilterAttributes.All`:

```csharp
new()
{
    Field = "IsSnoozed",
    Label = "Is Snoozed",
    Type = "boolean",
    Operators = OperatorDefinitions.BooleanOperators,
}
```

Add to `ViewFilterBuilder.BuildFilterBody()`:

```csharp
"issnoozed" => BuildBooleanExpression(param, filter, 
    t => t.SnoozedUntil != null && t.SnoozedUntil > DateTime.UtcNow),
```

---

## Page Endpoints

### Staff Area

| Page | Route | Purpose |
|------|-------|---------|
| Ticket Detail | `/Staff/Tickets/{id}` | Shows snooze status, snooze/unsnooze buttons |
| Ticket List | `/Staff/Tickets` | Snooze filter toggle, snooze indicator |
| Snoozed View | `/Staff/Tickets?view=snoozed` | Built-in view for snoozed tickets |

### Admin Area

| Page | Route | Purpose |
|------|-------|---------|
| Organization Settings | `/Admin/Settings/Organization` | PauseSlaOnSnooze toggle |
| System Views | `/Admin/TicketViews` | IsSnoozed condition available |

---

## Event Handlers

### TicketUnsnoozedEventHandler_SendNotification

Sends notifications when a ticket is unsnoozed.

**Input**: `TicketUnsnoozedEvent`

**Logic**:
```
IF auto-unsnooze (WasAutoUnsnooze = true):
    Notify: assignee, followers
    
IF manual unsnooze:
    actorId = UnsnoozedById
    
    FOR assignee:
        IF actorId != assigneeId:
            Notify assignee
    
    FOR each follower:
        IF actorId != followerId:
            Notify follower
```

**Notification Content**:
- Event Type: `TICKET_UNSNOOZED`
- In-app: "Ticket #123 has been unsnoozed" / "Ticket #123 was unsnoozed by John"
- Email: Uses `TicketUnsnoozedEmail` template

---

## Configuration

### Environment Variables

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `SNOOZE_MAX_DURATION_DAYS` | int | 90 | Maximum snooze duration in days |

### Organization Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `PauseSlaOnSnooze` | bool | true | Pause SLA timers while snoozed |

---

## Changelog Entry Format

### Snooze Action

```json
{
    "SnoozedUntil": {
        "OldValue": null,
        "NewValue": "2026-02-01T09:00:00Z"
    }
}
```

Message: "Snoozed ticket until Feb 1, 2026 at 9:00 AM"

### Unsnooze Action

```json
{
    "SnoozedUntil": {
        "OldValue": "2026-02-01T09:00:00Z",
        "NewValue": null
    }
}
```

Message: "Unsnoozed ticket" or "Ticket automatically unsnoozed"

### Auto-unsnooze on Unassign

```json
{
    "SnoozedUntil": {
        "OldValue": "2026-02-01T09:00:00Z",
        "NewValue": null
    },
    "AssigneeId": {
        "OldValue": "abc123",
        "NewValue": null
    }
}
```

Message: "Ticket automatically unsnoozed (assignee removed)"
