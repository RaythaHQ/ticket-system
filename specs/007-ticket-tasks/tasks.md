# Tasks: Ticket Tasks

**Input**: Design documents from `/specs/007-ticket-tasks/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec. Test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Domain**: `src/App.Domain/`
- **Application**: `src/App.Application/`
- **Infrastructure**: `src/App.Infrastructure/`
- **Web**: `src/App.Web/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create domain-layer primitives that all user stories depend on

- [x] T001 Create TicketTaskStatus value object with Open/Closed values in src/App.Domain/ValueObjects/TicketTaskStatus.cs — follow TicketStatus pattern (ValueObject base, From(), Label/DeveloperName, SupportedTypes, implicit string conversion, constants OPEN="open" CLOSED="closed"). Also create TicketTaskStatusNotFoundException in src/App.Domain/Exceptions/TicketTaskStatusNotFoundException.cs following TicketStatusNotFoundException pattern
- [x] T002 Create TicketTask entity in src/App.Domain/Entities/TicketTask.cs — BaseAuditableEntity with properties: TicketId (long), Title (string), Status (string, default OPEN), AssigneeId (Guid?), OwningTeamId (Guid?), DueAt (DateTime?), DependsOnTaskId (Guid? self-ref), SortOrder (int), ClosedAt (DateTime?), ClosedByStaffId (Guid?), CreatedByStaffId (Guid?). Navigation properties: Ticket, Assignee, OwningTeam, DependsOnTask, DependentTasks, CreatedByStaff, ClosedByStaff. NotMapped: StatusValue, IsBlocked, IsOverdue
- [x] T003 [P] Create all task domain events in src/App.Domain/Events/ — TicketTaskCreatedEvent.cs, TicketTaskAssignedEvent.cs, TicketTaskCompletedEvent.cs, TicketTaskReopenedEvent.cs, TicketTaskDeletedEvent.cs, TicketTaskUnblockedEvent.cs, TicketTaskDueDateChangedEvent.cs, TicketTaskDependencyChangedEvent.cs. Follow existing event pattern (record classes inheriting BaseEvent/IDomainEvent with relevant entity data)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: EF Core persistence, DbContext, migration, and shared DTOs that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T00- [x] T004 Add Tasks navigation property to Ticket entity in src/App.Domain/Entities/Ticket.cs — add `public virtual ICollection<TicketTask> Tasks { get; set; } = new List<TicketTask>();`
- [x] T00- [x] T005 [P] Create TicketTaskConfiguration in src/App.Infrastructure/Persistence/Configurations/TicketTaskConfiguration.cs — follow TicketConfiguration pattern: ValueGeneratedNever for Id, configure all properties (Title required max 500, Status required max 50), all relationships with correct delete behaviors (Ticket=Cascade, User FKs=SetNull, DependsOnTask=SetNull, Team=SetNull), soft delete query filter, indexes on TicketId, AssigneeId, OwningTeamId, Status, DueAt (partial where not null), DependsOnTaskId, CreatedByStaffId
- [x] T00- [x] T006 Add TicketTasks DbSet to IAppDbContext in src/App.Application/Common/Interfaces/IRaythaDbContext.cs and to AppDbContext in src/App.Infrastructure/Persistence/AppDbContext.cs — add `DbSet<TicketTask> TicketTasks` to both
- [x] T00- [x] T007 Generate EF Core migration — run `dotnet ef migrations add AddTicketTasks --startup-project ../App.Web` from src/App.Infrastructure. Verify generated migration creates TicketTasks table with all columns, foreign keys, and indexes
- [x] T00- [x] T008 [P] Create TicketTaskDto with static MapFrom method in src/App.Application/TicketTasks/TicketTaskDto.cs — include all fields per contracts/task-endpoints.md (Id as ShortGuid, TicketId, Title, Status, IsBlocked, AssigneeId/Name, OwningTeamId/Name, DueAt, IsOverdue, DependsOnTaskId/Title, SortOrder, ClosedAt, ClosedByStaffName, CreatedByStaffId/Name, CreatedAt). Add TaskListItemDto extending with TicketTitle and TicketAssigneeName
- [x] T00- [x] T009 [P] Add RouteNames constants for Tasks in src/App.Web/Areas/Staff/Pages/Shared/RouteNames.cs — add nested Tasks class with Index, Reports route constants

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 — Managing Tasks on a Ticket (Priority: P1) MVP

