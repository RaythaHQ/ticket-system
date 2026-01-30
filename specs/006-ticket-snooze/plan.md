# Implementation Plan: Ticket Snooze

**Branch**: `006-ticket-snooze` | **Date**: 2026-01-30 | **Spec**: [spec.md](./spec.md)

## Summary

Implement ticket snooze functionality allowing staff to temporarily hide tickets from their active queue until a scheduled time. Tickets auto-unsnooze at the scheduled time via a background job, with notifications sent to assignees and followers. Snoozed tickets require an individual assignee; removing the assignee auto-unsnoozes the ticket. SLA timers pause during snooze (configurable). Views support filtering by snooze state.

---

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: ASP.NET Core, Entity Framework Core, MediatR, FluentValidation, SignalR  
**Storage**: PostgreSQL via EF Core  
**Testing**: xUnit, FluentAssertions  
**Target Platform**: Linux server (Docker)  
**Project Type**: Web application (Razor Pages + minimal JS)  
**Performance Goals**: Snooze/unsnooze operations < 200ms; background job processes 100 tickets/batch  
**Constraints**: 5-minute maximum latency for auto-unsnooze  
**Scale/Scope**: Thousands of tickets, hundreds of users

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Clean Architecture & Dependency Rule | ✅ Pass | Domain events in Domain, commands/queries in Application, background job in Infrastructure, UI in Web |
| CQRS & Mediator-Driven Use Cases | ✅ Pass | SnoozeTicket/UnsnoozeTicket commands, GetSnoozePresets/GetTicketSnoozeInfo queries |
| Razor Pages First, Minimal JavaScript | ✅ Pass | Snooze modal uses server-side with progressive enhancement |
| Explicit Data Access & Performance Discipline | ✅ Pass | Partial index on SnoozedUntil, batch processing in background job |
| Security, Testing & Observability | ✅ Pass | Validation in commands, changelog for audit, structured logging |
| GUID vs ShortGuid Pattern | ✅ Pass | Domain uses Guid, DTOs use ShortGuid |
| Alert/Message Display Pattern | ✅ Pass | Use SetSuccessMessage() for snooze confirmations |
| BuiltIn Value Objects Pattern | ✅ Pass | NotificationEventType extended with TICKET_UNSNOOZED |
| Staff Area UI Pattern | ✅ Pass | Snooze UI follows staff-card, staff-badge patterns |
| Route Constants | ✅ Pass | Add snooze routes to RouteNames.cs |

---

## Project Structure

### Documentation (this feature)

```text
specs/006-ticket-snooze/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Technical research and decisions
├── data-model.md        # Entity and database schema changes
├── quickstart.md        # Implementation quick reference
├── contracts/
│   └── api.md           # API contracts (commands, queries, DTOs)
├── tasks.md             # Implementation tasks (created by /speckit.tasks)
└── checklists/
    └── requirements.md  # Specification quality checklist
```

### Source Code (affected files)

```text
src/
├── App.Domain/
│   ├── Entities/
│   │   └── Ticket.cs                          # Add snooze fields
│   ├── Events/
│   │   ├── TicketSnoozedEvent.cs              # NEW
│   │   └── TicketUnsnoozedEvent.cs            # NEW
│   └── ValueObjects/
│       ├── NotificationEventType.cs           # Add TICKET_UNSNOOZED
│       └── BuiltInEmailTemplate.cs            # Add TicketUnsnoozedEmail
│
├── App.Application/
│   ├── Tickets/
│   │   ├── Commands/
│   │   │   ├── SnoozeTicket.cs                # NEW
│   │   │   ├── UnsnoozeTicket.cs              # NEW
│   │   │   ├── AssignTicket.cs                # Modify for auto-unsnooze
│   │   │   └── UpdateTicket.cs                # Modify for auto-unsnooze
│   │   ├── Queries/
│   │   │   ├── GetSnoozePresets.cs            # NEW
│   │   │   └── GetTicketSnoozeInfo.cs         # NEW
│   │   ├── EventHandlers/
│   │   │   └── TicketUnsnoozedEventHandler_SendNotification.cs  # NEW
│   │   ├── TicketDto.cs                       # Add snooze fields
│   │   └── TicketListItemDto.cs               # Add snooze fields
│   └── TicketViews/
│       ├── FilterAttributeDefinition.cs       # Add IsSnoozed
│       └── Services/ViewFilterBuilder.cs      # Add issnoozed case
│
├── App.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   └── TicketConfiguration.cs         # Add snooze config
│   │   └── Migrations/
│   │       └── YYYYMMDD_AddTicketSnooze.cs    # NEW (via EF)
│   ├── BackgroundTasks/
│   │   └── SnoozeEvaluationJob.cs             # NEW
│   ├── Configurations/
│   │   └── SnoozeConfiguration.cs             # NEW
│   └── ConfigureServices.cs                   # Register services
│
└── App.Web/
    └── Areas/
        ├── Staff/
        │   └── Pages/
        │       └── Tickets/
        │           ├── Index.cshtml.cs        # Modify built-in views
        │           ├── Index.cshtml           # Add snooze toggle
        │           ├── Details.cshtml.cs      # Add snooze actions
        │           ├── Details.cshtml         # Add snooze UI
        │           └── _SnoozeModal.cshtml    # NEW
        └── Admin/
            └── Pages/
                └── Settings/
                    ├── Organization.cshtml.cs # Add PauseSlaOnSnooze
                    └── Organization.cshtml    # Add toggle

tests/
└── App.Application.UnitTests/
    └── Tickets/
        ├── Commands/
        │   ├── SnoozeTicketTests.cs           # NEW
        │   └── UnsnoozeTicketTests.cs         # NEW
        └── Queries/
            └── GetSnoozePresetsTests.cs       # NEW
```

