# API Contracts: Ticket Tasks

**Branch**: `007-ticket-tasks` | **Date**: 2026-02-11

## Conventions

- Razor Page handlers use `?handler=HandlerName` query string for POST actions
- All IDs in Application/Web layers use `ShortGuid`; Domain uses `Guid`
- All responses follow `CommandResponseDto<T>` / `QueryResponseDto<T>` patterns
- All commands go through Mediator pipeline (validation → handler → response)
- AJAX endpoints return JSON; page loads return Razor views

---

## 1. Ticket Detail Page — Task Operations

**Page**: `Areas/Staff/Pages/Tickets/Details.cshtml(.cs)`  
**Base URL**: `/staff/tickets/{ticketId}`

### GET (existing, extended)

Returns the ticket detail page with the Tasks section populated.

**Additional query data** (added to existing `OnGet`):
```
TicketTaskDto[] Tasks — ordered by SortOrder
TaskTemplateListItemDto[] AvailableTemplates — active templates for template picker
```

### POST ?handler=CreateTask

Creates a new task on the ticket.

**Request** (JSON):
```json
{
  "title": "string (required, max 500)"
}
```

**Response** (JSON):
```json
{
  "success": true,
  "result": {
    "id": "ShortGuid",
    "title": "string",
    "status": "open",
    "sortOrder": 6,
    "isBlocked": false
  }
}
```

**Command**: `CreateTicketTask.Command`

---

### POST ?handler=UpdateTask

Updates task properties (title, assignee, team, due date, dependency).

**Request** (JSON):
```json
{
  "taskId": "ShortGuid (required)",
  "title": "string? (if updating)",
  "assigneeId": "ShortGuid? (null to unassign)",
  "owningTeamId": "ShortGuid? (null to unassign)",
  "dueAt": "ISO 8601 datetime? (null to clear)",
  "dependsOnTaskId": "ShortGuid? (null to remove dependency)"
}
```

**Response** (JSON):
```json
{
  "success": true,
  "result": {
    "id": "ShortGuid",
    "title": "string",
    "status": "open",
    "assigneeId": "ShortGuid?",
    "assigneeName": "string?",
    "owningTeamId": "ShortGuid?",
    "owningTeamName": "string?",
    "dueAt": "ISO 8601?",
    "dependsOnTaskId": "ShortGuid?",
    "dependsOnTaskTitle": "string?",
    "isBlocked": false,
    "sortOrder": 3
  }
}
```

**Command**: `UpdateTicketTask.Command`

**Validation**:
- Title: not empty, max 500 chars (if provided)
- AssigneeId: must exist and be active (if provided)
- OwningTeamId: must exist (if provided)
- DependsOnTaskId: must be a task on the same ticket, must not create circular dependency

---

### POST ?handler=CompleteTask

Marks a task as Closed.

**Request** (JSON):
```json
{
  "taskId": "ShortGuid (required)"
}
```

**Response** (JSON):
```json
{
  "success": true,
  "result": {
    "id": "ShortGuid",
    "status": "closed",
    "closedAt": "ISO 8601",
    "unblockedTasks": [
      {
        "id": "ShortGuid",
        "title": "string",
        "isBlocked": false
      }
    ]
  }
}
```

**Command**: `CompleteTicketTask.Command`

**Validation**:
- Task must not be Blocked (dependency must be resolved)

**Side effects**:
- Sets `ClosedAt` and `ClosedByStaffId`
- Checks if any dependent tasks become unblocked → raises `TicketTaskUnblockedEvent`
- Returns list of newly unblocked tasks for UI update

---

### POST ?handler=ReopenTask

Reopens a Closed task.

**Request** (JSON):
```json
{
  "taskId": "ShortGuid (required)"
}
```

**Response** (JSON):
```json
{
  "success": true,
  "result": {
    "id": "ShortGuid",
    "status": "open",
    "reblockedTasks": [
      {
        "id": "ShortGuid",
        "title": "string",
        "isBlocked": true
      }
    ]
  }
}
```

**Command**: `ReopenTicketTask.Command`

**Side effects**:
- Clears `ClosedAt` and `ClosedByStaffId`
- Checks if any tasks depend on this one → they become Blocked again
- Returns list of newly re-blocked tasks for UI update

---

### POST ?handler=DeleteTask

Deletes a task (soft delete).

**Request** (JSON):
```json
{
  "taskId": "ShortGuid (required)"
}
```

**Response** (JSON):
```json
{
  "success": true,
  "result": {
    "deletedTaskId": "ShortGuid",
    "unblockedTasks": [
      {
        "id": "ShortGuid",
        "title": "string",
        "isBlocked": false
      }
    ]
  }
}
```

**Command**: `DeleteTicketTask.Command`