**Goal**: Staff can create, edit, reorder, complete, reopen, and delete tasks inline on the Ticket Detail View. Closing a ticket with incomplete tasks shows a confirmation to force-close all tasks.

**Independent Test**: Create a ticket, add several tasks inline, edit their properties, reorder them, complete them, reopen one, delete one. Close the ticket and confirm force-close prompt works. All without leaving the Ticket Detail View.

### Implementation for User Story 1

- [x] T010 [P] [US1] Create CreateTicketTask command in src/App.Application/TicketTasks/Commands/CreateTicketTask.cs — Command: TicketId (long), Title (string). Validator: title not empty, max 500, ticket exists. Handler: create TicketTask with status=open, SortOrder=max+1, CreatedByStaffId from ICurrentUser. Raise TicketTaskCreatedEvent. Return CommandResponseDto<TicketTaskDto>
- [x] T011 [P] [US1] Create UpdateTicketTask command in src/App.Application/TicketTasks/Commands/UpdateTicketTask.cs — Command: TaskId (ShortGuid), Title?, AssigneeId?, OwningTeamId?, DueAt?, DependsOnTaskId?. Validator: task exists, title max 500 if provided, assignee exists+active if provided, team exists if provided. Handler: update changed fields, raise appropriate domain events (TicketTaskAssignedEvent if assignee changed, TicketTaskDueDateChangedEvent if due date changed, TicketTaskDependencyChangedEvent if dependency changed). If assignee set, infer team from membership. Return CommandResponseDto<TicketTaskDto>
- [x] T012 [P] [US1] Create CompleteTicketTask command in src/App.Application/TicketTasks/Commands/CompleteTicketTask.cs — Command: TaskId (ShortGuid). Validator: task exists, task is not Blocked (dependency must be resolved). Handler: set Status=CLOSED, ClosedAt=UtcNow, ClosedByStaffId. Raise TicketTaskCompletedEvent. Return CommandResponseDto<TicketTaskDto> with list of affected dependent tasks
- [x] T013 [P] [US1] Create ReopenTicketTask command in src/App.Application/TicketTasks/Commands/ReopenTicketTask.cs — Command: TaskId (ShortGuid). Validator: task exists, task is Closed. Handler: set Status=OPEN, clear ClosedAt/ClosedByStaffId. Raise TicketTaskReopenedEvent. Return CommandResponseDto<TicketTaskDto> with list of tasks that become re-blocked
- [x] T014 [P] [US1] Create DeleteTicketTask command in src/App.Application/TicketTasks/Commands/DeleteTicketTask.cs — Command: TaskId (ShortGuid). Validator: task exists. Handler: soft delete, set DependsOnTaskId=null on any dependent tasks (unblock them). Raise TicketTaskDeletedEvent. Permission check: require ticket-delete permission via TicketPermissionService. Return CommandResponseDto with list of unblocked tasks
- [x] T015 [P] [US1] Create ReorderTicketTasks command in src/App.Application/TicketTasks/Commands/ReorderTicketTasks.cs — Command: TicketId (long), OrderedIds (List<ShortGuid>). Handler: update SortOrder = index+1 for each task. Follow ReorderTicketPriorities pattern
- [x] T016 [US1] Create GetTasksByTicketId query in src/App.Application/TicketTasks/Queries/GetTasksByTicketId.cs — Query: TicketId (long). Handler: load all tasks for ticket with includes (Assignee, OwningTeam, DependsOnTask, CreatedByStaff, ClosedByStaff), AsNoTracking, order by SortOrder. Map to TicketTaskDto[]. Compute IsBlocked by checking DependsOnTask.Status
- [x] T017 [US1] Create _TasksSection.cshtml Razor partial in src/App.Web/Areas/Staff/Pages/Tickets/_TasksSection.cshtml — staff-card with "Tasks" header. Inline add-task input (title only, enter to create). Task list with: checkbox toggle (open/closed), title (editable inline), assignee display, due date display, drag handle. Delete button (permission-gated). Empty state when no tasks. SortableJS drag handle on each task row
- [x] T018 [US1] Add task POST handlers to Details page model in src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs — OnPostCreateTask, OnPostUpdateTask, OnPostCompleteTask, OnPostReopenTask, OnPostDeleteTask, OnPostReorderTasks. Each sends Mediator command, returns JsonResult. Load tasks in OnGet for initial page render via GetTasksByTicketId query
- [x] T019 [US1] Integrate Tasks section into ticket detail view in src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml — add `<partial name="_TasksSection" />` in left column after Comments card. Pass tasks data from page model. Include SortableJS script reference and ticket-tasks.js
- [x] T020 [US1] Create ticket-tasks.js in src/App.Web/wwwroot/staff/js/ticket-tasks.js — inline task creation (POST to CreateTask handler on Enter), inline title editing (click-to-edit, blur/Enter to save via UpdateTask), checkbox toggle for complete/reopen (POST to CompleteTask/ReopenTask), delete button (POST to DeleteTask with confirmation), SortableJS initialization with drag handle and reorder POST, assignee inline dropdown, due date inline datetime-local picker (reuse Snooze date picker pattern). Update UI optimistically on each action
- [x] T021 [US1] Create BulkCloseTicketTasks command in src/App.Application/TicketTasks/Commands/BulkCloseTicketTasks.cs — Command: TicketId (long). Handler: load all non-closed tasks for ticket, set all to Status=CLOSED with ClosedAt/ClosedByStaffId. Raise TicketTaskCompletedEvent for each. Return count of tasks closed
- [x] T022 [US1] Extend ticket status change to implement closure gate — modify the existing status change handler (find the command that changes ticket status). Add ForceCloseTasks flag to command. In handler: when target status has StatusType=closed (check via TicketStatusConfig), query for incomplete tasks. If incomplete tasks exist and ForceCloseTasks=false, return needs-confirmation response. If ForceCloseTasks=true, call BulkCloseTicketTasks first, then proceed with status change. Add individual activity log entries for each force-closed task
- [x] T023 [US1] Add closure gate UI to ticket detail view — in the status change JS flow, intercept the response when needsTaskConfirmation=true. Display inline confirmation message: "There are X tasks on this ticket that are not complete. Would you like to mark all tasks as complete and close the ticket?" with Confirm/Cancel buttons. On confirm, re-submit with forceCloseTasks=true. On cancel, revert status dropdown