**Structure Decision**: Follows existing Clean Architecture layout. New files organized by feature (Tickets) with subfolders by type (Commands, Queries, EventHandlers).

---

## Design Decisions Summary

| Decision | Rationale |
|----------|-----------|
| Snooze fields on Ticket entity | Simpler queries for view filtering; matches SLA field pattern |
| Partial index on SnoozedUntil | Efficient background job queries |
| Single NotificationEventType | TICKET_UNSNOOZED covers both auto and manual unsnooze |
| Background job every 5 min | Acceptable latency per spec; matches SLA job pattern |
| SLA extended on unsnooze | Simple approach; extend SlaDueAt by snooze duration |
| Env var for max duration | SNOOZE_MAX_DURATION_DAYS=90 default |

See [research.md](./research.md) for detailed rationale and alternatives considered.

---

## Data Model Summary

### Ticket Entity Changes

| Field | Type | Purpose |
|-------|------|---------|
| `SnoozedUntil` | `DateTime?` | Auto-unsnooze time (UTC) |
| `SnoozedAt` | `DateTime?` | When snoozed (UTC) |
| `SnoozedById` | `Guid?` | Who snoozed |
| `SnoozedReason` | `string?` | Optional note (500 chars) |
| `UnsnoozedAt` | `DateTime?` | For "recently unsnoozed" indicator |

### New Domain Events

- `TicketSnoozedEvent` - Raised when ticket is snoozed
- `TicketUnsnoozedEvent` - Raised when ticket is unsnoozed (auto or manual)

See [data-model.md](./data-model.md) for complete schema.

---

## API Summary

### Commands

| Command | Purpose |
|---------|---------|
| `SnoozeTicket` | Snooze ticket until specified time |
| `UnsnoozeTicket` | Manually unsnooze ticket |

### Queries

| Query | Purpose |
|-------|---------|
| `GetSnoozePresets` | Get preset options with calculated times |
| `GetTicketSnoozeInfo` | Get snooze status for a ticket |

See [contracts/api.md](./contracts/api.md) for complete contracts.

---

## Implementation Phases

### Phase 1: Core Infrastructure (User Story 1)

**Goal**: Basic snooze/unsnooze with auto-unsnooze background job

1. Domain: Ticket entity changes, domain events
2. Infrastructure: Migration, background job, configuration
3. Application: SnoozeTicket, UnsnoozeTicket commands
4. Basic validation and changelog

**Deliverable**: Staff can snooze tickets, they auto-unsnooze at scheduled time

### Phase 2: Quick Presets (User Story 2)

**Goal**: Preset options for common snooze durations

1. Application: GetSnoozePresets query
2. Web: Snooze modal with preset buttons

**Deliverable**: Staff can snooze with one-click presets

### Phase 3: Manual Unsnooze & Assignment Constraints (User Stories 3, 6)

**Goal**: Manual unsnooze and enforced assignee requirement

1. Application: UnsnoozeTicket command refinement
2. Application: Update AssignTicket/UpdateTicket for auto-unsnooze
3. Web: Unsnooze button on ticket detail

**Deliverable**: Staff can manually unsnooze; unassigning auto-unsnoozes

### Phase 4: SLA Integration (User Story 4)

**Goal**: SLA pauses during snooze

1. Domain: OrganizationSettings.PauseSlaOnSnooze
2. Application: Extend SlaDueAt on unsnooze
3. Web: Admin toggle for setting

**Deliverable**: SLA timers pause while snoozed (configurable)

### Phase 5: Notifications (User Story 5)

**Goal**: Proper notification delivery

1. Domain: NotificationEventType.TICKET_UNSNOOZED
2. Application: TicketUnsnoozedEventHandler_SendNotification
3. Infrastructure: Email template, migration for default preferences

**Deliverable**: Notifications sent on unsnooze (respecting rules)

### Phase 6: View Filtering (User Stories 7, 8)

**Goal**: Filter snoozed tickets in views

1. Application: IsSnoozed filter condition
2. Web: Built-in view updates, snooze toggle UI
3. Web: "Snoozed" built-in view

**Deliverable**: Staff can filter views by snooze state

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Background job delays | 5-minute interval acceptable; add monitoring/alerting |
| Large batch processing | Process in batches of 100; pagination |
| Timezone confusion | Store UTC, convert for display; document clearly |
| Orphaned snoozed tickets | Auto-unsnooze on unassignment enforced at command level |

---

## Complexity Tracking

No constitution violations requiring justification. Implementation follows established patterns.

---

## Next Steps

Run `/speckit.tasks` to generate implementation tasks from this plan.
