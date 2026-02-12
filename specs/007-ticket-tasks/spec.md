# Feature Specification: Ticket Tasks

**Feature Branch**: `007-ticket-tasks`  
**Created**: 2026-02-11  
**Status**: Draft  
**Input**: User description: "Ticket Tasks Feature — Introduce a Tasks feature to the existing Ticket Management System. Tasks represent discrete pieces of work that must be completed to resolve a Ticket."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Managing Tasks on a Ticket (Priority: P1)

A staff member opens a ticket and needs to break it into discrete pieces of work. From the Ticket Detail View, they add tasks inline by typing a title, then optionally set an assignee, due date, and dependency. They reorder tasks via drag-and-drop to reflect priority. They mark tasks as closed when complete and can reopen them if needed. A user with ticket-delete permission can also delete tasks.

**Why this priority**: This is the foundational capability. Without inline task management on tickets, no other feature (dependencies, templates, views, reports) has value. This delivers immediate, standalone value for breaking work into trackable pieces.

**Independent Test**: Can be fully tested by creating a ticket, adding several tasks inline, editing their properties, reordering them, completing them, and reopening one — all without leaving the Ticket Detail View.

**Acceptance Scenarios**:

1. **Given** a staff user is viewing a Ticket Detail page, **When** they click the add-task control and type a title, **Then** a new task is created with status Open, no assignee, no due date, and no dependency.
2. **Given** a task exists on a ticket, **When** a staff user edits the task title inline, **Then** the title is updated immediately without a page reload or modal.
3. **Given** a task exists on a ticket, **When** a staff user sets an assignee (user or team), **Then** the assignee is saved and displayed, and if a user is assigned, the team is inferred automatically.
4. **Given** a task exists on a ticket, **When** a staff user sets a due date using the same date/time picker and timezone handling as the Snooze feature, **Then** the due date is saved and displayed.
5. **Given** multiple tasks exist on a ticket, **When** a staff user drags a task to a new position, **Then** the sort order is updated and persists.
6. **Given** an Open task exists, **When** a staff user marks it as Closed, **Then** the task status changes to Closed and is visually indicated as complete.
7. **Given** a Closed task exists, **When** a staff user reopens it, **Then** the task status changes back to Open.
8. **Given** a staff user has ticket-delete permission, **When** they delete a task, **Then** the task is removed from the ticket.
9. **Given** a staff user does not have ticket-delete permission, **When** they view a task, **Then** no delete option is available.
10. **Given** a staff user is on the Ticket Edit screen, **When** the page renders, **Then** no Tasks section is displayed (tasks only appear on Ticket Detail View).
11. **Given** a ticket has incomplete tasks (Open or Blocked), **When** a staff user attempts to change the ticket status to one with Status Type = Closed, **Then** the system displays an inline confirmation message such as: "There are tasks on this ticket that are not complete. Would you like to mark all tasks as complete and close the ticket?"
12. **Given** the ticket-closure confirmation is displayed and the user approves, **When** the action is confirmed, **Then** all incomplete tasks (including Blocked tasks) are marked as Closed and the ticket status is changed to the selected closed status.
13. **Given** the ticket-closure confirmation is displayed and the user declines, **When** the action is cancelled, **Then** no tasks are modified and the ticket status remains unchanged.

---

### User Story 2 - Task Dependencies and Blocking (Priority: P2)

A staff member sets up a task that depends on another task being completed first. The dependent task is created immediately and can be pre-assigned with a due date, but it is visually marked as Blocked and is not actionable. When the prerequisite task is closed, the blocked task automatically transitions to Open, becomes actionable, and assignment notifications are sent at that moment.

**Why this priority**: Dependencies are the core differentiator of this feature — they enable structured workflows across teams and make blocked work visible. Without this, tasks are just a flat checklist.

**Independent Test**: Can be tested by creating two tasks, setting a dependency from Task B to Task A, verifying Task B appears blocked, closing Task A, and confirming Task B transitions to Open with notifications sent.

**Acceptance Scenarios**:

1. **Given** two tasks exist on a ticket, **When** a staff user sets Task B to depend on Task A, **Then** Task B is marked as Blocked.
2. **Given** a task is Blocked, **When** it is displayed in the task list, **Then** it appears visually muted/greyed with a lock or dependency icon and inline text reading "Blocked by: [Task A title]".
3. **Given** a task is Blocked, **When** a staff user attempts to mark it as Closed, **Then** the system prevents the action (the close control is disabled or hidden).
4. **Given** a Blocked task is assigned to a user, **When** the task is created or the dependency is first set, **Then** no assignment notification is sent.
5. **Given** Task B is Blocked by Task A, **When** Task A is marked as Closed, **Then** Task B automatically transitions from Blocked to Open.
6. **Given** a task transitions from Blocked to Open and has an assignee, **When** the transition occurs, **Then** the appropriate assignment notification is sent at that moment.
7. **Given** a task has a dependency, **When** a staff user removes the dependency, **Then** the task immediately becomes Open (if it was Blocked).
8. **Given** a task has a dependency on Task A, **When** Task A is reopened after being Closed, **Then** the dependent task returns to Blocked status.
9. **Given** a task is set to depend on another task, **When** a staff user attempts to create a circular dependency, **Then** the system prevents the circular dependency and informs the user.

---

### User Story 3 - Staff Tasks Page with Built-in Views (Priority: P3)

A staff member navigates to the new "Tasks" section in the left navigation to see all tasks relevant to them across all tickets. The page provides built-in views (My Tasks, Team Tasks, Unassigned, Created by Me, Overdue, All Tasks), each with sensible default sorting, search, and filters. Staff can change assignees, due dates, and mark tasks Open/Closed directly from this page.

**Why this priority**: A centralized task view lets staff manage their workload without navigating ticket-by-ticket. This is critical for daily workflow and team coordination, but depends on tasks existing (P1) to have value.

**Independent Test**: Can be tested by creating tasks across multiple tickets with various assignees, then navigating to the Tasks page and verifying each built-in view filters correctly, inline actions work, and search/sort behaves as expected.

**Acceptance Scenarios**:

1. **Given** a staff user clicks "Tasks" in the left navigation, **When** the page loads, **Then** the default view shows Open (incomplete) tasks only.
2. **Given** the Tasks page is displayed, **When** a staff user selects "My Tasks", **Then** only tasks assigned to the logged-in user are shown.
3. **Given** the Tasks page is displayed, **When** a staff user selects "Team Tasks", **Then** only tasks assigned to teams the logged-in user belongs to are shown.
4. **Given** the Tasks page is displayed, **When** a staff user selects "Unassigned", **Then** only tasks with no assignee are shown.
5. **Given** the Tasks page is displayed, **When** a staff user selects "Created by Me", **Then** only tasks created by the logged-in user are shown.
6. **Given** the Tasks page is displayed, **When** a staff user selects "Overdue", **Then** only Open tasks past their due date are shown.
7. **Given** the Tasks page is displayed, **When** a staff user selects "All Tasks", **Then** all tasks (Open and Closed) are shown.
8. **Given** a task is displayed on the Tasks page, **When** a staff user changes the assignee inline, **Then** the assignee is updated without navigating away.
9. **Given** a task is displayed on the Tasks page, **When** a staff user changes the due date inline, **Then** the due date is updated without navigating away.
10. **Given** a task is displayed on the Tasks page, **When** a staff user marks the task Closed, **Then** the task status updates inline and the task may be removed from the current view if it no longer matches the filter.
11. **Given** a built-in view has no matching tasks, **When** the view loads, **Then** a clear, friendly empty state message is displayed.
12. **Given** a staff user types in the search box on the Tasks page, **When** results are returned, **Then** tasks are filtered by title match across the current view.
13. **Given** a task is displayed on the Tasks page, **When** the staff user views the task, **Then** the parent ticket number, title (as a clickable link), and parent ticket assignee are visible alongside the task.

---

### User Story 4 - Task Notifications (Priority: P4)