**Checkpoint**: User Story 1 complete — tasks can be created, edited, reordered, completed, reopened, deleted inline. Ticket closure gate works. This is the MVP.

---

## Phase 4: User Story 2 — Task Dependencies and Blocking (Priority: P2)

**Goal**: Tasks can depend on another task. Blocked tasks are visually distinct, not actionable, and auto-transition when dependencies resolve.

**Independent Test**: Create two tasks, set dependency from B to A, verify B appears blocked. Close A, verify B becomes Open. Reopen A, verify B is blocked again. Remove dependency, verify B is Open.

**Depends on**: US1 (core task CRUD must exist)

### Implementation for User Story 2

- [x] T024 [US2] Add circular dependency validation to UpdateTicketTask validator in src/App.Application/TicketTasks/Commands/UpdateTicketTask.cs — when DependsOnTaskId is set, validate: target task is on same ticket, walk dependency chain to detect cycles (A→B→C→A), reject with clear error message if circular
- [x] T025 [US2] Add unblock cascade to CompleteTicketTask handler in src/App.Application/TicketTasks/Commands/CompleteTicketTask.cs — after closing a task, query for tasks that depend on it (DependentTasks). For each that was Blocked and is now unblocked: raise TicketTaskUnblockedEvent. Return unblocked task list in response
- [x] T026 [US2] Add reblock cascade to ReopenTicketTask handler in src/App.Application/TicketTasks/Commands/ReopenTicketTask.cs — after reopening a task, query for tasks that depend on it. They become Blocked again. Return re-blocked task list in response
- [x] T027 [US2] Add unblock cascade to DeleteTicketTask handler in src/App.Application/TicketTasks/Commands/DeleteTicketTask.cs — before soft-deleting, set DependsOnTaskId=null on dependent tasks (they become Open). Raise TicketTaskUnblockedEvent for each
- [x] T028 [US2] Update _TasksSection.cshtml with blocked task visual states in src/App.Web/Areas/Staff/Pages/Tickets/_TasksSection.cshtml — blocked tasks: muted/greyed opacity, lock icon (or chain icon), inline text "Blocked by: [Task Title]", checkbox disabled/hidden. Distinguish from Open and Closed visually
- [x] T029 [US2] Add dependency picker to ticket-tasks.js in src/App.Web/wwwroot/staff/js/ticket-tasks.js — inline dropdown to select dependency (list other tasks on same ticket, excluding self and tasks that would create cycles). Show "Blocked by: [Title]" text. Update UI when dependency changes. Handle unblock/reblock cascade responses from server

