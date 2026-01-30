# Feature Specification: Ticket Snooze

**Feature Branch**: `006-ticket-snooze`  
**Created**: 2026-01-30  
**Status**: Draft  
**Input**: User description: "Introduce the concept of snoozing a ticket - a maximally useful and intuitive Snooze feature following best practices for how a Snooze feature would be expected to work in a system like this."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Snooze Ticket Until Specific Time (Priority: P1)

As a support staff member, I want to snooze a ticket until a specific date and time so that it temporarily disappears from my active queue and automatically resurfaces when I need to follow up.

**Why this priority**: This is the core functionality of the snooze feature. Without the ability to snooze tickets with a scheduled return, the feature has no value. This enables staff to manage their workload effectively by deferring tickets that cannot be actioned immediately.

**Independent Test**: Can be fully tested by snoozing a single ticket with a future date, verifying it's hidden from the active queue, then confirming it reappears at the scheduled time and delivers immediate workload management value.

**Acceptance Scenarios**:

1. **Given** an open ticket assigned to me, **When** I click the snooze button and select "Tomorrow at 9am", **Then** the ticket is marked as snoozed, removed from my default inbox view, and scheduled to reappear tomorrow at 9am.

2. **Given** a snoozed ticket with a snooze end time of today at 2pm, **When** the system clock reaches 2pm, **Then** the ticket is automatically unsnoozed and appears in my active ticket queue with a visual indicator that it just unsnoozed.

3. **Given** a snoozed ticket, **When** I navigate to the "Snoozed" tickets view, **Then** I can see all my snoozed tickets with their scheduled unsnooze times clearly displayed.

4. **Given** I am snoozing a ticket, **When** I select "Custom date/time", **Then** I can pick any future date and time (respecting organization timezone) for the ticket to unsnooze.

5. **Given** a ticket that is not assigned to a specific individual (unassigned or team-only), **When** I attempt to snooze it, **Then** the system prevents the snooze and prompts me to assign it to an individual first.

---

### User Story 2 - Quick Snooze Presets (Priority: P2)

As a support staff member, I want to quickly snooze tickets using common time presets so that I can efficiently defer tickets without manually selecting dates each time.

**Why this priority**: While snoozing to a custom time works, most snooze actions follow predictable patterns. Presets dramatically speed up the workflow for the most common use cases.

**Independent Test**: Can be tested by snoozing a ticket using each preset option and verifying the correct unsnooze time is calculated.

**Acceptance Scenarios**:

1. **Given** an open ticket assigned to me, **When** I click snooze and select "Later Today", **Then** the ticket is snoozed until 3 hours from now (or 9am tomorrow if less than 3 hours remain in business hours).