**Authorization**: Requires ticket-delete permission (`TicketPermissionService`)

**Side effects**:
- Soft deletes the task
- Any tasks depending on this one have `DependsOnTaskId` set to null → become Open
- Returns list of newly unblocked tasks for UI update

---

### POST ?handler=ReorderTasks

Reorders tasks via drag-and-drop.

**Request** (JSON):
```json
{
  "orderedIds": ["ShortGuid", "ShortGuid", "..."]
}
```

**Response** (JSON):
```json
{
  "success": true
}
```

**Command**: `ReorderTicketTasks.Command`

---

### POST ?handler=ApplyTaskTemplate

Applies a task template to the ticket.

**Request** (JSON):
```json
{
  "templateId": "ShortGuid (required)"
}
```

**Response** (JSON):
```json
{
  "success": true,
  "result": {
    "createdTasks": [
      {
        "id": "ShortGuid",
        "title": "string",
        "status": "open",
        "dependsOnTaskId": "ShortGuid?",
        "dependsOnTaskTitle": "string?",
        "isBlocked": true,
        "sortOrder": 7
      }
    ]
  }
}
```

**Command**: `ApplyTaskTemplate.Command`

**Side effects**:
- Creates all template items as tasks in one atomic operation
- Appends after existing tasks (sort order continues from max existing)
- Maps template dependency relationships to new task IDs
- All tasks default to unassigned, no due date

---

## 2. Ticket Status Change — Closure Gate

**Page**: `Areas/Staff/Pages/Tickets/Details.cshtml(.cs)` (existing status change flow)

### POST ?handler=ChangeStatus (modified existing)

**Existing behavior**: Changes ticket status.

**New behavior**: If target status has StatusType = "closed" and ticket has incomplete tasks, returns a confirmation-needed response instead of proceeding.

**Request** (JSON or form — matches existing pattern):
```json
{
  "status": "string (developer name)",
  "forceCloseTasks": false
}
```

**Response when confirmation needed** (JSON):
```json
{
  "success": false,
  "needsTaskConfirmation": true,
  "incompleteTaskCount": 4,
  "message": "There are 4 tasks on this ticket that are not complete. Would you like to mark all tasks as complete and close the ticket?"
}
```

**Response on confirmation** (with `forceCloseTasks: true`):
```json
{
  "success": true,
  "result": {
    "newStatus": "closed",
    "tasksForceCompleted": 4
  }
}
```

**Command**: Extend existing `UpdateTicketStatus.Command` with `bool ForceCloseTasks` property.

---

## 3. Staff Tasks Page

**Page**: `Areas/Staff/Pages/Tasks/Index.cshtml(.cs)`  
**Base URL**: `/staff/tasks`

### GET /staff/tasks

Returns the Tasks page with the default view (My Tasks, Open only).

**Query parameters**:
```
view: string — built-in view name (default: "my-tasks")
         Values: "unassigned", "my-tasks", "created-by-me", "team-tasks", "overdue", "all"
search: string? — title search term
page: int — pagination (default: 1)
pageSize: int — items per page (default: 25)
sort: string? — sort field (default: view-specific)
sortDir: string? — "asc" or "desc"
```

**Response data** (Razor view model):
```
TaskListItemDto[] Tasks
  - Id, Title, Status, IsBlocked
  - AssigneeId, AssigneeName
  - OwningTeamId, OwningTeamName
  - DueAt, IsOverdue
  - DependsOnTaskTitle
  - TicketId, TicketTitle, TicketAssigneeName
  - CreatedAt
int TotalCount
string CurrentView
string? SearchTerm
```

**Query**: `GetTasks.Query`

### POST /staff/tasks?handler=UpdateTaskAssignee

**Request** (JSON):
```json
{
  "taskId": "ShortGuid",
  "assigneeId": "ShortGuid?",
  "owningTeamId": "ShortGuid?"
}
```

**Command**: Reuses `UpdateTicketTask.Command` (assignee fields only)

### POST /staff/tasks?handler=UpdateTaskDueDate

**Request** (JSON):
```json
{
  "taskId": "ShortGuid",
  "dueAt": "ISO 8601?"
}
```

**Command**: Reuses `UpdateTicketTask.Command` (due date field only)

### POST /staff/tasks?handler=ToggleTaskStatus

**Request** (JSON):
```json
{
  "taskId": "ShortGuid"
}
```

**Command**: Calls `CompleteTicketTask.Command` or `ReopenTicketTask.Command` based on current status.

---

## 4. Admin Task Templates

**Pages**: `Areas/Admin/Pages/Tickets/TaskTemplates/`  
**Base URL**: `/admin/tickets/task-templates`

### GET /admin/tickets/task-templates (Index)

Lists all task templates.

