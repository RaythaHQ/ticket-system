# Feature Specification: SLA Extension Controls

**Feature Branch**: `004-sla-extension-controls`  
**Created**: 2025-12-31  
**Status**: Draft  
**Input**: User description: "Custom SLA extension with permission-based restrictions, extension limits, and business-day-aware default calculations"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Extend SLA by Hours (Priority: P1)

A staff member viewing a ticket needs to extend the SLA deadline when they need more time to resolve an issue. Currently they can only "refresh from now" or "refresh from creation time." They need a third option to extend by a specific number of hours.

**Why this priority**: This is the core functionality - without the ability to extend SLA by hours, the rest of the feature has no foundation.

**Independent Test**: Can be fully tested by opening a ticket with an SLA, clicking "Extend SLA," entering hours, and verifying the due date moves forward by that amount.

**Acceptance Scenarios**:

1. **Given** a ticket with an active SLA due in 2 hours, **When** a staff member extends the SLA by 24 hours, **Then** the SLA due date moves forward by 24 hours and a change log entry is created.

2. **Given** a ticket with an active SLA, **When** a staff member opens the extend dialog, **Then** the system pre-calculates and displays the default hours needed to reach 4pm the next business day in the organization's timezone.

3. **Given** the current time is Friday 3pm in the org timezone, **When** the system calculates the default extension hours, **Then** it targets Monday 4pm (skipping Saturday and Sunday).

4. **Given** a staff member enters 0 or negative hours, **When** they attempt to extend, **Then** the system prevents the action and displays a validation error.

5. **Given** a staff member is entering hours in the extend dialog, **When** they type or change the hours value, **Then** the UI shows a live preview of the resulting SLA due date/time.

---

### User Story 2 - Permission-Based Extension Limits (Priority: P2)

Users without "Manage Tickets" permission have restrictions on how many times they can extend an SLA and by how many hours, while those with the permission have unrestricted access.

**Why this priority**: Critical for access control - prevents abuse while allowing flexibility for authorized staff.

**Independent Test**: Can be tested by logging in as a user without Manage Tickets permission, extending an SLA once, then verifying the second attempt is blocked.

**Acceptance Scenarios**:

1. **Given** a user with "Manage Tickets" permission, **When** they extend an SLA, **Then** they can extend any number of times with any number of hours.

2. **Given** a user without "Manage Tickets" permission and a ticket that has been extended 0 times, **When** they extend the SLA, **Then** the extension succeeds and the extension count increases to 1.

3. **Given** a user without "Manage Tickets" permission and a ticket that has reached the maximum extension count (default: 1), **When** they attempt to extend the SLA, **Then** the system prevents the action and displays a message explaining the limit has been reached.

4. **Given** a user without "Manage Tickets" permission, **When** they attempt to extend an SLA by more than the maximum allowed hours (default: 168), **Then** the system prevents the action and shows the maximum allowed.

---

### User Story 3 - Extension Status Display (Priority: P3)

The UI clearly shows users how many times the SLA has been extended, how many extensions remain, and whether they can extend based on their permissions.

**Why this priority**: Essential UX - users need visibility into the extension state before attempting actions.

**Independent Test**: Can be tested by viewing a ticket that has been extended once and verifying the UI shows "1 of 1 extensions used."

**Acceptance Scenarios**:

1. **Given** a ticket with an SLA that has been extended 1 time and a max of 1 extension allowed, **When** a user without Manage Tickets permission views the ticket, **Then** the UI shows "1 of 1 extensions used" and the extend option is disabled with an explanation.

2. **Given** a ticket with an SLA that has been extended 0 times, **When** any user views the ticket, **Then** the UI shows "0 of 1 extensions used" (or configured max) and the extend option is available.

3. **Given** a user with "Manage Tickets" permission viewing a ticket at max extensions, **When** they view the SLA section, **Then** the extend option remains enabled with a note that they have unlimited extension capability.

---

### Edge Cases

