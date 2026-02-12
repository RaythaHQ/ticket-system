# Data Model: Ticket Tasks Feature

**Branch**: `007-ticket-tasks` | **Date**: 2026-02-11

## Entity Relationship Overview

```
Ticket (1) ──── (*) TicketTask
                      │
                      ├── (0..1) DependsOnTask (self-reference)
                      ├── (0..1) Assignee (User)
                      ├── (0..1) OwningTeam (Team)
                      └── (0..1) CreatedByStaff (User)

TaskTemplate (1) ──── (*) TaskTemplateItem
                              │
                              └── (0..1) DependsOnItem (self-reference)
```

## Entities

### TicketTask

**Layer**: `App.Domain/Entities/TicketTask.cs`  
**Base class**: `BaseAuditableEntity` (Guid ID, audit fields, soft delete)  
**Table**: `TicketTasks`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key (from BaseAuditableEntity) |
| TicketId | long | Yes | FK → Ticket.Id. Parent ticket. CASCADE delete. |
| Title | string | Yes | Task title (max 500 chars). The only content field. |
| Status | string | Yes | "open" or "closed". Default: "open". Stored as DeveloperName from TicketTaskStatus value object. |
| AssigneeId | Guid? | No | FK → User.Id. Assigned user. SET NULL on delete. |
| OwningTeamId | Guid? | No | FK → Team.Id. Assigned team. SET NULL on delete. When AssigneeId is set, team is inferred from user's team membership. |
| DueAt | DateTime? | No | Absolute due date/time in UTC. |
| DependsOnTaskId | Guid? | No | FK → TicketTask.Id (self-reference). Single dependency. SET NULL on delete. |
| SortOrder | int | Yes | Explicit ordering within the ticket. 1-based. |
| ClosedAt | DateTime? | No | When the task was last marked Closed. |
| ClosedByStaffId | Guid? | No | FK → User.Id. Who closed the task. SET NULL on delete. |
| CreatedByStaffId | Guid? | No | FK → User.Id. Who created the task. SET NULL on delete. |

**Computed properties (NotMapped)**:

| Property | Type | Derivation |
|----------|------|------------|
| StatusValue | TicketTaskStatus | `TicketTaskStatus.From(Status)` |
| IsBlocked | bool | `DependsOnTaskId != null && DependsOnTask?.Status != TicketTaskStatus.CLOSED` |
| IsOverdue | bool | `Status == TicketTaskStatus.OPEN && DueAt.HasValue && DueAt < DateTime.UtcNow` |

**Navigation properties**:

| Property | Type | Relationship |
|----------|------|-------------|
| Ticket | Ticket | Required parent |
| Assignee | User? | Optional |
| OwningTeam | Team? | Optional |
| DependsOnTask | TicketTask? | Optional self-reference |
| DependentTasks | ICollection&lt;TicketTask&gt; | Inverse of DependsOnTask |
| CreatedByStaff | User? | Optional |
| ClosedByStaff | User? | Optional |

**Indexes**:
- `IX_TicketTasks_TicketId` on (TicketId) — Primary query path
- `IX_TicketTasks_AssigneeId` on (AssigneeId) — Staff Tasks page queries
- `IX_TicketTasks_OwningTeamId` on (OwningTeamId) — Team Tasks view
- `IX_TicketTasks_Status` on (Status) — Status filtering
- `IX_TicketTasks_DueAt` partial on (DueAt) WHERE DueAt IS NOT NULL — Overdue queries
- `IX_TicketTasks_DependsOnTaskId` on (DependsOnTaskId) — Dependency lookups
- `IX_TicketTasks_CreatedByStaffId` on (CreatedByStaffId) — Created by Me view

**Soft delete**: Inherits `IsDeleted` query filter from `BaseAuditableEntity`.

**Domain events**:
- `TicketTaskCreatedEvent(TicketTask task)`
- `TicketTaskAssignedEvent(TicketTask task, Guid? previousAssigneeId, Guid? previousTeamId)`
- `TicketTaskCompletedEvent(TicketTask task)`
- `TicketTaskReopenedEvent(TicketTask task)`
- `TicketTaskDeletedEvent(long ticketId, string taskTitle, Guid? assigneeId)`
- `TicketTaskUnblockedEvent(TicketTask task)`
- `TicketTaskDueDateChangedEvent(TicketTask task, DateTime? previousDueAt)`
- `TicketTaskDependencyChangedEvent(TicketTask task, Guid? previousDependsOnTaskId)`

---

### TicketTaskStatus (Value Object)

**Layer**: `App.Domain/ValueObjects/TicketTaskStatus.cs`  
**Base class**: `ValueObject`

Fixed two-value BuiltIn value object. Not configurable by admins.

| Static Property | Label | DeveloperName | Constant |
|-----------------|-------|---------------|----------|
| Open | "Open" | "open" | OPEN |
| Closed | "Closed" | "closed" | CLOSED |

**Pattern**: Same as `TicketStatus` / `TicketPriority` — inherits `ValueObject`, has `From(string)` factory, `Label`/`DeveloperName` properties, implicit string conversion, `SupportedTypes`, equality by `DeveloperName`. Does NOT support dynamic/custom values (no config entity).

---

### TaskTemplate

**Layer**: `App.Domain/Entities/TaskTemplate.cs`  
**Base class**: `BaseAuditableEntity` (Guid ID, audit fields, soft delete)  
**Table**: `TaskTemplates`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| Name | string | Yes | Template name (max 200 chars) |
| Description | string? | No | Optional description (max 1000 chars) |
| IsActive | bool | Yes | Whether template is available for use. Default: true. |