When tasks are assigned or completed, the system sends notifications through the existing notification framework. Assignment notifications are sent only when a task becomes Open (not while Blocked). Task completion notifications go to the task assignee and ticket followers.

**Why this priority**: Notifications keep the right people informed and drive task completion. They are essential for cross-team workflows but depend on the core task system (P1) and dependency logic (P2) being in place.

**Independent Test**: Can be tested by assigning a task, verifying the notification is sent, then creating a blocked task with an assignee, verifying no notification is sent until the dependency is resolved.

**Acceptance Scenarios**:

1. **Given** a task is assigned to a user and the task is Open, **When** the assignment is saved, **Then** a "Task Assigned to User" notification is sent to the assigned user and ticket followers.
2. **Given** a task is assigned to a team and the task is Open, **When** the assignment is saved, **Then** a "Task Assigned to Team" notification is sent to all members of the team and ticket followers.
3. **Given** a task is Blocked and assigned to a user, **When** the dependency is resolved and the task transitions to Open, **Then** the "Task Assigned to User" notification is sent at that moment.
4. **Given** a task is Blocked and assigned to a user, **When** the task is still Blocked, **Then** no assignment notification is sent.
5. **Given** an Open task is marked as Closed, **When** the status change is saved, **Then** a "Task Completed" notification is sent to the task assignee and ticket followers.
6. **Given** a new installation or migration, **When** the system is updated, **Then** email templates for all task notifications exist and notifications are enabled by default for all users.

---

### User Story 5 - Task Templates (Priority: P5)

An administrator creates reusable task templates from the Admin Portal. Each template defines a set of task items (titles only), their order, and dependency relationships. Staff members can apply a template to a ticket, which inserts the full task list preserving order and dependencies, with all tasks defaulting to no due date and unassigned.

**Why this priority**: Templates eliminate repetitive task creation for common workflows. They require the core task system (P1) and dependencies (P2) to function, and are an efficiency multiplier rather than a core capability.

**Independent Test**: Can be tested by creating a template with ordered tasks and dependencies in Admin, then applying it to a ticket in the Staff Portal and verifying all tasks, order, and dependencies are correctly created.

**Acceptance Scenarios**:

1. **Given** an admin user navigates to the new "Task Templates" item in the Admin left navigation, **When** the page loads, **Then** they see a list of existing task templates and can create a new one.
2. **Given** an admin is creating a task template, **When** they define task items, **Then** each item requires only a title.
3. **Given** an admin is creating a task template, **When** they set dependency relationships between template tasks, **Then** the dependencies are saved as part of the template.
4. **Given** an admin is creating a task template, **When** they arrange tasks in a specific order, **Then** the order is saved as part of the template.
5. **Given** task templates have been created, **When** the admin views the template permissions, **Then** they match the same permission model as Ticket Priorities and Ticket Statuses.
6. **Given** a staff user is adding tasks to a ticket, **When** they choose to apply a task template, **Then** all tasks from the template are inserted in one operation with the correct order and dependencies.
7. **Given** a task template is applied, **When** the tasks are created, **Then** all tasks default to no due date and unassigned.
8. **Given** a staff user is adding tasks to a ticket, **When** they choose to add a single manual task instead of a template, **Then** the manual add flow works as normal.

---

### User Story 6 - Activity Logging for Tasks (Priority: P6)

All task-related events are recorded in the existing Activity Log system for auditability and staff debugging. Logged events include task creation, assignment/reassignment, due date changes, dependency changes, unblocking, completion, reopening, and deletion.

**Why this priority**: Auditability is a non-negotiable requirement for ticket systems. Activity logging gives staff and managers a full audit trail, but it is an enhancement layer on top of core task operations.

**Independent Test**: Can be tested by performing each task action (create, assign, set due date, add dependency, complete, reopen, delete) and verifying the corresponding activity log entry appears on the ticket's activity timeline.

**Acceptance Scenarios**:

1. **Given** a task is created, **When** the activity log is viewed, **Then** a "Task created" entry appears with the task title and creator.
2. **Given** a task is assigned or reassigned, **When** the activity log is viewed, **Then** a "Task assigned" entry appears showing the previous and new assignee.
3. **Given** a task's due date is changed, **When** the activity log is viewed, **Then** a "Due date changed" entry appears showing the previous and new due date.
4. **Given** a dependency is added to or removed from a task, **When** the activity log is viewed, **Then** a "Dependency added/removed" entry appears identifying the related task.
5. **Given** a task is unblocked (dependency task completed), **When** the activity log is viewed, **Then** a "Task unblocked" entry appears.
6. **Given** a task is completed, **When** the activity log is viewed, **Then** a "Task completed" entry appears.
7. **Given** a task is reopened, **When** the activity log is viewed, **Then** a "Task reopened" entry appears.
8. **Given** a task is deleted, **When** the activity log is viewed, **Then** a "Task deleted" entry appears with the task title.

---

### User Story 7 - Task Reports (Priority: P7)

A staff member or manager navigates to a new "Tasks Reports" section to see aggregate metrics about task activity. The reports follow the same patterns as existing Ticket Reports: date range filters, summary statistics, and visual charts. Metrics include tasks created, completed, reopened, open vs. closed counts, and overdue tasks.

**Why this priority**: Reports provide visibility into team productivity and bottlenecks. They are valuable for management but depend on a sufficient volume of task data from day-to-day usage.

**Independent Test**: Can be tested by creating, completing, and reopening tasks over a date range, then navigating to Task Reports and verifying all metrics, filters, and charts display correctly.

**Acceptance Scenarios**:

1. **Given** a staff user navigates to the Tasks Reports section, **When** the page loads, **Then** summary statistics are displayed for the default date range.
2. **Given** the Tasks Reports page is displayed, **When** a staff user adjusts the date range filter, **Then** all metrics and charts update to reflect the selected range.
3. **Given** tasks have been created in the system, **When** the "Tasks Created" metric is displayed, **Then** it accurately counts tasks created within the date range.
4. **Given** tasks have been completed, **When** the "Tasks Completed" metric is displayed, **Then** it accurately counts tasks completed within the date range.
5. **Given** tasks have been reopened, **When** the "Tasks Reopened" metric is displayed, **Then** it accurately counts tasks reopened within the date range.
6. **Given** a mix of Open and Closed tasks exist, **When** the "Open vs Closed" chart is displayed, **Then** it accurately represents the current distribution.
7. **Given** tasks with past due dates exist, **When** the "Overdue Tasks" metric is displayed, **Then** it counts only Open tasks with due dates in the past.

---

### User Story 8 - Ticket Views and Task Column (Priority: P8)

Administrators can add new task-related conditions to the Ticket View condition builder: "Has Any Tasks" and "Has Incomplete Tasks." Additionally, a selectable "Tasks" column is available in ticket lists, displaying a compact completion indicator (e.g., "2 / 5") with color-coded progress that fits cleanly within the existing table UI.

**Why this priority**: This integrates tasks into the existing ticket management workflows (views and lists). It's an enhancement layer that depends on tasks existing but adds significant discoverability and filtering power.

**Independent Test**: Can be tested by creating Ticket Views with task conditions and verifying tickets are filtered correctly, and by enabling the Tasks column in a ticket list and verifying the progress indicator displays accurately.

**Acceptance Scenarios**:

1. **Given** an admin is editing a Ticket View, **When** they add the "Has Any Tasks" condition, **Then** the view filters to only tickets that have at least one task.
2. **Given** an admin is editing a Ticket View, **When** they add the "Has Incomplete Tasks" condition, **Then** the view filters to only tickets that have at least one Open task.
3. **Given** a ticket list is configured with the "Tasks" column, **When** the list displays, **Then** each ticket row shows a compact completion indicator (e.g., "2 / 5").
4. **Given** the Tasks column is displayed, **When** a ticket has tasks in various states of completion, **Then** the indicator is color-coded to reflect the completion state (e.g., all complete, partially complete, none complete).
5. **Given** a ticket has no tasks, **When** the Tasks column is displayed, **Then** the cell is empty or shows a dash (no misleading data).

