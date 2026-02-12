# Research: Ticket Tasks Feature

**Branch**: `007-ticket-tasks` | **Date**: 2026-02-11

## Research Tasks & Findings

### 1. Domain Entity Patterns (Ticket, Base Classes)

**Decision**: Tasks will use `BaseAuditableEntity` (Guid-based ID) as a child entity of `Ticket` (numeric ID).

**Rationale**: All child entities of Ticket (`TicketComment`, `TicketAttachment`, `TicketFollower`, `TicketChangeLogEntry`) use Guid-based IDs via `BaseAuditableEntity`. Tasks follow the same pattern — they reference `Ticket.Id` (long) as a foreign key.

**Alternatives considered**:
- Numeric ID (`BaseNumericFullAuditableEntity`): Only used for top-level entities like Ticket where sequential user-visible IDs matter. Tasks don't need sequential IDs.

### 2. Task Status Value Object

**Decision**: Create `TicketTaskStatus` as a BuiltIn value object with exactly two values: Open and Closed.

**Rationale**: The constitution mandates that "Enumerated values like statuses MUST be implemented as BuiltIn value objects." Even though task statuses are simpler than ticket statuses (not configurable), consistency with the pattern is required. Unlike `TicketStatus` (which supports dynamic values from `TicketStatusConfig`), `TicketTaskStatus` is fixed — only Open and Closed, no admin configuration.

**Alternatives considered**:
- Simple string constants on entity: Violates the constitution's BuiltIn value objects rule.
- Reusing `TicketStatus`: Semantically wrong — ticket statuses are configurable and have many values; task statuses are fixed at two.

### 3. Blocked State Derivation

**Decision**: Blocked is a computed property (`IsBlocked`) on the `TicketTask` entity, not a stored status. It is true when `DependsOnTaskId` is not null AND the dependency task has status != Closed.

**Rationale**: The spec explicitly states "Blocked is not a status, but a separate concept." Deriving it avoids state synchronization issues — when a dependency is completed, querying the dependent task automatically reflects the unblocked state.

**Alternatives considered**:
- Stored `IsBlocked` flag with event-driven updates: Adds denormalization and sync risk. A computed/queried property is simpler and always consistent.

**Implementation note**: For EF Core queries, `IsBlocked` cannot be a simple `[NotMapped]` property used in LINQ queries. Instead, query handlers must join/check the dependency task's status. A specification or helper method on the query will handle this.

### 4. Activity Logging Pattern

**Decision**: Use the existing `TicketChangeLogEntry` entity for persisted task audit trail, plus broadcast via `ActivityStreamService` for real-time updates.

**Rationale**: The codebase has two activity systems:
1. `TicketChangeLogEntry` — Persisted to database, tied to a ticket. Records changes as message strings with actor and timestamp.
2. `ActivityStreamService` — Real-time SignalR broadcast only, not persisted.

Task events naturally belong to a ticket's change log. Adding task events to `TicketChangeLogEntry` keeps the audit trail in the existing persisted system. Broadcasting via the activity stream provides real-time visibility.

**Alternatives considered**:
- Separate `TicketTaskChangeLogEntry` entity: Fragments the audit trail. Since tasks belong to tickets, keeping events in the same log is more coherent for staff viewing ticket history.

### 5. Notification Integration

**Decision**: Follow the existing event-driven notification pattern: domain events → MediatR handlers → `InAppNotificationService` + email via Liquid templates.

**Rationale**: The existing pattern (`TicketCommentAddedEvent` → `TicketCommentAddedEventHandler_SendNotification`) is well-established. Task notifications follow the same flow:
1. Domain event raised on task entity (e.g., `TicketTaskCompletedEvent`)
2. MediatR handler collects recipients (assignee, ticket followers)
3. Handler calls `InAppNotificationService.SendToUsersAsync()` for in-app + SignalR
4. Handler sends emails via `IEmailer` with Liquid templates