2. **Given** an open ticket assigned to me, **When** I click snooze and select "Tomorrow", **Then** the ticket is snoozed until 9am tomorrow (organization's configured business hours start time).

3. **Given** an open ticket assigned to me, **When** I click snooze and select "Next Week", **Then** the ticket is snoozed until 9am on the following Monday (or first business day).

4. **Given** an open ticket assigned to me, **When** I click snooze and select "In 3 days", **Then** the ticket is snoozed until 9am three days from now.

---

### User Story 3 - Manual Unsnooze (Priority: P3)

As a support staff member, I want to manually unsnooze a ticket before its scheduled time so that I can immediately work on it if circumstances change.

**Why this priority**: Staff need control over their snoozed tickets. Circumstances change, and manual unsnooze ensures staff aren't locked out of tickets they've snoozed.

**Independent Test**: Can be tested by snoozing a ticket, navigating to the snoozed view, clicking unsnooze, and verifying the ticket returns to the active queue.

**Acceptance Scenarios**:

1. **Given** a snoozed ticket in my snoozed tickets view, **When** I click "Unsnooze", **Then** the ticket immediately returns to the active queue and the snooze is cancelled.

2. **Given** a snoozed ticket, **When** I view the ticket detail page, **Then** I see a prominent "Unsnooze" button and the scheduled unsnooze time.

3. **Given** I am the assignee of a snoozed ticket, **When** I manually unsnooze it, **Then** no notification is sent (I already know I unsnoozed it).

---

### User Story 4 - SLA Pause During Snooze (Priority: P4)

As a support staff member, I want SLA timers to pause while a ticket is snoozed so that legitimate waiting periods don't unfairly count against resolution time.

**Why this priority**: Snoozing often happens when waiting for external factors (information gathering, scheduled follow-ups). SLA should reflect actual working time, not waiting time. This is critical for accurate SLA reporting but depends on core snooze functionality working first.

**Independent Test**: Can be tested by snoozing a ticket with an active SLA, letting time pass, unsnoozing, and verifying the SLA due date has been extended by the snoozed duration.

**Acceptance Scenarios**:

1. **Given** a ticket with an SLA due in 4 hours, **When** I snooze the ticket for 2 hours, **Then** when it unsnoozes, the SLA due time is extended by 2 hours (now due in 4 hours from unsnooze time).

2. **Given** a snoozed ticket, **When** I view SLA information, **Then** I see the SLA is "Paused" rather than actively counting down.

3. **Given** organization settings, **When** an admin configures snooze behavior, **Then** they can choose whether SLA pauses during snooze (default: yes) or continues running.

---

### User Story 5 - Snooze Notifications (Priority: P4)

As a support staff member, I want to receive appropriate notifications about snooze activity so that I stay informed without being overwhelmed by self-caused notifications.

**Why this priority**: Notifications ensure snoozed tickets don't fall through the cracks while avoiding notification noise for actions I initiated myself.

**Independent Test**: Can be tested by triggering various snooze/unsnooze scenarios and verifying correct notification behavior for assignees and followers.

**Acceptance Scenarios**:

1. **Given** a snoozed ticket that reaches its scheduled unsnooze time, **When** the ticket auto-unsnoozes, **Then** the assignee receives an in-app and email notification with the ticket title and a link.

2. **Given** I am following a snoozed ticket, **When** the ticket auto-unsnoozes, **Then** I receive a notification indicating the ticket has unsnoozed.

3. **Given** I am the assignee of a snoozed ticket, **When** someone else manually unsnoozes my ticket, **Then** I receive a notification that the ticket was unsnoozed (by whom).

4. **Given** I am following a snoozed ticket, **When** someone else manually unsnoozes it, **Then** I receive a notification. **But** if I manually unsnooze it myself, **Then** I do not receive a notification.

5. **Given** my notification preferences, **When** I configure email notifications, **Then** I can enable or disable email notifications for unsnoozed tickets.

6. **Given** a snoozed ticket that is closed, **When** the ticket is closed, **Then** no snooze-related notification is sent (though ticket closed notifications may still be sent).

7. **Given** a system migration, **When** the snooze feature is deployed, **Then** all users default to having both email and in-app notifications enabled for ticket unsnooze events.

---

### User Story 6 - Snooze Assignment Constraints (Priority: P3)

As a system, I must enforce that snoozed tickets always have an individual assignee to ensure someone is accountable for the ticket when it unsnoozes.

**Why this priority**: Without this constraint, snoozed tickets could fall through the cracks with no one responsible. This is a foundational integrity rule.

**Independent Test**: Can be tested by attempting to unassign or team-assign a snoozed ticket and verifying it triggers automatic unsnooze.

**Acceptance Scenarios**:

1. **Given** a snoozed ticket assigned to me, **When** I try to unassign it completely, **Then** the ticket is automatically unsnoozed first, then unassigned.

2. **Given** a snoozed ticket assigned to me, **When** I try to reassign it to a team without specifying an individual assignee, **Then** the ticket is automatically unsnoozed first, then reassigned to the team.

3. **Given** a snoozed ticket assigned to me, **When** I reassign it to a different individual person, **Then** the snooze remains active and the new assignee inherits the snoozed ticket.

4. **Given** a ticket that is not assigned to any individual (unassigned or team-only), **When** I attempt to snooze it, **Then** the snooze action is blocked with a message indicating an individual assignee is required.

---

### User Story 7 - Snoozed Ticket Filtering in Views (Priority: P3)

As a support staff member, I want to control whether I see snoozed tickets in my views so that I can focus on actionable tickets while still being able to find snoozed ones when needed.

**Why this priority**: This directly impacts daily workflow. Staff need clean views for focus, but also need access to snoozed tickets for review.

**Independent Test**: Can be tested by toggling the snooze filter in various views and verifying correct ticket visibility.

**Acceptance Scenarios**:

1. **Given** I am viewing "My Tickets" or other built-in views (except "All Tickets"), **When** the view loads, **Then** snoozed tickets are hidden by default.

2. **Given** I am viewing a built-in view with snoozed tickets hidden, **When** I check the "Show snoozed" checkbox filter, **Then** snoozed tickets appear in the list with a visual snooze indicator.

3. **Given** I am viewing the "All Tickets" built-in view, **When** the view loads, **Then** all tickets including snoozed tickets are shown by default.

4. **Given** I am viewing "All Tickets", **When** I use the snooze filter dropdown, **Then** I can filter to show "All", "Only snoozed", or "Exclude snoozed" tickets.

5. **Given** the dedicated "Snoozed" view, **When** I access it, **Then** only snoozed tickets are displayed with their unsnooze times prominently shown.

---

### User Story 8 - Is Snoozed View Condition (Priority: P4)

As an admin configuring system views, or as staff creating custom views, I want to use "Is Snoozed" as a filter condition so that I can create views that include or exclude snoozed tickets.

**Why this priority**: Extends the existing view condition pattern for consistency and flexibility. Depends on core snooze being implemented.

**Independent Test**: Can be tested by creating a custom view with "Is Snoozed = Yes" condition and verifying only snoozed tickets appear.

**Acceptance Scenarios**:

1. **Given** I am an admin configuring system views, **When** I add a condition, **Then** "Is Snoozed" appears as an option alongside "Has Attachments", "Has Contact", etc.

2. **Given** I am staff creating a custom view, **When** I add a filter condition, **Then** "Is Snoozed" appears as an option with Yes/No values.

3. **Given** a custom view with condition "Is Snoozed = Yes", **When** I view the results, **Then** only snoozed tickets matching other criteria are displayed.

4. **Given** a custom view with condition "Is Snoozed = No", **When** I view the results, **Then** only non-snoozed tickets matching other criteria are displayed.

---

### Edge Cases

- **What happens when snooze time is in the past?** System rejects the snooze and prompts for a future time.
- **What happens if a snoozed ticket is reassigned to another individual?** The snooze remains active; new assignee sees it in their snoozed view and receives unsnooze notifications.
- **What happens if a snoozed ticket is reassigned to a team (no individual) or unassigned?** The ticket is automatically unsnoozed before the reassignment completes.
- **What happens if ticket is closed while snoozed?** Snooze is automatically cancelled. No snooze notification is sent, though ticket closed notifications may still fire.
- **What if business hours change after snoozing?** Unsnooze time remains fixed to the original calculated time.
- **What happens to snooze if ticket status changes to resolved/closed?** Snooze is cancelled automatically.
- **Can multiple users snooze the same ticket differently?** No. A ticket has one snooze state. Snoozing updates the existing snooze (last snooze wins).
- **What if the unsnooze background job is delayed?** Ticket unsnoozes on next job run; slight delays are acceptable.
- **Maximum snooze duration?** Default 90 days, configurable via environment variables.
- **What about tickets with no assignee?** Cannot be snoozed. Snooze requires an individual assignee.
- **Can I snooze a ticket assigned only to a team?** No. Must be assigned to a specific individual.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow staff to snooze an open or in-progress ticket until a specified future date and time.
- **FR-002**: System MUST provide quick snooze presets: "Later Today", "Tomorrow", "In 3 Days", "Next Week", and "Custom".
- **FR-003**: System MUST hide snoozed tickets from built-in views by default (except "All Tickets" view).
- **FR-004**: System MUST provide a "Show snoozed" checkbox filter on built-in views to toggle visibility of snoozed tickets.
- **FR-005**: System MUST show all tickets (including snoozed) in the "All Tickets" view with a filter dropdown to show All/Only snoozed/Exclude snoozed.
- **FR-006**: System MUST automatically unsnooze tickets when their scheduled unsnooze time is reached.
- **FR-007**: System MUST provide a dedicated "Snoozed" view showing all snoozed tickets with their unsnooze times.
- **FR-008**: System MUST allow manual unsnooze of any snoozed ticket at any time.
- **FR-009**: System MUST record all snooze and unsnooze actions in the ticket's changelog with timestamps, actor, and reason.
- **FR-010**: System MUST display a visual indicator on snoozed tickets showing the scheduled unsnooze time.
- **FR-011**: System MUST pause SLA timers while a ticket is snoozed (organizationally configurable).
- **FR-012**: System MUST notify the assignee when a ticket auto-unsnoozes (both in-app and email, per preferences).
- **FR-013**: System MUST notify ticket followers when a ticket auto-unsnoozes.
- **FR-014**: System MUST notify the assignee when someone else manually unsnoozes their ticket.
- **FR-015**: System MUST notify ticket followers when someone else manually unsnoozes a ticket (but not if they unsnoozed it themselves).
- **FR-016**: System MUST NOT send any notification to a user who performed the manual unsnooze action themselves.
- **FR-017**: System MUST NOT send snooze-related notifications when a snoozed ticket is closed.
- **FR-018**: System MUST cancel active snooze when a ticket is closed or resolved.
- **FR-019**: System MUST calculate snooze preset times based on organization timezone and business hours configuration.
- **FR-020**: System MUST enforce maximum snooze duration of 90 days by default, configurable via environment variable.
- **FR-021**: System MUST display recently unsnoozed tickets with a temporary visual indicator for the first 30 minutes after unsnooze.
- **FR-022**: System MUST prevent snoozing a ticket that is not assigned to a specific individual.
- **FR-023**: System MUST automatically unsnooze a ticket when it is unassigned or reassigned to a team without an individual assignee.
- **FR-024**: System MUST allow reassigning a snoozed ticket to a different individual while maintaining the snooze.
- **FR-025**: System MUST provide an "Is Snoozed" condition for system views (admin configuration).
- **FR-026**: System MUST provide an "Is Snoozed" condition for custom views (staff configuration).
- **FR-027**: The "Is Snoozed" condition MUST follow the same pattern as existing conditions like "Has Attachments" and "Has Contact".
- **FR-028**: On migration, all users MUST default to having email and in-app notifications enabled for ticket unsnooze events.

### Key Entities

- **TicketSnooze**: Represents a snooze action on a ticket. Tracks when snooze starts, when it should end, who initiated it, optional snooze reason/note, and whether the snooze is currently active.
- **Ticket (extended)**: Gains a reference to its active snooze state, enabling efficient filtering and display of snooze status.
- **SnoozeSettings (organization-level)**: Configuration for snooze behavior including whether SLA pauses during snooze.
- **Environment Configuration**: Maximum snooze duration setting (default: 90 days).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff can snooze and schedule ticket return in under 5 seconds using presets.
- **SC-002**: 100% of snoozed tickets automatically reappear within 5 minutes of their scheduled unsnooze time.
- **SC-003**: Staff can view all their snoozed tickets and remaining snooze times on a single dedicated page.
- **SC-004**: Snoozed tickets are hidden from default views, reducing visible queue size for improved focus.
- **SC-005**: Attempting to snooze an unassigned ticket results in immediate feedback explaining an assignee is required.
- **SC-006**: Unassigning or team-assigning a snoozed ticket triggers automatic unsnooze before the assignment change completes.
- **SC-007**: SLA metrics accurately reflect paused time during snooze periods (when configured).
- **SC-008**: All snooze/unsnooze actions are fully auditable via ticket changelog.
- **SC-009**: Users performing manual unsnooze on their own tickets receive zero notifications from that action.
- **SC-010**: Custom views with "Is Snoozed" condition correctly filter tickets based on snooze state.

## Assumptions

1. **Business hours configuration already exists**: The system already has `BusinessHoursConfigJson` on SLA rules that can be leveraged for preset calculations. If no organization-level business hours exist, sensible defaults (9am-5pm, Mon-Fri) will be used.

2. **Background job infrastructure exists**: The system already has `SlaEvaluationJob` running every 5 minutes. The snooze evaluation can piggyback on this infrastructure or use a similar pattern.

3. **Notification system is in place**: The existing `IInAppNotificationService` and email notification infrastructure will be extended to support snooze-related notifications.

4. **Changelog infrastructure exists**: Ticket already has `ChangeLogEntries` collection that will be used to record snooze/unsnooze events.

5. **Permission model**: Users who can edit a ticket can snooze it. No additional permissions are required for basic snooze functionality.

6. **Single snooze per ticket**: A ticket can only have one active snooze at a time. Snoozing an already-snoozed ticket updates the existing snooze.

7. **View condition infrastructure exists**: The system has existing view conditions like "Has Attachments" and "Has Contact" that provide a pattern for implementing "Is Snoozed".

8. **Environment variable configuration**: The system supports reading configuration from environment variables for settings like maximum snooze duration.

9. **Internal staff only**: All users are internal staff. There is no concept of external customer/contact replies or communication through the system.