**Checkpoint**: Dependencies and blocking fully functional. Blocked tasks are unmistakable and auto-transition.

---

## Phase 5: User Story 3 — Staff Tasks Page with Built-in Views (Priority: P3)

**Goal**: Centralized Tasks page with built-in views (My Tasks, Team Tasks, Unassigned, Created by Me, Overdue, All Tasks), search, and inline actions.

**Independent Test**: Create tasks across multiple tickets with various assignees, navigate to Tasks page, verify each view filters correctly, inline actions work, search/sort behaves as expected.

**Depends on**: US1 (tasks must exist)

### Implementation for User Story 3

- [x] T030 [US3] Create GetTasks query with built-in view filters in src/App.Application/TicketTasks/Queries/GetTasks.cs — Query: View (enum: MyTasks, TeamTasks, Unassigned, CreatedByMe, Overdue, AllTasks), Search (string?), Page, PageSize, Sort, SortDir. Handler: base query on TicketTasks with includes (Ticket, Ticket.Assignee, Assignee, OwningTeam, DependsOnTask). Apply view filter (MyTasks: AssigneeId=currentUser, TeamTasks: OwningTeamId in user's teams, Unassigned: AssigneeId=null AND OwningTeamId=null, CreatedByMe: CreatedByStaffId=currentUser, Overdue: Status=open AND DueAt<UtcNow, AllTasks: no status filter). Default shows Open tasks only (except AllTasks). Apply title search. Apply sorting (sensible defaults per view). Paginate. Map to TaskListItemDto[] (DTO already created in T008)
- [x] T031 [US3] Create Tasks Index page model in src/App.Web/Areas/Staff/Pages/Tasks/Index.cshtml.cs — inherit BaseStaffPageModel. OnGet: parse view/search/sort params, send GetTasks query, populate view model. POST handlers: OnPostUpdateTaskAssignee, OnPostUpdateTaskDueDate, OnPostToggleTaskStatus (calls CompleteTicketTask or ReopenTicketTask based on current status). Set ViewData["ActiveMenu"] = "Tasks"
- [x] T032 [US3] Create Tasks Index Razor view in src/App.Web/Areas/Staff/Pages/Tasks/Index.cshtml — staff-card layout. View selector (sidebar or tabs for built-in views). Search bar. Task list (not necessarily a table — use clear, intuitive layout). Each task shows: title, status toggle, parent ticket number + title (clickable link), parent ticket assignee, task assignee, due date, blocked indicator. Inline action controls for assignee, due date, status toggle. Pagination. Empty state per view. Include inline editing JS
- [x] T033 [US3] Add Tasks nav item to Staff sidebar in src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml — add "Tasks" link in main navigation section using RouteNames.Tasks.Index. Set active state based on ViewData["ActiveMenu"]

**Checkpoint**: Staff Tasks page operational with all built-in views, search, and inline actions.

---

## Phase 6: User Story 4 — Task Notifications (Priority: P4)

**Goal**: Notification system sends task-related notifications through existing framework. No notifications while blocked; assignment notifications fire when task becomes Open.

**Independent Test**: Assign a task, verify notification. Create blocked task with assignee, verify no notification. Resolve dependency, verify notification fires. Complete task, verify completion notification.

**Depends on**: US1, US2 (notifications need tasks and dependency transitions)

### Implementation for User Story 4

- [ ] T034 [P] [US4] Add task notification constants to NotificationEventType in src/App.Domain/ValueObjects/NotificationEventType.cs — add TASK_ASSIGNED_USER="task_assigned_user", TASK_ASSIGNED_TEAM="task_assigned_team", TASK_COMPLETED="task_completed"
- [ ] T035 [P] [US4] Create email_task_assigned_user.liquid in src/App.Domain/Entities/DefaultTemplates/email_task_assigned_user.liquid — Liquid template: greeting, task title, ticket reference (#TicketId - TicketTitle), link to ticket. Follow email_ticket_commentadded.liquid structure
- [ ] T036 [P] [US4] Create email_task_assigned_team.liquid in src/App.Domain/Entities/DefaultTemplates/email_task_assigned_team.liquid — Liquid template: greeting with team name, task title, ticket reference, link to ticket
- [ ] T037 [P] [US4] Create email_task_completed.liquid in src/App.Domain/Entities/DefaultTemplates/email_task_completed.liquid — Liquid template: greeting, task title completed, who completed it, ticket reference, link to ticket
- [ ] T038 [US4] Register task email templates in BuiltInEmailTemplate value object — add entries for email_task_assigned_user, email_task_assigned_team, email_task_completed. Find BuiltInEmailTemplate.cs and add static properties following existing pattern
- [ ] T039 [US4] Create TicketTaskNotificationHandlers in src/App.Application/TicketTasks/EventHandlers/TicketTaskNotificationHandlers.cs — implement INotificationHandler for: TicketTaskAssignedEvent (send TASK_ASSIGNED_USER or TASK_ASSIGNED_TEAM based on assignment type, only if task is not Blocked), TicketTaskCompletedEvent (send TASK_COMPLETED to assignee + ticket followers), TicketTaskUnblockedEvent (send assignment notification if task has assignee, since it just became actionable). Collect recipients from assignee and ticket followers. Use IInAppNotificationService and IEmailer with Liquid templates
- [ ] T040 [US4] Add migration seed data for task notification templates and preferences — create migration or seeder to insert email templates for 3 task notification types. Ensure notifications are enabled by default for all existing users via NotificationPreference entries

**Checkpoint**: All task notifications operational through existing framework.

---

## Phase 7: User Story 5 — Task Templates (Priority: P5)

**Goal**: Admins create reusable task templates. Staff apply templates to insert pre-configured task lists with order and dependencies.

**Independent Test**: Create template with ordered tasks and dependencies in Admin. Apply to ticket in Staff Portal. Verify all tasks, order, and dependencies created correctly.

**Depends on**: US1 (tasks), US2 (dependencies for template dependencies)

### Implementation for User Story 5

- [ ] T041 [P] [US5] Create TaskTemplate entity in src/App.Domain/Entities/TaskTemplate.cs — BaseAuditableEntity. Properties: Name (string, required), Description (string?), IsActive (bool, default true). Navigation: Items (ICollection<TaskTemplateItem>)
- [ ] T042 [P] [US5] Create TaskTemplateItem entity in src/App.Domain/Entities/TaskTemplateItem.cs — BaseAuditableEntity. Properties: TaskTemplateId (Guid), Title (string, required), SortOrder (int), DependsOnItemId (Guid?, self-ref). Navigation: TaskTemplate, DependsOnItem
- [ ] T043 [P] [US5] Create TaskTemplateConfiguration in src/App.Infrastructure/Persistence/Configurations/TaskTemplateConfiguration.cs — Name required max 200, Description max 1000, soft delete filter, HasMany Items with cascade delete
- [ ] T044 [P] [US5] Create TaskTemplateItemConfiguration in src/App.Infrastructure/Persistence/Configurations/TaskTemplateItemConfiguration.cs — Title required max 500, FK to TaskTemplate cascade, FK to DependsOnItem set null, index on TaskTemplateId, soft delete filter
- [ ] T045 [US5] Add TaskTemplates and TaskTemplateItems DbSets to IAppDbContext in src/App.Application/Common/Interfaces/IRaythaDbContext.cs and AppDbContext in src/App.Infrastructure/Persistence/AppDbContext.cs
- [ ] T046 [US5] Generate EF Core migration AddTaskTemplates — run `dotnet ef migrations add AddTaskTemplates --startup-project ../App.Web` from src/App.Infrastructure
- [ ] T047 [P] [US5] Create CreateTaskTemplate command in src/App.Application/TicketConfig/Commands/CreateTaskTemplate.cs — Command: Name, Description?, Items[]{Title, SortOrder, DependsOnIndex?}. Validator: name required max 200, at least one item, titles required max 500, no circular deps. Handler: create template + items, map DependsOnIndex to actual item IDs. Return CommandResponseDto<ShortGuid>
- [ ] T048 [P] [US5] Create UpdateTaskTemplate command in src/App.Application/TicketConfig/Commands/UpdateTaskTemplate.cs — replace all items (delete existing, create new). Same validation as create
- [ ] T049 [P] [US5] Create DeleteTaskTemplate command in src/App.Application/TicketConfig/Commands/DeleteTaskTemplate.cs — soft delete template and items
- [ ] T050 [P] [US5] Create ToggleTaskTemplateActive command in src/App.Application/TicketConfig/Commands/ToggleTaskTemplateActive.cs — toggle IsActive flag
- [ ] T051 [P] [US5] Create GetTaskTemplates query in src/App.Application/TicketConfig/Queries/GetTaskTemplates.cs — list all templates with item count, ordered by name
- [ ] T052 [P] [US5] Create GetTaskTemplateById query in src/App.Application/TicketConfig/Queries/GetTaskTemplateById.cs — load template with items ordered by SortOrder, include dependency info
- [ ] T053 [US5] Create ApplyTaskTemplate command in src/App.Application/TicketTasks/Commands/ApplyTaskTemplate.cs — Command: TicketId (long), TemplateId (ShortGuid). Handler: load template with items, calculate starting SortOrder (max existing + 1), create TicketTask for each item, map DependsOnItemId to new task IDs for dependencies. Atomic operation (all or nothing). Raise TicketTaskCreatedEvent for each. Return list of created TicketTaskDto
- [ ] T054 [US5] Add RouteNames constants for TaskTemplates in src/App.Web/Areas/Admin/Pages/Shared/RouteNames.cs — add nested TaskTemplates class with Index, Create, Edit route constants
- [ ] T055 [US5] Create Admin TaskTemplates Index page in src/App.Web/Areas/Admin/Pages/Tickets/TaskTemplates/Index.cshtml and Index.cshtml.cs — BaseAdminPageModel, Authorize with MANAGE_SYSTEM_SETTINGS_PERMISSION. Table list of templates (Name, Description, Item Count, Active toggle, Edit/Delete actions). Set ViewData["ActiveMenu"]="TaskTemplates", ViewData["ExpandTicketingMenu"]=true. Follow Priorities/Statuses index pattern
- [ ] T056 [US5] Create Admin TaskTemplates Create page in src/App.Web/Areas/Admin/Pages/Tickets/TaskTemplates/Create.cshtml and Create.cshtml.cs — form with Name, Description fields. Dynamic task item list: add/remove items, each with Title input and dependency dropdown (select from other items). SortableJS for reordering items. Submit creates template via Mediator
- [ ] T057 [US5] Create Admin TaskTemplates Edit page in src/App.Web/Areas/Admin/Pages/Tickets/TaskTemplates/Edit.cshtml and Edit.cshtml.cs — same form as Create, pre-populated with existing data. Submit updates via Mediator
- [ ] T058 [US5] Add Task Templates nav item to Admin sidebar in src/App.Web/Areas/Admin/Pages/Shared/SidebarLayout.cshtml — add under Ticketing submenu, after Statuses. Permission: MANAGE_SYSTEM_SETTINGS_PERMISSION. Use RouteNames.TaskTemplates.Index
- [ ] T059 [US5] Add template picker to Tasks section on ticket detail in src/App.Web/Areas/Staff/Pages/Tickets/_TasksSection.cshtml — add "Apply Template" button/dropdown next to "Add Task" input. Show list of active templates. On selection, POST to ApplyTaskTemplate handler
- [ ] T060 [US5] Add ApplyTaskTemplate handler to Details.cshtml.cs in src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs — OnPostApplyTaskTemplate: send ApplyTaskTemplate command via Mediator, return JSON with created tasks for UI update

**Checkpoint**: Task templates CRUD in Admin, template application on tickets in Staff portal.

---

## Phase 8: User Story 6 — Activity Logging (Priority: P6)

**Goal**: All task events logged in existing Activity Log for auditability. Events appear in ticket change log and real-time activity stream.

**Independent Test**: Perform each task action (create, assign, due date, dependency, complete, reopen, delete) and verify activity log entries appear on ticket timeline.

**Depends on**: US1 (task events must exist)

### Implementation for User Story 6

- [ ] T061 [P] [US6] Add task activity event type constants to ActivityEventType in src/App.Application/Common/Interfaces/IActivityStreamService.cs — add TaskCreated, TaskAssigned, TaskDueDateChanged, TaskDependencyChanged, TaskUnblocked, TaskCompleted, TaskReopened, TaskDeleted
- [ ] T062 [US6] Create TicketTaskActivityHandlers in src/App.Application/TicketTasks/EventHandlers/TicketTaskActivityHandlers.cs — implement INotificationHandler for all 8 task domain events. For each: (1) create TicketChangeLogEntry with descriptive message (e.g., "Task created: [title]", "Task assigned to [name]", "Task completed", etc.) and persist via _db.TicketChangeLogEntries.Add, (2) broadcast ActivityEvent via IActivityStreamService.BroadcastActivityAsync with type, message, actor, ticket context. Include previous/new values where applicable (e.g., "Due date changed from X to Y", "Assigned from [old] to [new]")

**Checkpoint**: Full audit trail for all task actions in persisted change log and real-time activity stream.

---

## Phase 9: User Story 7 — Task Reports (Priority: P7)

**Goal**: Tasks Reports section with date range filters, summary statistics, and visual charts following existing Ticket Reports patterns.

**Independent Test**: Create, complete, reopen tasks over a date range. Navigate to Task Reports. Verify all metrics and date range filters work correctly.

**Depends on**: US1 (tasks must exist with sufficient data)

### Implementation for User Story 7

- [ ] T063 [US7] Create GetTaskReports query in src/App.Application/TicketTasks/Queries/GetTaskReports.cs — Query: StartDate, EndDate. Handler: count tasks created in range (CreationTime), completed in range (ClosedAt), reopened in range (use change log entries for reopen events). Current open vs closed counts. Overdue count (open + DueAt < UtcNow). Daily breakdown for chart data. Return TaskReportDto
- [ ] T064 [US7] Create Tasks Reports page model in src/App.Web/Areas/Staff/Pages/Tasks/Reports.cshtml.cs — BaseStaffPageModel. OnGet: parse date range (default last 30 days), send GetTaskReports query. Set ViewData["ActiveMenu"]="Tasks"
- [ ] T065 [US7] Create Tasks Reports Razor view in src/App.Web/Areas/Staff/Pages/Tasks/Reports.cshtml — staff-card layout. Date range picker (start/end date inputs). Summary stat cards (tasks created, completed, reopened, open, closed, overdue) using staff-stat-card pattern. Charts for daily created/completed trends and open vs closed distribution. Follow existing Ticket Reports visual patterns

**Checkpoint**: Task reports operational with all metrics and charts.

---

## Phase 10: User Story 8 — Ticket Views and Task Column (Priority: P8)

**Goal**: New ticket view conditions (Has Any Tasks, Has Incomplete Tasks) and a Tasks column in ticket lists showing completion progress.

**Independent Test**: Create ticket views with task conditions, verify correct filtering. Enable Tasks column, verify progress indicator displays accurately.

**Depends on**: US1 (tasks must exist)

### Implementation for User Story 8

- [x] T066 [US8] Add HasTasks and HasIncompleteTasks filter attributes to FilterAttributeDefinition in src/App.Application/TicketViews/FilterAttributeDefinition.cs — add two new boolean-type filter definitions: "HasTasks" (label: "Has Any Tasks") and "HasIncompleteTasks" (label: "Has Incomplete Tasks")
- [x] T067 [US8] Implement task filter conditions in ViewFilterBuilder in src/App.Application/TicketViews/Services/ViewFilterBuilder.cs — add case handlers for HasTasks (ticket.Tasks.Any()) and HasIncompleteTasks (ticket.Tasks.Any(t => t.Status == TicketTaskStatus.OPEN)). Use EF Core subquery pattern to filter tickets
- [x] T068 [US8] Add Tasks column definition in src/App.Application/TicketViews/ColumnDefinition.cs — add "Tasks" column (label: "Tasks", not clickable, not searchable). Requires query projection to include completed/total counts
- [x] T069 [US8] Extend ticket list query to include task counts — modify GetTickets query handler (or the DTO mapping) to include CompletedTaskCount and TotalTaskCount when Tasks column is selected. Use efficient subquery (Select with Count)
- [x] T070 [US8] Render Tasks column in ticket list view — find the ticket list rendering partial/view. Add Tasks column rendering: show "X / Y" format (completed / total) with compact color-coded progress indicator (green=all complete, yellow=partial, grey=none, empty/dash=no tasks). Use CSS for progress bar/badge styling consistent with staff area design

**Checkpoint**: Ticket views support task conditions and task progress column.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: REST API, cleanup, and final integration

- [x] T071 [P] Create TicketTasksController REST API in src/App.Web/Areas/Api/Controllers/V1/TicketTasksController.cs — RESTful endpoints per contracts/task-endpoints.md: GET tasks, POST create, PUT update, PUT complete, PUT reopen, DELETE. Follow existing TicketsController patterns. Use Mediator commands/queries. Authorize appropriately
- [x] T072 Verify all task domain events are raised correctly across all commands — review each command handler to ensure events fire for: created, assigned, due date changed, dependency changed, unblocked, completed, reopened, deleted. Verify BulkCloseTicketTasks raises individual events per task
- [x] T073 End-to-end validation across all user stories — verify: tasks on ticket detail (US1), dependencies and blocking (US2), staff tasks page views (US3), notifications fire correctly (US4), templates create and apply (US5), activity log entries appear (US6), reports show accurate data (US7), ticket view conditions filter correctly (US8), REST API works (all). Check closure gate with blocked tasks. Check template application to ticket with existing tasks

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — MVP, start here
- **US2 (Phase 4)**: Depends on US1 (extends task commands with dependency logic)
- **US3 (Phase 5)**: Depends on US1 (needs tasks to display), can run parallel with US2
- **US4 (Phase 6)**: Depends on US1 + US2 (needs tasks and unblock events)
- **US5 (Phase 7)**: Depends on US1 + US2 (templates include dependencies)
- **US6 (Phase 8)**: Depends on US1 (needs domain events to handle), can run parallel with US2-US5
- **US7 (Phase 9)**: Depends on US1 (needs task data to report on)
- **US8 (Phase 10)**: Depends on US1 (needs tasks for view conditions)
- **Polish (Phase 11)**: Depends on all desired user stories

### User Story Dependencies

```
Phase 1: Setup
    │
Phase 2: Foundational
    │
    ├── US1 (P1) ─── MVP ──────────────────────────────────┐
    │       │                                                │
    │       ├── US2 (P2) ── extends US1 commands             │
    │       │       │                                        │
    │       │       ├── US4 (P4) ── needs unblock events     │
    │       │       │                                        │
    │       │       └── US5 (P5) ── templates with deps      │
    │       │                                                │
    │       ├── US3 (P3) ── can parallel with US2            │
    │       │                                                │
    │       ├── US6 (P6) ── can parallel with US2-US5        │
    │       │                                                │
    │       ├── US7 (P7) ── can parallel with US2-US6        │
    │       │                                                │
    │       └── US8 (P8) ── can parallel with US2-US7        │
    │                                                        │
    └──────────────────── Phase 11: Polish ─────────────────┘
```

### Parallel Opportunities

**After Foundational (Phase 2) completes**:
- US1 must complete first (all other stories depend on it)

**After US1 completes**:
- US2, US3, US6, US7, US8 can all start in parallel
- US4 and US5 must wait for US2

**After US2 completes**:
- US4 and US5 can start (and run parallel with any ongoing US3/US6/US7/US8 work)

### Within Each User Story

- Commands/queries [P] before web layer
- Web partial before page integration
- Handler before view that uses it
- Core implementation before polish

---

## Parallel Example: User Story 1

```text
# After Phase 2 (Foundational) is complete, launch all commands in parallel:
Task T010: "Create CreateTicketTask command"
Task T011: "Create UpdateTicketTask command"
Task T012: "Create CompleteTicketTask command"
Task T013: "Create ReopenTicketTask command"
Task T014: "Create DeleteTicketTask command"
Task T015: "Create ReorderTicketTasks command"

# Then launch query (can parallel with commands if DTO is ready):
Task T016: "Create GetTasksByTicketId query"

# Then web layer sequentially:
Task T017 → T018 → T019 → T020 (partial → handlers → view → JS)

# Then closure gate:
Task T021 → T022 → T023
```

---

## Parallel Example: After US1 Completes

```text
# These can all start simultaneously with different developers:
Developer A: US2 (Phase 4) — dependencies and blocking
Developer B: US3 (Phase 5) — staff tasks page
Developer C: US6 (Phase 8) — activity logging
Developer D: US7 (Phase 9) — reports
Developer E: US8 (Phase 10) — ticket views
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T009)
3. Complete Phase 3: User Story 1 (T010-T023)
4. **STOP and VALIDATE**: Test inline task CRUD and closure gate end-to-end
5. Deploy/demo if ready — this delivers core task management value

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → **MVP!** Inline task management on tickets
3. US2 → Dependencies make tasks a workflow tool
4. US3 → Staff can manage tasks across all tickets
5. US4 → People get notified about task assignments/completions
6. US5 → Templates eliminate repetitive task creation
7. US6 → Full audit trail for compliance
8. US7 → Management visibility via reports
9. US8 → Tasks integrated into ticket views

### Parallel Team Strategy

1. All developers complete Setup + Foundational together
2. One developer completes US1 (MVP — blocks others)
3. After US1:
   - Dev A: US2 (dependencies)
   - Dev B: US3 (tasks page) + US7 (reports) + US8 (views)
   - Dev C: US6 (activity logging)
4. After US2: Dev A continues to US4 (notifications) + US5 (templates)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently testable at its checkpoint
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Task IDs are sequential within this file — dependencies reference earlier IDs
- Total tasks: 73 across 11 phases