---

### Edge Cases

- What happens when a task's dependency is deleted? The dependent task should become Open (unblocked), since its prerequisite no longer exists.
- What happens when a user attempts to close a task that other tasks depend on, but the dependent tasks are already closed? The close succeeds normally with no side effects.
- What happens when a task template is applied to a ticket that already has tasks? The template tasks are appended after existing tasks, preserving existing task order and independently maintaining template dependencies.
- What happens when the only task on a ticket is deleted? The Tasks section remains visible but shows an empty state inviting the user to add a task.
- What happens when a staff user tries to set a dependency on a Closed task? The dependency is set, but since the prerequisite is already Closed, the task remains Open (not Blocked).
- What happens when a task assignee is deactivated or removed from the system? The task retains the assignee reference for historical purposes but is effectively unassigned for workflow purposes, following existing ticket assignee patterns.
- What happens when a task's due date is in the past at creation time? The system allows it (no validation block) — the task simply appears as overdue immediately.
- What happens when a template with dependencies is applied but one task from the template fails to create? The entire template application should be atomic — all tasks are created or none are.
- What happens when a ticket is closed via the closure confirmation and tasks include Blocked tasks? All tasks (including Blocked) are force-closed regardless of dependency state, since the ticket itself is being resolved. Dependencies become moot.
- What happens when tasks are bulk-closed via the ticket-closure confirmation — do notifications fire for each task? Yes, each task generates a "Task Completed" activity log entry. However, notification behavior for bulk-closed tasks should follow the same rules as individual completion (notify assignee and ticket followers per task).

## Requirements *(mandatory)*

### Functional Requirements

**Task Core**

- **FR-001**: System MUST allow staff users to create tasks on a ticket with a required title field.
- **FR-002**: System MUST support exactly two task statuses: Open and Closed.
- **FR-003**: System MUST derive the Blocked state from an unresolved dependency, not as a stored status.
- **FR-004**: System MUST allow tasks to have an optional single dependency on another task within the same ticket.
- **FR-005**: System MUST create dependent tasks immediately in the database, even while Blocked.
- **FR-006**: System MUST automatically transition a task from Blocked to Open when its dependency task is Closed.
- **FR-007**: System MUST automatically transition a task back to Blocked if its dependency task is reopened.
- **FR-008**: System MUST prevent circular dependencies between tasks.
- **FR-009**: System MUST support optional task assignee: unassigned, assigned to a team, or assigned to a user (team inferred automatically), following existing assignment patterns.
- **FR-010**: System MUST support optional absolute due date and time for tasks, using the same UI and timezone handling as the existing Snooze feature.
- **FR-011**: System MUST support explicit task ordering with drag-and-drop reordering.
- **FR-012**: System MUST persist task sort order across sessions.

**Permissions**

- **FR-013**: Any regular staff user MUST be able to create, edit, assign, set due dates on, and complete/reopen tasks.
- **FR-014**: Task deletion permission MUST match ticket deletion permission — only users who can delete tickets can delete tasks.

**Ticket Status and Task Completion Gate**

- **FR-046**: When a user changes a ticket's status to one with Status Type = Closed and the ticket has any incomplete tasks (Open or Blocked), the system MUST display an inline confirmation message informing the user that incomplete tasks exist and offering to mark all tasks as complete and close the ticket.
- **FR-047**: If the user confirms the ticket-closure prompt, the system MUST mark all incomplete tasks (including Blocked tasks) as Closed and then change the ticket status to the selected closed status.
- **FR-048**: If the user declines the ticket-closure prompt, the system MUST leave all tasks and the ticket status unchanged.
- **FR-049**: Each task force-closed via the ticket-closure confirmation MUST generate an individual "Task completed" activity log entry for audit trail completeness.

**Ticket Detail View (Staff Portal)**