**Navigation properties**:

| Property | Type | Relationship |
|----------|------|-------------|
| Items | ICollection&lt;TaskTemplateItem&gt; | Ordered template items |

**Soft delete**: Inherits `IsDeleted` query filter.

---

### TaskTemplateItem

**Layer**: `App.Domain/Entities/TaskTemplateItem.cs`  
**Base class**: `BaseAuditableEntity` (Guid ID, audit fields, soft delete)  
**Table**: `TaskTemplateItems`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| TaskTemplateId | Guid | Yes | FK → TaskTemplate.Id. CASCADE delete. |
| Title | string | Yes | Task title (max 500 chars) |
| SortOrder | int | Yes | Explicit ordering within template. 1-based. |
| DependsOnItemId | Guid? | No | FK → TaskTemplateItem.Id (self-reference). SET NULL on delete. |

**Navigation properties**:

| Property | Type | Relationship |
|----------|------|-------------|
| TaskTemplate | TaskTemplate | Required parent |
| DependsOnItem | TaskTemplateItem? | Optional self-reference |

**Indexes**:
- `IX_TaskTemplateItems_TaskTemplateId` on (TaskTemplateId)

---

## Existing Entities: Modifications

### Ticket (existing)

**Add navigation property**:

```csharp
public virtual ICollection<TicketTask> Tasks { get; set; } = new List<TicketTask>();
```

### NotificationEventType (existing value object)

**Add constants**:

```csharp
public const string TASK_ASSIGNED_USER = "task_assigned_user";
public const string TASK_ASSIGNED_TEAM = "task_assigned_team";
public const string TASK_COMPLETED = "task_completed";
```

### ActivityEventType (existing static class)

**Add constants**:

```csharp
public const string TaskCreated = "task_created";
public const string TaskAssigned = "task_assigned";
public const string TaskDueDateChanged = "task_due_date_changed";
public const string TaskDependencyChanged = "task_dependency_changed";
public const string TaskUnblocked = "task_unblocked";
public const string TaskCompleted = "task_completed";
public const string TaskReopened = "task_reopened";
public const string TaskDeleted = "task_deleted";
```

### BuiltInEmailTemplate (existing value object)

**Add templates**:

| DeveloperName | Subject Template | Purpose |
|---------------|-----------------|---------|
| email_task_assigned_user | "Task assigned to you on ticket #{{ Target.TicketId }}" | Assigned-to-user notification |
| email_task_assigned_team | "Task assigned to {{ Target.TeamName }} on ticket #{{ Target.TicketId }}" | Assigned-to-team notification |
| email_task_completed | "Task completed on ticket #{{ Target.TicketId }}" | Task completion notification |

### IAppDbContext (existing interface)

**Add DbSet properties**:

```csharp
public DbSet<TicketTask> TicketTasks { get; }
public DbSet<TaskTemplate> TaskTemplates { get; }
public DbSet<TaskTemplateItem> TaskTemplateItems { get; }
```

---

## State Transitions

### Task Status Lifecycle

```
                    ┌─────────────────────────────────────┐
                    │                                     │
                    ▼                                     │
  [Created] ──► OPEN ◄──────────────────────────── CLOSED
                 │  ▲                                ▲
                 │  │                                │
                 │  └── (dependency resolved) ──┐    │
                 │                              │    │
                 └──► BLOCKED (derived) ────────┘    │
                        │                            │
                        └── (ticket force-close) ────┘
```

**Transitions**:
1. **Created → Open**: Default on creation (if no unresolved dependency)
2. **Created → Blocked**: On creation with dependency on non-Closed task (status is still "open" in DB; Blocked is derived)
3. **Open → Closed**: Staff marks complete, OR ticket closure force-close
4. **Closed → Open**: Staff reopens
5. **Blocked → Open**: Dependency task closed, OR dependency removed
6. **Open → Blocked**: Dependency task reopened (status stays "open" in DB; blocking is re-derived)

**Key invariant**: The `Status` column only ever contains "open" or "closed". Blocked is always derived from the dependency relationship at query time.

### Notification Trigger Rules

| Event | Condition | Notification Type | Recipients |
|-------|-----------|-------------------|------------|
| Task assigned to user | Task is Open (not Blocked) | TASK_ASSIGNED_USER | Assigned user + ticket followers |
| Task assigned to team | Task is Open (not Blocked) | TASK_ASSIGNED_TEAM | Team members + ticket followers |
| Task unblocked (has user assignee) | Dependency resolved | TASK_ASSIGNED_USER | Assigned user + ticket followers |
| Task unblocked (has team assignee) | Dependency resolved | TASK_ASSIGNED_TEAM | Team members + ticket followers |
| Task completed | Status → Closed | TASK_COMPLETED | Assignee + ticket followers |
| Task Blocked | Any | (none) | (no notifications while blocked) |

---

## Migration Plan

**Migration name**: `AddTicketTasks`

**Tables created**:
1. `TicketTasks` — with all columns, FKs, indexes, soft delete filter
2. `TaskTemplates` — with all columns, soft delete filter
3. `TaskTemplateItems` — with all columns, FKs, indexes, soft delete filter

**Seed data**:
- Email templates for task notifications (3 templates)
- Default notification preferences for task events (enabled for all users)

**No data migration needed** — this is a new feature with no existing data to transform.