**New notification types** (added to `NotificationEventType`):
- `TASK_ASSIGNED_USER` = "task_assigned_user"
- `TASK_ASSIGNED_TEAM` = "task_assigned_team"
- `TASK_COMPLETED` = "task_completed"

**New email templates** (added to `BuiltInEmailTemplate`):
- `email_task_assigned_user`
- `email_task_assigned_team`
- `email_task_completed`

### 6. Ticket View Extension

**Decision**: Extend `ViewFilterBuilder` with two new boolean-style filter attributes for ticket views, and add a `Tasks` column definition.

**Rationale**: The existing `ViewFilterBuilder.ApplyFilters()` method handles field-specific filtering. Adding task-related filters requires:
1. New filter attributes: `HasTasks` (boolean) and `HasIncompleteTasks` (boolean)
2. Implementation via EF Core subqueries (e.g., `ticket.Tasks.Any()`)
3. New column definition for the Tasks progress indicator

The `FilterAttributeDefinition` list and `ColumnDefinition` list are both extendable.

**Alternatives considered**:
- Separate task view system: Unnecessary — ticket views already support extensible conditions and columns.

### 7. Task Templates Permission Model

**Decision**: Task template management uses the existing `MANAGE_SYSTEM_SETTINGS_PERMISSION` permission, matching Ticket Priorities and Ticket Statuses.

**Rationale**: The spec states "Task template permissions match the same permission model as Ticket Priorities and Ticket Statuses." Both use `[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]`. Task templates follow the same pattern — they are system-level configuration items managed by admins.

### 8. Inline Editing Pattern (Staff Portal)

**Decision**: Use Razor Page POST handlers with AJAX for inline task operations on both the Ticket Detail page and the Tasks page. No modals.

**Rationale**: The existing codebase uses two inline patterns:
1. **Form POST handlers**: `OnPostSnooze`, `OnPostAddComment` on the Details page
2. **AJAX JSON handlers**: `OnPostReorder` on admin pages (for drag-drop)

Task operations will use AJAX JSON handlers for all inline actions (create, edit, complete, reopen, delete, reorder) to avoid full page reloads. This matches the spec's "fully inline, no modals" requirement.

**JavaScript approach**: Minimal targeted JS (per constitution). SortableJS for drag-and-drop. Custom JS for inline edit fields (assignee dropdown, date picker, status toggle). The Snooze feature's `datetime-local` input pattern is reusable for due dates.

### 9. Drag-and-Drop Reordering

**Decision**: Reuse the existing SortableJS + reorder command pattern used by Ticket Priorities and Ticket Statuses.

**Rationale**: The codebase already has a proven pattern:
1. Frontend: SortableJS with `.drag-handle` elements
2. On drag end: Collect ordered IDs from `data-id` attributes
3. POST to `?handler=ReorderTasks` with JSON `{ orderedIds: [...] }`
4. Backend: `ReorderTicketTasks.Command` updates `SortOrder = i + 1` for each task

### 10. Ticket Closure Gate (Status Type Check)

**Decision**: Intercept ticket status changes in the existing status-change command/handler. When the target status has `StatusType = "closed"` (from `TicketStatusConfig`), check for incomplete tasks and return a flag indicating confirmation is needed.

**Rationale**: `TicketStatusConfig` entities have a `StatusType` field ("open" or "closed"). The closure gate checks:
1. Is the new status of type "closed"?
2. Does the ticket have any tasks with status != "closed" OR with unresolved dependencies?
3. If yes, return a response indicating confirmation is needed (client shows the message)
4. On confirmation, a separate command bulk-closes all tasks and changes the status

**Implementation**: The existing ticket status change flow (likely `UpdateTicketStatus` or similar command) will be extended with a `ForceCloseTasks` flag. If the flag is false and incomplete tasks exist, the command returns a "needs confirmation" response. If true, it bulk-closes tasks first.