- What happens when a ticket has no SLA rule assigned? SLA options (refresh from now, refresh from creation, extend by hours) remain available so an SLA can be applied later to tickets that initially had none. This enables workflows where ticket classification changes and an SLA becomes applicable.
- What happens when the SLA is already breached? Extensions should still be allowed to give a new target date.
- What happens when a ticket is closed/resolved? SLA extension should be disabled (current behavior).
- What happens when the organization timezone is not set? Fall back to UTC for business day calculations.
- What happens when extending on a holiday? Use weekends-only calculation initially; holiday support can be a future enhancement.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an "Extend SLA" option that allows extending the due date by a specified number of hours.

- **FR-002**: System MUST calculate a smart default for extension hours that targets 4pm the next business day in the organization's timezone, accounting for weekends.

- **FR-003**: System MUST prevent extension by zero or negative hours.

- **FR-004**: System MUST track how many times each ticket's SLA has been extended.

- **FR-005**: System MUST enforce a configurable maximum number of SLA extensions for users without "Manage Tickets" permission (default: 1, configurable via environment variable).

- **FR-006**: System MUST enforce a configurable maximum extension hours for users without "Manage Tickets" permission (default: 168 hours / 7 days, configurable via environment variable).

- **FR-007**: System MUST allow users with "Manage Tickets" permission to extend SLA unlimited times and by unlimited hours (using the same hours-based UI as non-privileged users).

- **FR-008**: System MUST display the current extension count and remaining extensions allowed in the ticket UI.

- **FR-009**: System MUST create a change log entry each time an SLA is extended, recording who extended it, by how much, and what the new due date is.

- **FR-010**: System MUST disable SLA extension options for closed/resolved tickets.

- **FR-011**: System MUST prevent extension that would result in a due date in the past.

- **FR-012**: System MUST allow SLA options (refresh from now, refresh from creation, extend by hours) on tickets that have no SLA rule currently assigned, enabling SLA application to tickets that initially had none.

### Key Entities

- **Ticket**: Extended with `SlaExtensionCount` (integer) to track how many times the SLA has been extended for this ticket.

- **Configuration (Environment Variables)**:
  - `SLA_MAX_EXTENSIONS`: Maximum number of times non-privileged users can extend (default: 1)
  - `SLA_MAX_EXTENSION_HOURS`: Maximum hours non-privileged users can extend by (default: 168)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff can extend an SLA by a specified number of hours within 3 clicks from the ticket view.

- **SC-002**: The default extension hours calculation correctly targets the next business day at 4pm, accounting for weekends, 100% of the time.

- **SC-003**: Extension limit enforcement prevents 100% of unauthorized extension attempts by non-privileged users.

- **SC-004**: Users can understand their extension capabilities (count remaining, max hours) within 2 seconds of viewing the SLA section.

- **SC-005**: All SLA extensions are fully auditable through the ticket change log.

- **SC-006**: Privileged users experience zero friction when extending SLAs (no limits enforced).

## Clarifications

### Session 2025-12-31

- Q: Should privileged users have a separate datetime picker UI to set arbitrary SLA dates? → A: No. All users use the same hours-based extension UI. Privileged users simply have no restrictions on hours or extension count. User Story 4 removed for simplicity.
- Q: Should the UI show a live preview of the resulting SLA due date as the user enters hours? → A: Yes, show live preview as user types for immediate feedback.
- Q: What should happen when a ticket has no SLA rule assigned? → A: SLA options (refresh from now, refresh from creation, extend by hours) should still be available so an SLA can be applied to tickets that initially had none.

## Assumptions

- The organization timezone setting already exists in the system and can be retrieved for business day calculations.
- "Manage Tickets" permission already exists and can be checked via the existing permission service.
- The current SLA refresh functionality (refresh from now, refresh from creation) will remain available alongside the new extend option.
- Weekend days are Saturday and Sunday globally; holiday support is out of scope for this feature.
- The extension count is specific to each ticket and resets if the ticket is reopened after being closed.
