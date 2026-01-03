# Implementation Plan: SLA Extension Controls

**Branch**: `004-sla-extension-controls` | **Date**: 2026-01-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-sla-extension-controls/spec.md`

## Summary

This feature adds the ability to extend ticket SLA due dates by a specified number of hours, with permission-based restrictions. Non-privileged users are limited to a configurable number of extensions and maximum hours, while users with "Manage Tickets" permission have unlimited capabilities. The UI is inline (no modal dialogs) and includes a live preview of the resulting due date, smart defaults targeting 4pm next business day, and clear visibility into extension state.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core Razor Pages, MediatR (CQRS), FluentValidation, Entity Framework Core  
**Storage**: PostgreSQL with EF Core migrations  
**Testing**: xUnit, FluentAssertions (existing test infrastructure)  
**Target Platform**: Linux server (Railway deployment)  
**Project Type**: Web application with Clean Architecture layers  
**Performance Goals**: Inline UI updates should feel instant (<100ms perceived); SLA calculation <50ms  
**Constraints**: No JavaScript frameworks; vanilla JS for progressive enhancement only  
**Scale/Scope**: Single feature within existing ticketing system

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance | Notes |
|-----------|------------|-------|
| Clean Architecture & Dependency Rule | ✅ Pass | New command/query in App.Application; entity changes in App.Domain; UI in App.Web |
| CQRS & Mediator-Driven Use Cases | ✅ Pass | New `ExtendTicketSla` command with Validator and Handler |
| Razor Pages First, Minimal JavaScript | ✅ Pass | Inline UI with progressive JS for live preview only |
| Explicit Data Access & Performance | ✅ Pass | Async EF Core queries with proper Include statements |
| Security, Testing & Observability | ✅ Pass | FluentValidation, permission checks, audit logging |
| BuiltIn Value Objects Pattern | ✅ Pass | No new value objects needed |
| Staff Area UI Pattern | ✅ Pass | Uses existing `.staff-card` patterns |
| GUID vs ShortGuid Pattern | ✅ Pass | Domain uses Guid, DTOs use ShortGuid |
| Alert/Message Display Pattern | ✅ Pass | Uses existing SetSuccessMessage/SetErrorMessage |

**Gate Status**: ✅ PASS - No violations

## Project Structure

### Documentation (this feature)

```text
specs/004-sla-extension-controls/
├── plan.md              # This file
├── research.md          # Phase 0 output - technical decisions
├── data-model.md        # Phase 1 output - entity changes
├── quickstart.md        # Phase 1 output - developer guide
├── contracts/           # Phase 1 output - API contracts
│   └── api.md           # Command/Query definitions
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── App.Domain/
│   └── Entities/
│       └── Ticket.cs                    # Add SlaExtensionCount property
│
├── App.Application/
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   └── ISlaService.cs           # Add CalculateDefaultExtensionHours method
│   │   └── Models/
│   │       └── SlaExtensionSettings.cs  # NEW: Environment variable configuration
│   │
│   ├── SlaRules/
│   │   └── Services/
│   │       └── SlaService.cs            # Implement business day calculation
│   │
│   └── Tickets/
│       ├── Commands/
│       │   └── ExtendTicketSla.cs       # NEW: Main command for SLA extension
│       ├── Queries/
│       │   └── GetSlaExtensionInfo.cs   # NEW: Query for extension state
│       └── TicketDto.cs                 # Add SlaExtensionCount to DTO
│
├── App.Infrastructure/
│   └── Migrations/
│       └── YYYYMMDD_AddSlaExtensionCount.cs  # NEW: Database migration
│
└── App.Web/
    └── Areas/Staff/Pages/Tickets/
        ├── Details.cshtml               # Modify SLA card with inline extension UI
        └── Details.cshtml.cs            # Add ExtendSla handler
```

**Structure Decision**: Follows existing Clean Architecture patterns. New command/query files follow the established vertical slice pattern with nested Command/Validator/Handler types.

## Design Decisions

### UI Approach: Inline Extension (No Modal)

Based on user input: "ideally, the UI for extending happens there and no pop up dialogue"

The SLA card in the ticket sidebar will include:
1. **Collapsed state**: Shows current SLA info with "Extend" link
2. **Expanded state**: Shows hours input, live preview, and Apply button
3. **Extension status badge**: "0 of 1 extensions used" or "Unlimited" for privileged users

### Live Preview Implementation

- JavaScript updates the preview as user types (debounced 150ms)
- Server-side API endpoint calculates the exact target date
- Preview shows: "→ Mon, Jan 6, 2026 at 4:00 PM"

### Business Day Calculation

- Target: 4pm next business day in organization timezone
- Weekends: Skip Saturday and Sunday
- Holidays: Out of scope (documented for future enhancement)
- Fallback: UTC if organization timezone not set

### Permission Model

| User Type | Max Extensions | Max Hours | UI State |
|-----------|----------------|-----------|----------|
| With "Manage Tickets" | Unlimited | Unlimited | Always enabled, shows "Unlimited" badge |
| Without "Manage Tickets" | ENV: `SLA_MAX_EXTENSIONS` (default: 1) | ENV: `SLA_MAX_EXTENSION_HOURS` (default: 168) | Shows "X of Y extensions used", disabled at limit |

### Edge Case: No SLA Rule Assigned

Per spec clarification: SLA options ("refresh from now", "refresh from creation", "extend by hours") should be available even when no SLA rule is assigned. This enables workflows where:
- Ticket enters without SLA
- Later, user applies an SLA manually via refresh or extension
- Extension on tickets without SLA effectively sets an ad-hoc SLA due date

## Complexity Tracking

> No violations to justify - design follows existing patterns.

## Phase 1 Deliverables

- [x] plan.md (this file)
- [x] research.md
- [x] data-model.md
- [x] contracts/api.md
- [x] quickstart.md