**Query**: `GetTaskTemplates.Query`  
**Response model**: `TaskTemplateListItemDto[]` (Id, Name, Description, ItemCount, IsActive)

### GET /admin/tickets/task-templates/create (Create)

Renders the create template form.

### POST /admin/tickets/task-templates/create (Create)

**Form fields**:
```
Name: string (required, max 200)
Description: string? (max 1000)
Items[]: { Title: string, SortOrder: int, DependsOnIndex: int? }
```

**Command**: `CreateTaskTemplate.Command`

### GET /admin/tickets/task-templates/edit/{id} (Edit)

**Query**: `GetTaskTemplateById.Query`

### POST /admin/tickets/task-templates/edit/{id} (Edit)

**Form fields**: Same as Create.

**Command**: `UpdateTaskTemplate.Command`

### POST /admin/tickets/task-templates?handler=Delete

**Request** (JSON):
```json
{
  "id": "ShortGuid"
}
```

**Command**: `DeleteTaskTemplate.Command`

### POST /admin/tickets/task-templates?handler=ToggleActive

**Request** (JSON):
```json
{
  "id": "ShortGuid"
}
```

**Command**: `ToggleTaskTemplateActive.Command`

---

## 5. Task Reports

**Page**: `Areas/Staff/Pages/Tasks/Reports.cshtml(.cs)`  
**Base URL**: `/staff/tasks/reports`

### GET /staff/tasks/reports

**Query parameters**:
```
startDate: ISO 8601? — date range start (default: 30 days ago)
endDate: ISO 8601? — date range end (default: now)
```

**Response model**:
```
TaskReportDto:
  - TasksCreated: int
  - TasksCompleted: int
  - TasksReopened: int
  - OpenCount: int
  - ClosedCount: int
  - OverdueCount: int
  - DailyCreated: { Date, Count }[]
  - DailyCompleted: { Date, Count }[]
```

**Query**: `GetTaskReports.Query`

---

## 6. REST API (V1)

**Controller**: `Areas/Api/Controllers/V1/TicketTasksController.cs`  
**Base URL**: `/api/v1/tickets/{ticketId}/tasks`

### GET /api/v1/tickets/{ticketId}/tasks

Lists tasks for a ticket.

**Response**: `QueryResponseDto<TicketTaskDto[]>`

### POST /api/v1/tickets/{ticketId}/tasks

Creates a task.

**Request body**: `{ title, assigneeId?, owningTeamId?, dueAt?, dependsOnTaskId? }`  
**Response**: `CommandResponseDto<TicketTaskDto>`

### PUT /api/v1/tickets/{ticketId}/tasks/{taskId}

Updates a task.

**Request body**: `{ title?, assigneeId?, owningTeamId?, dueAt?, dependsOnTaskId? }`  
**Response**: `CommandResponseDto<TicketTaskDto>`

### PUT /api/v1/tickets/{ticketId}/tasks/{taskId}/complete

Marks task as Closed.

**Response**: `CommandResponseDto<TicketTaskDto>`

### PUT /api/v1/tickets/{ticketId}/tasks/{taskId}/reopen

Marks task as Open.

**Response**: `CommandResponseDto<TicketTaskDto>`

### DELETE /api/v1/tickets/{ticketId}/tasks/{taskId}

Deletes a task.

**Response**: `CommandResponseDto<bool>`

---

## DTOs

### TicketTaskDto

```csharp
public record TicketTaskDto
{
    public ShortGuid Id { get; init; }
    public long TicketId { get; init; }
    public string Title { get; init; }
    public string Status { get; init; }           // "open" or "closed"
    public bool IsBlocked { get; init; }
    public ShortGuid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public ShortGuid? OwningTeamId { get; init; }
    public string? OwningTeamName { get; init; }
    public DateTime? DueAt { get; init; }
    public bool IsOverdue { get; init; }
    public ShortGuid? DependsOnTaskId { get; init; }
    public string? DependsOnTaskTitle { get; init; }
    public int SortOrder { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? ClosedByStaffName { get; init; }
    public ShortGuid? CreatedByStaffId { get; init; }
    public string? CreatedByStaffName { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### TaskListItemDto (Staff Tasks page — includes parent ticket context)

```csharp
public record TaskListItemDto : TicketTaskDto
{
    public string TicketTitle { get; init; }
    public string? TicketAssigneeName { get; init; }
}
```

### TaskTemplateDto

```csharp
public record TaskTemplateDto
{
    public ShortGuid Id { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public List<TaskTemplateItemDto> Items { get; init; }
}

public record TaskTemplateItemDto
{
    public ShortGuid Id { get; init; }
    public string Title { get; init; }
    public int SortOrder { get; init; }
    public ShortGuid? DependsOnItemId { get; init; }
    public string? DependsOnItemTitle { get; init; }
}
```