- **FR-015**: System MUST display a Tasks section on the Ticket Detail View.
- **FR-016**: System MUST NOT display tasks on the Ticket Edit screen.
- **FR-017**: All task interactions (add, edit, reorder, complete, reopen, delete) MUST be inline — no modals or popups.
- **FR-018**: Blocked tasks MUST be visually distinct: muted/greyed appearance, lock or dependency icon, and inline text indicating "Blocked by: [Task Title]".
- **FR-019**: Blocked tasks MUST NOT be actionable (close control disabled or hidden).

**Task Templates (Admin Portal)**

- **FR-020**: System MUST provide a "Task Templates" item in the Admin left navigation.
- **FR-021**: Task template permissions MUST match the permission model of Ticket Priorities and Ticket Statuses.
- **FR-022**: A task template MUST consist of task items (title only), dependency relationships, and explicit task order.
- **FR-023**: Task templates MUST NOT include due dates or assignees.
- **FR-024**: Staff MUST be able to apply a task template to a ticket, inserting all tasks in one atomic operation with preserved order and dependencies.
- **FR-025**: All tasks created from a template MUST default to no due date and unassigned.

**Staff Tasks Page**

- **FR-026**: System MUST provide a "Tasks" item in the Staff left navigation leading to a dedicated Tasks page.
- **FR-027**: The Tasks page MUST be a separate page with its own search, filters, sorting, and UI — not a reuse of the ticket list.
- **FR-027a**: Each task on the Tasks page MUST display the parent ticket number and title as a clickable link to the ticket, plus the parent ticket's current assignee.
- **FR-028**: The Tasks page MUST default to showing Open (incomplete) tasks only.
- **FR-029**: Staff MUST be able to change assignee, change due date, and toggle task Open/Closed inline from the Tasks page.
- **FR-030**: System MUST provide built-in views only (no custom staff views): Unassigned, My Tasks, Created by Me, Team Tasks, Overdue, and All Tasks.
- **FR-031**: Each built-in view MUST have sensible default sorting, fast searching, and clear empty states.

**Notifications**

- **FR-032**: System MUST send a "Task Assigned to User" notification to the assigned user and ticket followers when a task is assigned and the task is Open.
- **FR-033**: System MUST send a "Task Assigned to Team" notification to all team members and ticket followers when a task is assigned to a team and the task is Open.
- **FR-034**: System MUST send a "Task Completed" notification to the task assignee and ticket followers when a task is closed.
- **FR-035**: System MUST NOT send any notifications for tasks that are currently Blocked.
- **FR-036**: System MUST send assignment notifications when a Blocked task transitions to Open.
- **FR-037**: Database migrations MUST add email templates for all new task notification types.
- **FR-038**: Task notifications MUST be enabled by default for all users.

**Ticket Views & System Views**

- **FR-039**: System MUST add a "Has Any Tasks" condition to the Ticket View condition builder, filtering to tickets with at least one task.
- **FR-040**: System MUST add a "Has Incomplete Tasks" condition to the Ticket View condition builder, filtering to tickets with at least one Open task.
- **FR-041**: System MUST provide a selectable "Tasks" column in ticket lists showing completed/total count (e.g., "2 / 5") with a compact color-coded progress indicator.

**Task Reports**

- **FR-042**: System MUST provide a Tasks Reports section following existing Ticket Reports patterns (date range filters, summary statistics, visual charts).
- **FR-043**: Task reports MUST include metrics for: tasks created, tasks completed, tasks reopened, open vs. closed counts, and overdue tasks.

**Activity Logging**

- **FR-044**: System MUST log the following task events in the existing Activity Log: task created, task assigned/reassigned, due date changed, dependency added/removed, task unblocked, task completed, task reopened, and task deleted.

**Data & Migrations**

- **FR-045**: Database migrations MUST add schema for tasks, dependencies, sort order, and task activity events.

### Key Entities

