# Implementation Plan: Ticket Tasks

**Branch**: `007-ticket-tasks` | **Date**: 2026-02-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-ticket-tasks/spec.md`

## Summary

Introduce a Tasks feature subordinate to Tickets in the existing Ticket Management System. Tasks represent discrete work items with two statuses (Open/Closed), optional single dependencies (derived Blocked state), assignees, due dates, and explicit ordering. The feature spans all four architectural layers: new domain entities and value objects, CQRS commands/queries with FluentValidation, EF Core persistence with migrations, and Razor Pages UI with targeted JavaScript for inline editing and drag-and-drop. Integration points include the existing notification system (3 new event types), activity logging (8 new event types), ticket view condition builder (2 new conditions + 1 column), and an Admin task templates system.

## Technical Context

**Language/Version**: C# / .NET (ASP.NET Core)  
**Primary Dependencies**: Mediator (source-generated MediatR), FluentValidation, EF Core, SortableJS, SignalR, DotLiquid  
**Storage**: PostgreSQL via EF Core (`IAppDbContext` / `AppDbContext`)  
**Testing**: xUnit with unit test projects (`App.Domain.UnitTests`, `App.Application.UnitTests`, `App.Infrastructure.UnitTests`)  
**Target Platform**: Web (ASP.NET Core, server-rendered Razor Pages)  
**Project Type**: Web application (Clean Architecture monolith: Domain → Application → Infrastructure → Web)  
**Performance Goals**: Ticket Detail page with 50 tasks loads with no perceptible delay; Tasks list page handles 500 tasks  
**Constraints**: Razor Pages first (no SPA frameworks), minimal targeted JavaScript, all inline interactions (no modals), last-write-wins concurrency  
**Scale/Scope**: Existing multi-tenant ticket system with teams, SLA, snooze, views, notifications, and activity streaming

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Clean Architecture & Dependency Rule | PASS | New entities in Domain, commands/queries in Application, EF configs in Infrastructure, Razor Pages + JS in Web. Dependencies flow inwards. |
| CQRS & Mediator-Driven Use Cases | PASS | All task operations as Commands/Queries through Mediator pipeline. Validators use FluentValidation. Handlers own transaction boundaries. |
| Razor Pages First, Minimal JavaScript | PASS | Task UI is Razor Pages with targeted JS (SortableJS for drag-drop, small inline edit handlers). No SPA frameworks. |
| Explicit Data Access & Performance | PASS | All access via `IAppDbContext`. Queries use `AsNoTracking()` and projection. Indexes on key query paths. |
| Security, Testing & Observability | PASS | FluentValidation on all commands. Permission checks via existing authorization policies. Activity logging for all task events. Structured logging. |
| BuiltIn Value Objects | PASS | `TicketTaskStatus` follows the established pattern (ValueObject, From(), Label/DeveloperName). |
| GUID vs ShortGuid | PASS | Domain uses Guid; Application/Web use ShortGuid. |
| Route Constants | PASS | All routes via RouteNames constants. |
| Staff Area UI Pattern | PASS | Uses staff-card, staff-table, staff-badge CSS classes and partials. |
| Admin Area Page Layout | PASS | Uses PageHeading, TableCreateAndSearchBar partials. SidebarLayout. |
| Alert/Message Display | PASS | Uses SetSuccessMessage/SetErrorMessage from BasePageModel. |
| Vertical Slices | PASS | New `TicketTasks` feature slice with Commands/, Queries/, EventHandlers/, DTOs. TaskTemplates as separate slice under TicketConfig. |

**Post Phase 1 re-check**: All gates remain PASS. No constitutional violations in the data model or contract design. The inline AJAX pattern for task operations is consistent with existing patterns (priority reorder, snooze).

## Project Structure

### Documentation (this feature)

```text
specs/007-ticket-tasks/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Entity definitions and relationships
├── quickstart.md        # Developer getting-started guide
├── contracts/
│   └── task-endpoints.md  # API endpoint contracts
├── checklists/
│   └── requirements.md    # Spec quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── App.Domain/
│   ├── Entities/
│   │   ├── TicketTask.cs                    # NEW — Task entity
│   │   ├── TaskTemplate.cs                  # NEW — Template entity
│   │   ├── TaskTemplateItem.cs              # NEW — Template item entity
│   │   └── Ticket.cs                        # MODIFIED — Add Tasks navigation property
│   ├── ValueObjects/
│   │   ├── TicketTaskStatus.cs              # NEW — Open/Closed value object
│   │   └── NotificationEventType.cs         # MODIFIED — Add task notification types
│   ├── Events/
│   │   ├── TicketTaskCreatedEvent.cs        # NEW
│   │   ├── TicketTaskAssignedEvent.cs       # NEW
│   │   ├── TicketTaskCompletedEvent.cs      # NEW
│   │   ├── TicketTaskReopenedEvent.cs       # NEW
│   │   ├── TicketTaskDeletedEvent.cs        # NEW
│   │   ├── TicketTaskUnblockedEvent.cs      # NEW
│   │   ├── TicketTaskDueDateChangedEvent.cs # NEW
│   │   └── TicketTaskDependencyChangedEvent.cs # NEW
│   └── Entities/DefaultTemplates/
│       ├── email_task_assigned_user.liquid   # NEW
│       ├── email_task_assigned_team.liquid   # NEW
│       └── email_task_completed.liquid       # NEW
│
├── App.Application/
│   ├── TicketTasks/
│   │   ├── Commands/
│   │   │   ├── CreateTicketTask.cs          # NEW
│   │   │   ├── UpdateTicketTask.cs          # NEW
│   │   │   ├── CompleteTicketTask.cs        # NEW
│   │   │   ├── ReopenTicketTask.cs          # NEW
│   │   │   ├── DeleteTicketTask.cs          # NEW
│   │   │   ├── ReorderTicketTasks.cs        # NEW
│   │   │   ├── ApplyTaskTemplate.cs         # NEW
│   │   │   └── BulkCloseTicketTasks.cs      # NEW — For ticket closure gate
│   │   ├── Queries/
│   │   │   ├── GetTasksByTicketId.cs        # NEW
│   │   │   ├── GetTasks.cs                  # NEW — Staff Tasks page
│   │   │   └── GetTaskReports.cs            # NEW
│   │   ├── EventHandlers/
│   │   │   ├── TicketTaskNotificationHandlers.cs   # NEW — All notification handlers
│   │   │   └── TicketTaskActivityHandlers.cs       # NEW — Activity stream + change log
│   │   │   # NOTE: Dependency cascade (unblock/reblock) logic lives directly in
│   │   │   # CompleteTicketTask, ReopenTicketTask, and DeleteTicketTask command handlers
│   │   │   # rather than a separate event handler, for simplicity and deterministic ordering.
│   │   └── TicketTaskDto.cs                 # NEW
│   │
│   ├── TicketConfig/
│   │   └── Commands/
│   │       ├── CreateTaskTemplate.cs        # NEW
│   │       ├── UpdateTaskTemplate.cs        # NEW
│   │       ├── DeleteTaskTemplate.cs        # NEW
│   │       └── ToggleTaskTemplateActive.cs  # NEW
│   │   └── Queries/
│   │       ├── GetTaskTemplates.cs          # NEW
│   │       └── GetTaskTemplateById.cs       # NEW
│   │
│   ├── Tickets/Commands/
│   │   └── UpdateTicketStatus.cs            # MODIFIED — Add ForceCloseTasks flag
│   │
│   ├── TicketViews/
│   │   ├── Services/ViewFilterBuilder.cs    # MODIFIED — Add task filter attributes
│   │   ├── ColumnDefinition.cs              # MODIFIED — Add Tasks column
│   │   └── FilterAttributeDefinition.cs     # MODIFIED — Add HasTasks, HasIncompleteTasks
│   │
│   └── Common/Interfaces/
│       └── IRaythaDbContext.cs               # MODIFIED — Add TicketTasks, TaskTemplates, TaskTemplateItems DbSets
│
├── App.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs                  # MODIFIED — Add DbSet properties
│   │   ├── Configurations/
│   │   │   ├── TicketTaskConfiguration.cs   # NEW
│   │   │   ├── TaskTemplateConfiguration.cs # NEW
│   │   │   └── TaskTemplateItemConfiguration.cs # NEW
│   │   └── Migrations/
│   │       └── XXXXXXXX_AddTicketTasks.cs   # NEW — Auto-generated
│   └── Seeds/
│       └── TaskNotificationSeeder.cs        # NEW — Email templates + notification prefs
│
└── App.Web/
    ├── Areas/
    │   ├── Staff/Pages/
    │   │   ├── Tickets/
    │   │   │   ├── Details.cshtml            # MODIFIED — Add Tasks section
    │   │   │   ├── Details.cshtml.cs         # MODIFIED — Add task handlers
    │   │   │   └── _TasksSection.cshtml      # NEW — Tasks partial for ticket detail
    │   │   ├── Tasks/
    │   │   │   ├── Index.cshtml              # NEW — Staff Tasks page
    │   │   │   ├── Index.cshtml.cs           # NEW
    │   │   │   └── Reports.cshtml            # NEW — Task Reports page
    │   │   │   └── Reports.cshtml.cs         # NEW
    │   │   └── Shared/
    │   │       ├── _Layout.cshtml            # MODIFIED — Add Tasks nav item
    │   │       └── RouteNames.cs             # MODIFIED — Add Tasks route constants
    │   │
    │   ├── Admin/Pages/
    │   │   ├── Tickets/TaskTemplates/
    │   │   │   ├── Index.cshtml              # NEW — Template list
    │   │   │   ├── Index.cshtml.cs           # NEW
    │   │   │   ├── Create.cshtml             # NEW — Create template
    │   │   │   ├── Create.cshtml.cs          # NEW
    │   │   │   ├── Edit.cshtml               # NEW — Edit template
    │   │   │   └── Edit.cshtml.cs            # NEW
    │   │   └── Shared/
    │   │       ├── SidebarLayout.cshtml      # MODIFIED — Add Task Templates nav item
    │   │       └── RouteNames.cs             # MODIFIED — Add TaskTemplates route constants
    │   │
    │   └── Api/Controllers/V1/
    │       └── TicketTasksController.cs      # NEW — REST API for tasks
    │
    └── wwwroot/
        └── staff/js/
            └── ticket-tasks.js               # NEW — Inline edit, drag-drop, dependency picker JS
```

**Structure Decision**: Follows the existing vertical slice organization. `TicketTasks` is a new feature slice in Application. Task Templates live under `TicketConfig` since they are admin-level configuration (like Priorities/Statuses). Web layer adds pages under existing Staff and Admin areas. A single focused JS file handles all client-side task interactions.

## Complexity Tracking

No constitution violations. No complexity justifications needed.

## Generated Artifacts

| Artifact | Path | Description |
|----------|------|-------------|
| Research | [research.md](./research.md) | 10 research decisions with rationale |
| Data Model | [data-model.md](./data-model.md) | 3 new entities, 1 value object, entity modifications, state transitions, migration plan |
| Contracts | [contracts/task-endpoints.md](./contracts/task-endpoints.md) | 6 endpoint groups, DTOs, request/response schemas |
| Quickstart | [quickstart.md](./quickstart.md) | Reference files, implementation order, key conventions |
