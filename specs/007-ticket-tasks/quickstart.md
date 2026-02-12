# Quickstart: Ticket Tasks Feature

**Branch**: `007-ticket-tasks` | **Date**: 2026-02-11

## Prerequisites

- .NET SDK (matching project version)
- PostgreSQL database running
- Project builds and runs successfully on `main`

## Getting Started

### 1. Switch to feature branch

```bash
git checkout 007-ticket-tasks
```

### 2. Key reference files

Before writing any code, read these files to understand existing patterns:

| Pattern | Reference File |
|---------|---------------|
| Domain entity | `src/App.Domain/Entities/Ticket.cs` |
| Value object | `src/App.Domain/ValueObjects/TicketStatus.cs` |
| Domain event | `src/App.Domain/Events/TicketCreatedEvent.cs` |
| Command (CQRS) | `src/App.Application/Tickets/Commands/CreateTicket.cs` |
| Query (CQRS) | `src/App.Application/Tickets/Queries/GetTicketById.cs` |
| Event handler (notification) | `src/App.Application/Tickets/EventHandlers/TicketCommentAddedEventHandler_SendNotification.cs` |
| Event handler (activity stream) | `src/App.Application/Tickets/EventHandlers/TicketActivityStreamEventHandlers.cs` |
| EF configuration | `src/App.Infrastructure/Persistence/Configurations/TicketConfiguration.cs` |
| DbContext | `src/App.Infrastructure/Persistence/AppDbContext.cs` |
| Admin page (list + reorder) | `src/App.Web/Areas/Admin/Pages/Tickets/Priorities/Index.cshtml(.cs)` |
| Admin sidebar | `src/App.Web/Areas/Admin/Pages/Shared/SidebarLayout.cshtml` |
| Staff detail page | `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml(.cs)` |
| Staff sidebar | `src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml` |
| Reorder command | `src/App.Application/TicketConfig/Commands/ReorderTicketPriorities.cs` |
| Notification service | `src/App.Web/Services/InAppNotificationService.cs` |
| View filter builder | `src/App.Application/TicketViews/Services/ViewFilterBuilder.cs` |
| Column definitions | `src/App.Application/TicketViews/ColumnDefinition.cs` |
| Email template (Liquid) | `src/App.Domain/Entities/DefaultTemplates/email_ticket_commentadded.liquid` |
| Route names (Admin) | `src/App.Web/Areas/Admin/Pages/Shared/RouteNames.cs` |
| Route names (Staff) | `src/App.Web/Areas/Staff/Pages/Shared/RouteNames.cs` |
| Snooze datetime UI | `src/App.Web/Areas/Staff/Pages/Tickets/_SnoozeModal.cshtml` |
| SortableJS usage | `src/App.Web/Areas/Admin/Pages/Tickets/Priorities/Index.cshtml` |
| Ticket change log | `src/App.Domain/Entities/TicketChangeLogEntry.cs` |
| Ticket permission service | Search for `ITicketPermissionService` or `TicketPermissionService` |

### 3. Implementation order (by user story priority)

**P1 — Core Task CRUD (start here)**:
1. Domain: `TicketTask` entity, `TicketTaskStatus` value object, domain events
2. Infrastructure: EF configuration, migration, DbContext update
3. Application: Commands (Create, Update, Complete, Reopen, Delete, Reorder), Queries (GetTasksByTicketId)
4. Web: Tasks section on Ticket Detail page (Razor partial + AJAX handlers)

**P2 — Dependencies & Blocking**:
5. Domain: Add `DependsOnTaskId` logic, circular dependency check
6. Application: Extend UpdateTicketTask with dependency, add unblock/reblock logic to Complete/Reopen handlers
7. Web: Blocked task UI, dependency picker, "Blocked by" display

**P3 — Staff Tasks Page**:
8. Application: GetTasks query with built-in view filters
9. Web: New Tasks/Index page with views, search, inline actions

**P4 — Notifications**:
10. Domain: New notification event types
11. Application: Event handlers for task notifications
12. Infrastructure: Email templates (Liquid), migration for notification seed data

**P5 — Task Templates**:
13. Domain: TaskTemplate + TaskTemplateItem entities
14. Infrastructure: EF configuration, migration
15. Application: Template CRUD commands/queries, ApplyTaskTemplate command
16. Web: Admin Task Templates pages, template picker on Ticket Detail

**P6 — Activity Logging**:
17. Application: Event handlers for activity stream + change log entries

**P7 — Reports**:
18. Application: GetTaskReports query
19. Web: Tasks/Reports page

**P8 — Ticket Views & Column**:
20. Application: Extend ViewFilterBuilder, add column definition
21. Web: Update ticket list column rendering

### 4. Creating the first migration

After adding domain entities and EF configurations:

```bash
cd src/App.Infrastructure
dotnet ef migrations add AddTicketTasks --startup-project ../App.Web
```

### 5. Key conventions to follow

- **IDs**: `Guid` in Domain, `ShortGuid` everywhere else
- **Commands**: Return `CommandResponseDto<T>`, use `LoggableRequest<T>` base
- **Queries**: Return `IQueryResponseDto<T>`, use `LoggableQuery<T>` base
- **Validators**: FluentValidation, may use `IAppDbContext` for read-only checks
- **Status values**: Always use `TicketTaskStatus.OPEN` / `.CLOSED` constants, never raw strings
- **Async**: All I/O is async with `CancellationToken`
- **Routes**: Use `RouteNames` constants, never hardcoded strings
- **Staff UI**: Use `staff-card`, `staff-table`, `staff-badge` CSS classes
- **Admin UI**: Use `_Partials/PageHeading`, `_Partials/TableCreateAndSearchBar` partials
- **Messages**: Use `SetSuccessMessage()` / `SetErrorMessage()` from BasePageModel
- **ActiveMenu**: Set `ViewData["ActiveMenu"]` for sidebar highlighting