- **Task**: A discrete piece of work subordinate to a Ticket. Key attributes: title (the only content field — no description or notes), status (Open/Closed), assignee (user, team, or none), due date/time, dependency (reference to another task), sort order, creator, created/updated timestamps. A task cannot exist without a parent ticket.
- **Task Dependency**: A relationship where one task depends on another task within the same ticket. A task may have at most one dependency. The dependency determines the derived Blocked state.
- **Task Template**: An administrator-defined reusable blueprint for a set of tasks. Contains ordered task items (titles only) with dependency relationships. Does not include assignees or due dates.
- **Task Template Item**: An individual task definition within a template. Key attributes: title, sort order, dependency (reference to another template item).
- **Task Activity Event**: A record of a task-related action in the Activity Log. Types: created, assigned, reassigned, due date changed, dependency added, dependency removed, unblocked, completed, reopened, deleted.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff can create a task on a ticket and mark it complete in under 10 seconds, without navigating away from the Ticket Detail View.
- **SC-002**: Staff can identify all blocked tasks on a ticket within 2 seconds of viewing the Tasks section, without clicking or hovering.
- **SC-003**: Applying a task template to a ticket completes in a single action and all tasks appear within 3 seconds.
- **SC-004**: Staff can view and manage their personal task workload from the Tasks page without visiting individual tickets.
- **SC-005**: 95% of staff users can successfully create, assign, and complete a task on their first attempt without training or documentation.
- **SC-006**: The Ticket Detail page with up to 50 tasks loads and renders within acceptable performance thresholds (no perceptible delay beyond current page load times).
- **SC-007**: The Tasks list page with up to 500 tasks loads and renders within acceptable performance thresholds.
- **SC-008**: All task-related actions produce a corresponding activity log entry with no gaps in the audit trail.
- **SC-009**: Task reports accurately reflect task activity within a selected date range, matching the quality and usability of existing Ticket Reports.
- **SC-010**: Ticket views using task conditions ("Has Any Tasks", "Has Incomplete Tasks") return correct results with no false positives or negatives.

## Clarifications

### Session 2026-02-11

- Q: What happens when a user tries to close a ticket that has incomplete tasks? → A: The system displays an inline confirmation message offering to mark all incomplete tasks (including Blocked) as complete and close the ticket. If the user approves, all tasks are force-closed and the ticket status changes. If declined, nothing changes.
- Q: Should tasks have a description/notes field beyond the title? → A: No. Title only. Staff use descriptive titles or ticket comments for additional context.
- Q: What parent ticket context is shown on the Staff Tasks page? → A: Each task displays the parent ticket number and title as a clickable link, plus the parent ticket's assignee.
- Q: Should the Tasks page support bulk actions (multi-select close, assign, etc.)? → A: No. Bulk actions are out of scope for this release. Staff act on tasks individually. Deferred to a future release.
- Q: Should the system send a notification when a task becomes overdue? → A: No. Overdue tasks are surfaced via the Overdue built-in view and reports. Automatic overdue notifications are deferred to a future release.
- Q: How are concurrent edits to the same task handled? → A: Last write wins. No conflict detection or optimistic concurrency. The activity log captures all changes for audit purposes.

## Assumptions

- The existing Snooze feature's date/time picker and timezone handling is reusable for task due dates without modification.
- The existing notification framework supports adding new notification types without architectural changes.
- The existing Activity Log system supports adding new event types without architectural changes.
- The existing Ticket View condition builder supports adding new condition types without architectural changes.
- The existing assignment patterns (user/team assignment with team inference) are reusable for task assignment.
- The existing ticket list column system supports adding new selectable columns.
- "Ticket deletion permission" is a well-defined, existing permission that can be reused for task deletion gating.
- The Ticket Priorities and Ticket Statuses permission model is well-defined and reusable for Task Template permissions.
- Task dependencies are scoped to tasks within the same ticket (cross-ticket dependencies are out of scope).
- The Tasks page layout is not required to be a table — the most intuitive layout will be chosen during design.
- Custom staff views for the Tasks page are explicitly out of scope for this release.
- Bulk actions (multi-select and batch operations) on the Tasks page are explicitly out of scope for this release.
- Automatic overdue task notifications are explicitly out of scope for this release. Overdue tasks are discoverable via the Overdue view and reports.
- Concurrent task editing follows last-write-wins semantics — no conflict detection or optimistic concurrency. The activity log provides a full audit trail.
