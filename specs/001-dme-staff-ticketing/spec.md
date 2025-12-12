# Feature Specification: DME Staff Ticketing System

**Feature Branch**: `001-dme-staff-ticketing`  
**Created**: 2025-12-11  
**Status**: Draft  
**Input**: User description: "Create a detailed functional and technical specification for a back-office ticketing system for a DME (Durable Medical Equipment) company"

---

## Overview

This specification defines an internal staff ticketing system for a Durable Medical Equipment (DME) company's back-office operations. The system enables staff members to create, track, and manage support tickets linked to contacts (patients, providers, insurance representatives, etc.) while enforcing role-based access controls and SLA compliance.

**Key Objectives**:
- Centralize ticket management for all internal staff workflows
- Enable efficient contact management with ticket association
- Provide configurable SLA enforcement with business hours support
- Support team-based assignment with round-robin distribution
- Deliver actionable metrics and dashboards for operational visibility

**URL Structure**:
- `/staff/tickets/` — All ticket management interfaces
- `/staff/contacts/` — All contact management interfaces
- `/admin/` — System configuration (teams, SLAs, views, notifications)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Base Staff Creates and Comments on Tickets (Priority: P1)

A base staff member receives a phone call from a patient regarding a delayed order. They create a new ticket, link it to the patient's contact record, add relevant details, and optionally assign it to the appropriate team. They can add comments to track the conversation but cannot modify ticket attributes.

**Why this priority**: This is the fundamental workflow—staff must be able to create tickets and document interactions. Without this, no work can be captured.

**Independent Test**: Can be fully tested by creating a ticket, linking a contact, adding comments, and verifying the ticket appears in all relevant views.

**Acceptance Scenarios**:

1. **Given** a logged-in base staff user, **When** they navigate to `/staff/tickets/` and click "New Ticket", **Then** they see a form to enter title, description, optional team assignment, and optional contact linkage.

2. **Given** a base staff user creating a ticket, **When** they submit the form without selecting a team or assignee, **Then** the ticket is created with null owning_team_id and null assignee_id.

3. **Given** a base staff user viewing any ticket, **When** they add a comment, **Then** the comment is saved with their staff_id and timestamp, and appears in the ticket's comment thread.

4. **Given** a base staff user viewing a ticket, **When** they attempt to change status, priority, or assignee, **Then** those controls are disabled or hidden.

---

### User Story 2 - Staff with Manage Permission Modifies and Reassigns Tickets (Priority: P1)

A supervisor with "Can Manage Tickets" permission reviews the ticket queue, updates priorities based on urgency, reassigns tickets to appropriate team members, and closes resolved tickets.

**Why this priority**: Ticket lifecycle management is essential for workflow completion. Without modification capabilities, tickets cannot progress.

**Independent Test**: Can be tested by modifying ticket attributes, reassigning to users/teams, closing tickets, and verifying all changes are logged.

**Acceptance Scenarios**:

1. **Given** a staff user with "Can Manage Tickets" permission viewing a ticket, **When** they change the status from "Open" to "In Progress", **Then** the status updates, the change is logged in the ticket change log with old and new values, and timestamps are updated.

2. **Given** a staff user with "Can Manage Tickets" permission, **When** they reassign a ticket to a different user, **Then** the assignee changes, the previous assignee (if any) is logged, and the new assignee receives a notification (per their settings).

3. **Given** a staff user with "Can Manage Tickets" permission, **When** they close a ticket, **Then** the status changes to "Closed", closed_at timestamp is set, and the change is logged.

4. **Given** a staff user with "Can Manage Tickets" permission, **When** they reopen a previously closed ticket, **Then** the status changes, closed_at is cleared, and a reopen event is logged.

---

### User Story 3 - Contact Management and Ticket Association (Priority: P1)

A staff member needs to look up a contact by phone number (entered in various formats), view their profile, see all associated tickets, and add notes about recent interactions.

**Why this priority**: Contacts are central to DME operations—linking patients, providers, and insurers to tickets is essential for context and continuity.

**Independent Test**: Can be tested by creating contacts, searching by phone/email/name, linking to tickets, and viewing the contact's ticket list.

**Acceptance Scenarios**:

1. **Given** a staff user on `/staff/contacts/`, **When** they search by phone number "555-123-4567", **Then** the system normalizes the input and matches contacts stored as "+15551234567".

2. **Given** a staff user viewing a contact's detail page, **When** they click the "Tickets" tab, **Then** they see a filterable, sortable list of all tickets linked to that contact.

3. **Given** a staff user editing a contact, **When** they change the email address, **Then** the change is saved and logged in the contact change log with old and new values.

4. **Given** a staff user viewing a contact, **When** they add a comment, **Then** the comment is saved and visible in the contact's activity feed.

---

### User Story 4 - Using Views to Filter and Manage Ticket Lists (Priority: P2)

A staff member uses the "My Active Tickets" view to see their assigned open tickets, then switches to "Unassigned Tickets" to find work. They use the search bar to filter results within the current view.

**Why this priority**: Views enable efficient ticket discovery and workload management. Essential for productivity but builds on core ticket functionality.

**Independent Test**: Can be tested by selecting different views, verifying correct filters apply, using search, and confirming search respects visible columns.

**Acceptance Scenarios**:

1. **Given** a staff user on `/staff/tickets/`, **When** they select "My Active Tickets" view, **Then** only tickets assigned to them with status not "Closed" are displayed.

2. **Given** a staff user with "Unassigned Tickets" view active, **When** they type "wheelchair" in the search bar, **Then** results are filtered to unassigned tickets where "wheelchair" appears in the visible columns only.

3. **Given** a staff user creating a custom view, **When** they define conditions (status = "Open" AND priority = "High"), **Then** the view is saved and shows only matching tickets.

4. **Given** a staff user with a view showing columns [Title, Contact Name, Status], **When** they search for a term that only exists in the Description field, **Then** no results are returned (search only applies to visible columns).

---

### User Story 5 - SLA Assignment and Breach Monitoring (Priority: P2)

A ticket is created with priority "Urgent" and automatically receives an SLA requiring resolution within 4 business hours. As time passes, the system updates the SLA status. If the ticket approaches breach, notifications are sent.

**Why this priority**: SLA compliance is critical for DME operations (insurance requirements, patient care timelines). Builds on ticket infrastructure.

**Independent Test**: Can be tested by creating tickets matching SLA conditions, verifying SLA assignment, observing status changes as time passes, and confirming notifications fire.

**Acceptance Scenarios**:

1. **Given** an SLA rule configured for priority = "Urgent" with 4-hour target, **When** a ticket is created with priority "Urgent", **Then** sla_id is set, sla_due_at is calculated based on business hours, and sla_status is "On Track".

2. **Given** a ticket with sla_due_at approaching (e.g., 30 minutes remaining), **When** the background job runs, **Then** sla_status changes to "Approaching Breach" and configured notifications are sent.

3. **Given** a ticket past its sla_due_at, **When** the background job runs, **Then** sla_status changes to "Breached", sla_breached_at is set, and breach notifications fire.

4. **Given** a ticket's priority changes from "Normal" to "Urgent", **When** saved, **Then** the SLA is re-evaluated, potentially assigning a new SLA with updated due date.

---

### User Story 6 - Round Robin Auto-Assignment (Priority: P2)

A team has round-robin enabled. When a ticket is assigned to the team without a specific assignee, the system automatically assigns it to the next eligible team member.

**Why this priority**: Automates workload distribution and reduces manual assignment overhead. Requires team configuration to be in place.

**Independent Test**: Can be tested by configuring a team with eligible members, assigning tickets to the team, and verifying sequential distribution.

**Acceptance Scenarios**:

1. **Given** Team A has round-robin enabled with 3 eligible members, **When** a ticket is created and assigned to Team A without an assignee, **Then** the system assigns it to the next member in rotation.

2. **Given** a team member with is_assignable = false, **When** round-robin runs, **Then** that member is skipped and the next eligible member receives the assignment.

3. **Given** a team with no eligible members (all is_assignable = false), **When** a ticket is assigned to the team, **Then** the ticket remains unassigned within the team and an appropriate indicator is shown.

4. **Given** a ticket is reassigned from Team A to Team B, **When** Team B has round-robin enabled, **Then** the system auto-assigns to Team B's next eligible member.

---

### User Story 7 - Viewing Dashboards and Metrics (Priority: P3)

A staff member views their personal dashboard to see open tickets, resolution trends, and performance metrics. A supervisor views team-level summaries.

**Why this priority**: Metrics provide operational visibility and performance insights. Valuable but builds on completed ticket workflows.

**Independent Test**: Can be tested by viewing dashboard with various ticket states, verifying counts are accurate, and checking metric calculations.

**Acceptance Scenarios**:

1. **Given** a staff user on their dashboard, **When** they have 5 open assigned tickets, **Then** the "Open Tickets" count shows 5.

2. **Given** a staff user who resolved 10 tickets in the last 7 days, **When** viewing their dashboard, **Then** "Tickets Resolved (7 days)" shows 10.

3. **Given** a staff user with resolved tickets having median close time of 2.5 hours, **When** viewing their dashboard, **Then** the median close time metric displays "2.5 hours".

---

### User Story 8 - Notification Preferences and Delivery (Priority: P3)

A staff user configures their notification preferences to receive email notifications for ticket assignments but webhook notifications for SLA breaches.

**Why this priority**: Notifications enhance responsiveness but are an enhancement to core functionality.

**Independent Test**: Can be tested by configuring preferences, triggering notification events, and verifying correct channels receive the notifications.

**Acceptance Scenarios**:

1. **Given** a staff user with email enabled for "Ticket Assigned", **When** a ticket is assigned to them, **Then** they receive an email notification.

2. **Given** a staff user with webhook enabled for "SLA Breached", **When** their assigned ticket breaches SLA, **Then** the configured webhook endpoint receives a payload.

3. **Given** a staff user with all notifications disabled, **When** any notification event occurs, **Then** no notifications are sent to them.

---

### User Story 9 - Team Management with Manage Teams Permission (Priority: P2)

A staff user with "Manage Teams" permission configures teams, assigns members, and manages round-robin settings. Users without this permission can view but not modify team configuration.

**Why this priority**: Team configuration is prerequisite for assignment flows and round-robin. Parallel priority with features that depend on it.

**Independent Test**: Can be tested by creating teams, adding members, toggling settings, and verifying permission enforcement.

**Acceptance Scenarios**:

1. **Given** a staff user with "Manage Teams" permission on `/admin/teams/`, **When** they create a new team with name and description, **Then** the team is created and available for assignment.

2. **Given** a staff user with "Manage Teams" permission editing a team, **When** they add a staff member and set is_assignable = true, **Then** the member appears in team membership and is eligible for round-robin.

3. **Given** a staff user WITHOUT "Manage Teams" permission on `/admin/teams/`, **When** they attempt to create or edit a team, **Then** the action is blocked and an appropriate error is shown.

4. **Given** any staff user, **When** they view the team list, **Then** they can see all teams and their members (read-only access).

---

### User Story 10 - Accessing Reports with Access Reports Permission (Priority: P2)

A staff user with "Access Reports" permission views team-level and organization-wide reports, analyzes SLA compliance, and exports data. Users without this permission see only their personal dashboard.

**Why this priority**: Reporting provides operational visibility for supervisors and management. Builds on metrics tracking infrastructure.

**Independent Test**: Can be tested by accessing various report types and verifying permission enforcement.

**Acceptance Scenarios**:

1. **Given** a staff user with "Access Reports" permission, **When** they navigate to `/admin/reports/`, **Then** they see team-level and organization-wide report options.

2. **Given** a staff user with "Access Reports" permission viewing team reports, **When** they select a team, **Then** they see metrics including open tickets, SLA breaches, resolution rates, and member performance.

3. **Given** a staff user with "Access Reports" permission, **When** they click "Export", **Then** they can download report data as CSV or PDF.

4. **Given** a staff user WITHOUT "Access Reports" permission, **When** they navigate to `/admin/reports/`, **Then** they are denied access and redirected to their personal dashboard.

5. **Given** any staff user on their personal dashboard, **When** they view metrics, **Then** they see only their own performance data (open tickets, resolved count, close time, reopen rate).

---

### User Story 11 - System Administrator SLA Configuration (Priority: P3)

A system administrator configures SLA rules, system-wide views, and global notification settings. This requires elevated access beyond the standard staff permissions.

**Why this priority**: System configuration is foundational but typically set up once and rarely modified.

**Independent Test**: Can be tested by creating SLA rules and verifying they are applied to matching tickets.

**Acceptance Scenarios**:

1. **Given** a system administrator on `/admin/sla/`, **When** they create an SLA with conditions (priority = "High", owning_team = "Billing"), target of 8 hours, and business_hours = true, **Then** the SLA is saved and evaluated against matching tickets.

2. **Given** a staff user with only "Manage Teams" or "Access Reports" permissions, **When** they attempt to access `/admin/sla/`, **Then** access is denied.

---

### Edge Cases

- **No matching SLA**: When a ticket doesn't match any SLA conditions, sla_id, sla_due_at, and sla_status remain null.
- **Business hours span holidays**: If holidays are configured, SLA calculations must exclude holiday hours from target time.
- **Contact with no tickets**: Contact detail page shows empty ticket list with clear messaging.
- **Team deleted with assigned tickets**: Tickets retain historical team reference; system handles gracefully in queries and UI.
- **Concurrent ticket updates**: Two users editing same ticket simultaneously—last write wins with change log capturing both changes.
- **Phone number edge cases**: International formats, extensions, vanity numbers must all normalize correctly or display validation errors.
- **Circular reassignment**: Staff reassigns ticket rapidly between users—each assignment is logged; no infinite loops possible.
- **Permission combinations**: User with multiple permissions (e.g., "Can Manage Tickets" + "Manage Teams") receives union of all capabilities.
- **Permission revocation**: When a user loses a permission, previously performed actions remain in audit logs; future actions are blocked.
- **Team manager without ticket permission**: User with "Manage Teams" but not "Can Manage Tickets" can configure teams but cannot modify tickets assigned to those teams.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Ticket Management

- **FR-001**: System MUST allow any authenticated staff user to create tickets with title, description, optional team assignment, and optional contact linkage.
- **FR-002**: System MUST allow any authenticated staff user to view all tickets in the system regardless of assignment.
- **FR-003**: System MUST allow any authenticated staff user to add comments to any ticket.
- **FR-004**: System MUST restrict ticket attribute modification (status, priority, category, tags, team, assignee) to users with "Can Manage Tickets" permission.
- **FR-005**: System MUST allow users with "Can Manage Tickets" permission to close, reopen, and reassign tickets.
- **FR-006**: System MUST record all ticket changes in an immutable, append-only change log with timestamp, actor, field changes, old values, new values, and optional message.
- **FR-007**: System MUST support ticket statuses: Open, In Progress, Pending, Resolved, Closed.
- **FR-008**: System MUST support ticket priorities: Low, Normal, High, Urgent.
- **FR-009**: System MUST support file attachments on tickets.
- **FR-010**: System MUST automatically set created_at on ticket creation and updated_at on any modification.
- **FR-011**: System MUST set resolved_at when status changes to "Resolved" and closed_at when status changes to "Closed".

#### Contact Management

- **FR-012**: System MUST allow any authenticated staff user to search contacts by id, name (partial match), normalized phone number, email, and organization/account.
- **FR-013**: System MUST allow any authenticated staff user to create and edit contacts.
- **FR-014**: System MUST normalize phone numbers to E.164 format on storage while accepting various input formats.
- **FR-015**: System MUST match unnormalized search input against normalized stored phone numbers.
- **FR-016**: System MUST display all tickets associated with a contact on the contact detail page.
- **FR-017**: System MUST allow any authenticated staff user to add comments on contacts.
- **FR-018**: System MUST record all contact changes in an immutable, append-only change log.

#### Views and Search

- **FR-019**: System MUST provide default views: All Tickets, My Active Tickets, Tickets I Opened, Unassigned Tickets, Open Tickets, Recently Updated, Recently Closed.
- **FR-020**: System MUST provide team-specific default views: All Tickets for Team, Open Tickets for Team, Unassigned for Team.
- **FR-021**: System MUST support custom view creation with AND/OR condition builder on: status, priority, category, owning_team_id, assignee_id, created_by_staff_id, contact_id, date ranges, and tags.
- **FR-022**: System MUST support primary and secondary sort fields with direction in views.
- **FR-023**: System MUST allow users to select which columns are visible and their order in views.
- **FR-024**: System MUST provide a search bar on each view that searches only the columns visible in that view.
- **FR-025**: System MUST apply search as an additive filter on top of view conditions.

#### SLA Management

- **FR-026**: System MUST support SLA rule configuration with conditions (status, priority, category, owning team, contact type), target resolution time, optional target close time, business hours flag, and active flag.
- **FR-027**: System MUST support business hours configuration: workdays (default Mon-Fri, customizable), start time, end time.
- **FR-028**: System MUST evaluate all active SLA rules when a ticket is created or modified and assign the first matching rule.
- **FR-029**: System MUST calculate sla_due_at based on target time and business hours configuration when applicable.
- **FR-030**: System MUST update ticket sla_status via background job: On Track, Approaching Breach, Breached, Completed.
- **FR-031**: System MUST set sla_breached_at timestamp when sla_status transitions to Breached.
- **FR-032**: System MUST re-evaluate SLA when applicable ticket fields change and log SLA changes in the ticket change log.
- **FR-033**: System MUST display SLA name, due date, current status, and breach indicators on the ticket detail page.

#### Teams and Assignment

- **FR-034**: System MUST support team creation with name and description under `/admin/teams/`.
- **FR-035**: System MUST support team membership with team_id, staff_admin_id, and is_assignable flag.
- **FR-036**: System MUST NOT affect ticket visibility based on team membership (all staff see all tickets).
- **FR-037**: System MUST support round-robin toggle per team.
- **FR-038**: System MUST auto-assign tickets to next eligible (is_assignable = true) team member when round-robin is enabled and ticket enters team's unassigned pool.
- **FR-039**: System MUST handle round-robin when no eligible members exist (ticket remains unassigned).
- **FR-040**: System MUST re-run round-robin when owning_team_id changes on an existing ticket.

#### Notifications

- **FR-041**: System MUST support notification events: Ticket Assigned, Ticket Assigned to Team, Comment Added, Status Changed, Ticket Closed, Ticket Reopened, SLA Approaching Breach, SLA Breached.
- **FR-042**: System MUST support notification channels: Email and Webhook.
- **FR-043**: System MUST allow users to toggle notification preferences per event type and per channel.
- **FR-044**: System MUST support global notification defaults configurable by administrators.

#### Metrics and Dashboard

- **FR-045**: System MUST provide a per-user dashboard showing: open assigned tickets, tickets resolved in last 7/30 days, median close time, reopen rate (available to all staff).
- **FR-046**: System MUST provide team-level reports accessible only to users with "Access Reports" permission.
- **FR-047**: System MUST provide organization-wide reports accessible only to users with "Access Reports" permission.
- **FR-048**: System MUST support report data export (CSV, PDF) for users with "Access Reports" permission.
- **FR-049**: System MUST track metrics: status transitions, assignment transitions, resolution timestamps, close timestamps, reopens, SLA breaches and approach events.

#### Team Management

- **FR-050**: System MUST restrict team creation, editing, and deletion to users with "Manage Teams" permission.
- **FR-051**: System MUST restrict team membership management (add/remove members) to users with "Manage Teams" permission.
- **FR-052**: System MUST restrict toggling member assignment eligibility (is_assignable) to users with "Manage Teams" permission.
- **FR-053**: System MUST restrict round-robin configuration to users with "Manage Teams" permission.
- **FR-054**: System MUST allow all staff users to view team lists and membership (read-only).

#### Admin Configuration

- **FR-055**: System MUST provide administrative interfaces under `/admin/` for: teams, team membership, round-robin settings, SLA configuration, default view management, global notification defaults, system configuration.
- **FR-056**: System MUST restrict SLA configuration to system administrators.
- **FR-057**: System MUST restrict system-wide view management to system administrators.
- **FR-058**: System MUST restrict global notification configuration to system administrators.
- **FR-059**: System MUST restrict system settings to system administrators.

### Key Entities

#### Staff Admin User
Represents an internal staff member who can access the ticketing system. Has authentication credentials and permission flags that control access to various system capabilities.

**Key Attributes**: id, name, email, can_manage_tickets (boolean), manage_teams (boolean), access_reports (boolean), created_at, updated_at

#### Team
Represents a functional group for ticket assignment, round-robin distribution, and reporting.

**Key Attributes**: id, name, description, round_robin_enabled, created_at, updated_at

#### Team Membership
Junction entity linking staff to teams with assignment eligibility.

**Key Attributes**: id, team_id, staff_admin_id, is_assignable, created_at, updated_at

#### Ticket
Core entity representing a work item or issue to be tracked and resolved.

**Key Attributes**: id, title, description (rich text), status, priority, category, owning_team_id (nullable), assignee_id (nullable), created_by_staff_id (nullable), contact_id (nullable), tags, created_at, updated_at, resolved_at (nullable), closed_at (nullable), sla_id (nullable), sla_due_at (nullable), sla_breached_at (nullable), sla_status

#### Ticket Change Log Entry
Immutable audit record of ticket modifications.

**Key Attributes**: id, ticket_id, timestamp, actor_staff_id (nullable for system actions), field_changes (JSON: field name → {old_value, new_value}), message (optional)

#### Ticket Comment
User-added note on a ticket.

**Key Attributes**: id, ticket_id, author_staff_id, created_at, body (rich text)

#### Ticket Attachment
File attached to a ticket.

**Key Attributes**: id, ticket_id, filename, file_path, content_type, size_bytes, uploaded_by_staff_id, created_at

#### Contact
External person or entity associated with tickets (patients, providers, insurance reps, etc.).

**Key Attributes**: id, name, email, phone_numbers (array, normalized to E.164), address, organization_account, dme_identifiers (JSON), created_at, updated_at

#### Contact Change Log Entry
Immutable audit record of contact modifications.

**Key Attributes**: id, contact_id, timestamp, actor_staff_id, field_changes (JSON), message (optional)

#### Contact Comment
User-added note on a contact.

**Key Attributes**: id, contact_id, author_staff_id, created_at, body (rich text)

#### View
Saved filter configuration for ticket lists.

**Key Attributes**: id, name, owner_staff_id (nullable for system views), is_default, conditions (JSON: AND/OR filter tree), sort_primary_field, sort_primary_direction, sort_secondary_field (nullable), sort_secondary_direction (nullable), visible_columns (ordered array), created_at, updated_at

#### SLA Rule
Configuration defining service level expectations.

**Key Attributes**: id, name, description, conditions (JSON: matching criteria), target_resolution_time (duration), target_close_time (duration, nullable), business_hours_enabled, business_hours_config (JSON: workdays, start_time, end_time), active, breach_behavior (JSON: UI markers, notification settings, webhook config), created_at, updated_at

#### Notification Preference
User-level settings for notification delivery.

**Key Attributes**: id, staff_admin_id, event_type, email_enabled, webhook_enabled, webhook_url (nullable)

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff can create a new ticket and link it to a contact in under 60 seconds.
- **SC-002**: Search by phone number returns matching contacts regardless of input format (with 100% accuracy for valid numbers).
- **SC-003**: Ticket attribute changes are reflected in the change log within 1 second of save.
- **SC-004**: SLA status updates (Approaching Breach, Breached) occur within 5 minutes of threshold crossing.
- **SC-005**: Round-robin assignment distributes tickets evenly across eligible team members (variance < 10% over 100 tickets).
- **SC-006**: Staff with "Can Manage Tickets" permission can reassign a ticket in under 30 seconds.
- **SC-007**: View filtering and search return results within 2 seconds for datasets up to 100,000 tickets.
- **SC-008**: Dashboard metrics load within 3 seconds.
- **SC-009**: 100% of ticket and contact modifications are captured in their respective change logs.
- **SC-010**: Notification delivery occurs within 60 seconds of triggering event.
- **SC-011**: System supports 500 concurrent staff users without performance degradation.
- **SC-012**: Base staff users cannot modify ticket attributes even with direct manipulation attempts (100% server-side enforcement).

---

## Permissions Model

### Permission Flags

Staff users may have any combination of the following permission flags:

| Permission Flag | Description |
|-----------------|-------------|
| **Can Manage Tickets** | Modify ticket attributes, reassign, close/reopen tickets |
| **Manage Teams** | Create/edit/delete teams, manage membership, configure round-robin |
| **Access Reports** | View team-level and organization-level metrics, dashboards, and reports |

### Capability Matrix

| Capability | Base Staff | + Can Manage Tickets | + Manage Teams | + Access Reports |
|------------|:----------:|:--------------------:|:--------------:|:----------------:|
| **Ticket Viewing** | | | | |
| View all tickets | ✓ | ✓ | ✓ | ✓ |
| View ticket details | ✓ | ✓ | ✓ | ✓ |
| View ticket change logs | ✓ | ✓ | ✓ | ✓ |
| **Ticket Actions** | | | | |
| Add comments to tickets | ✓ | ✓ | ✓ | ✓ |
| Create tickets | ✓ | ✓ | ✓ | ✓ |
| Assign ticket to team on creation | ✓ | ✓ | ✓ | ✓ |
| Modify ticket attributes | ✗ | ✓ | — | — |
| Take/claim tickets | ✗ | ✓ | — | — |
| Reassign tickets | ✗ | ✓ | — | — |
| Close/reopen tickets | ✗ | ✓ | — | — |
| **Contacts** | | | | |
| Manage contacts (CRUD) | ✓ | ✓ | ✓ | ✓ |
| View contact change logs | ✓ | ✓ | ✓ | ✓ |
| Add comments on contacts | ✓ | ✓ | ✓ | ✓ |
| **Views** | | | | |
| Use accessible Views | ✓ | ✓ | ✓ | ✓ |
| Create personal custom views | ✓ | ✓ | ✓ | ✓ |
| **Teams (requires Manage Teams)** | | | | |
| View team list and membership | ✓ | ✓ | ✓ | ✓ |
| Create/edit/delete teams | ✗ | ✗ | ✓ | — |
| Add/remove team members | ✗ | ✗ | ✓ | — |
| Toggle member assignment eligibility | ✗ | ✗ | ✓ | — |
| Configure round-robin settings | ✗ | ✗ | ✓ | — |
| **Reports (requires Access Reports)** | | | | |
| View personal dashboard metrics | ✓ | ✓ | ✓ | ✓ |
| View team-level reports | ✗ | ✗ | — | ✓ |
| View organization-wide reports | ✗ | ✗ | — | ✓ |
| View SLA performance reports | ✗ | ✗ | — | ✓ |
| Export report data | ✗ | ✗ | — | ✓ |

*Legend: ✓ = granted, ✗ = denied, — = not affected by this permission*

### Key Permission Rules

- **Permissions are additive**: Users can have multiple permission flags (e.g., "Can Manage Tickets" + "Manage Teams").
- **Team membership does NOT affect visibility**: All staff see all tickets regardless of team.
- **Team membership is for operations**: Assignment flows, round-robin distribution, and reporting grouping.
- **Permission checks MUST be enforced server-side**: UI hides controls as UX convenience, not security.
- **SLA and View configuration**: Requires system administrator access (separate from these three permissions).

---

## Domain Model - Entity Relationships

```
StaffAdminUser
├── has_many TeamMemberships
├── has_many Tickets (as assignee)
├── has_many Tickets (as creator)
├── has_many TicketComments (as author)
├── has_many ContactComments (as author)
├── has_many Views (as owner)
└── has_many NotificationPreferences

Team
├── has_many TeamMemberships
├── has_many Tickets (as owning_team)
└── belongs_to SLA Rules (via conditions)

Ticket
├── belongs_to Team (owning_team, optional)
├── belongs_to StaffAdminUser (assignee, optional)
├── belongs_to StaffAdminUser (creator, optional)
├── belongs_to Contact (optional)
├── belongs_to SLA Rule (optional)
├── has_many TicketChangeLogEntries
├── has_many TicketComments
└── has_many TicketAttachments

Contact
├── has_many Tickets
├── has_many ContactChangeLogEntries
└── has_many ContactComments

View
└── belongs_to StaffAdminUser (owner, optional for system views)

SLARule
└── has_many Tickets (via sla_id)
```

---

## Views + Search Behavior

### Default System Views

| View Name | Filter Conditions |
|-----------|------------------|
| All Tickets | None |
| My Active Tickets | assignee_id = current_user AND status NOT IN (Closed) |
| Tickets I Opened | created_by_staff_id = current_user |
| Unassigned Tickets | assignee_id IS NULL |
| Open Tickets | status = Open |
| Recently Updated | ORDER BY updated_at DESC, LIMIT 100 |
| Recently Closed | status = Closed, ORDER BY closed_at DESC, LIMIT 100 |

### Team Default Views (per team)

| View Name | Filter Conditions |
|-----------|------------------|
| All Tickets for Team {Name} | owning_team_id = team_id |
| Open Tickets for Team {Name} | owning_team_id = team_id AND status = Open |
| Unassigned for Team {Name} | owning_team_id = team_id AND assignee_id IS NULL |

### Custom View Builder

**Condition Types**:
- Equality: field = value
- Inequality: field != value
- In Set: field IN (value1, value2, ...)
- Date Range: field BETWEEN date1 AND date2
- Null Check: field IS NULL / IS NOT NULL
- Tag Contains: tags CONTAINS value

**Logical Operators**: AND, OR with grouping support

**Sortable Fields**: id, title, status, priority, category, owning_team_id, assignee_id, created_at, updated_at, resolved_at, closed_at, sla_due_at

### Search Behavior

- Search bar appears on every view
- Search query matches against visible columns ONLY
- Search is applied as AND with existing view filters
- Case-insensitive partial matching
- Example: View with columns [Title, Contact Name, Status], search "smith" matches:
  - Tickets with "smith" in title
  - Tickets linked to contacts with "smith" in name
  - Does NOT match "smith" in description (not visible)

---

## SLA Rules and Behavior

### SLA Rule Structure

```
SLA Rule:
  - id: unique identifier
  - name: display name (e.g., "Urgent Priority SLA")
  - description: explanation of the rule
  - conditions: matching criteria
    - priority: Urgent
    - category: Equipment Failure (optional)
    - owning_team: any or specific team
  - target_resolution_time: 4 hours
  - target_close_time: 24 hours (optional)
  - business_hours_enabled: true
  - business_hours_config:
    - workdays: [Monday, Tuesday, Wednesday, Thursday, Friday]
    - start_time: 08:00
    - end_time: 18:00
  - active: true
  - breach_behavior:
    - ui_markers: true (red indicator)
    - notify_assignee: true
    - notify_team_leads: true
    - webhook_url: https://...
```

### SLA Evaluation Flow

1. **On Ticket Create/Update**:
   - Fetch all active SLA rules ordered by priority
   - Evaluate conditions against ticket fields
   - First matching rule is assigned
   - Calculate sla_due_at considering business hours if enabled
   - Set sla_status = "On Track"
   - Log SLA assignment in change log

2. **On Field Change Affecting SLA**:
   - Re-evaluate SLA rules
   - If new SLA matches, update sla_id and recalculate sla_due_at
   - Log change with old and new SLA details

3. **Background Job (runs every 1-5 minutes)**:
   - Query tickets with non-null sla_due_at and sla_status != "Completed"
   - For each ticket:
     - Calculate time remaining (considering business hours)
     - If < threshold (e.g., 1 hour): set "Approaching Breach", trigger notifications
     - If past due: set "Breached", set sla_breached_at, trigger breach notifications
   - Update sla_status and timestamps

### SLA Status Values

| Status | Condition |
|--------|-----------|
| On Track | Time remaining > approach threshold |
| Approaching Breach | Time remaining ≤ approach threshold (configurable, default 1 hour or 25% remaining) |
| Breached | Current time > sla_due_at |
| Completed | Ticket resolved/closed before breach |

---

## Metrics & Reporting

### Access Control

- **Personal Dashboard**: Available to all staff users (shows only their own metrics)
- **Team Reports**: Requires "Access Reports" permission
- **Organization Reports**: Requires "Access Reports" permission
- **Report Export**: Requires "Access Reports" permission

### Per-User Dashboard (All Staff)

| Metric | Calculation |
|--------|-------------|
| Open Assigned Tickets | COUNT(tickets WHERE assignee_id = user AND status NOT IN (Closed)) |
| Resolved (7 days) | COUNT(tickets WHERE assignee_id = user AND resolved_at >= NOW - 7 days) |
| Resolved (30 days) | COUNT(tickets WHERE assignee_id = user AND resolved_at >= NOW - 30 days) |
| Median Close Time | MEDIAN(closed_at - created_at) for user's resolved tickets |
| Reopen Rate | COUNT(reopened) / COUNT(closed) for user's tickets |

### Team Reports (Requires Access Reports)

| Metric | Calculation |
|--------|-------------|
| Team Open Tickets | COUNT(tickets WHERE owning_team_id = team AND status NOT IN (Closed)) |
| Team Unassigned | COUNT(tickets WHERE owning_team_id = team AND assignee_id IS NULL) |
| Team SLA Breaches (7 days) | COUNT(tickets WHERE owning_team_id = team AND sla_breached_at >= NOW - 7 days) |
| Team Resolution Rate | Tickets resolved / Tickets created per period |
| Team Avg Response Time | AVG(first_response_at - created_at) for team tickets |
| Member Performance | Per-member metrics within team |

### Organization Reports (Requires Access Reports)

| Metric | Calculation |
|--------|-------------|
| Total Open Tickets | COUNT(tickets WHERE status NOT IN (Closed)) |
| Overall SLA Compliance | % of tickets resolved within SLA |
| Tickets by Priority | Distribution across priority levels |
| Tickets by Category | Distribution across categories |
| Volume Trends | Ticket creation/resolution over time |
| Team Comparison | Side-by-side team performance |

### Tracked Events for Metrics

- Status transitions (from → to, timestamp)
- Assignment transitions (from → to, timestamp)
- Resolution timestamp
- Close timestamp
- Reopen events
- SLA breach events
- SLA approaching events

---

## Notifications

### Notification Events

| Event | Description |
|-------|-------------|
| ticket_assigned | Ticket assigned directly to user |
| ticket_assigned_team | Ticket assigned to user's team |
| comment_added | Comment added on ticket user created, is assigned to, or is watching |
| status_changed | Ticket status changed |
| ticket_closed | Ticket closed |
| ticket_reopened | Ticket reopened |
| sla_approaching | SLA approaching breach threshold |
| sla_breached | SLA has been breached |

### Notification Channels

- **Email**: Formatted email to user's registered address
- **Webhook**: HTTP POST to user-configured URL with JSON payload

### Notification Payload Structure

```json
{
  "event_type": "ticket_assigned",
  "timestamp": "2025-12-11T14:30:00Z",
  "ticket_id": "TKT-12345",
  "ticket_title": "Equipment delivery delayed",
  "actor": {
    "id": "staff-001",
    "name": "Jane Smith"
  },
  "details": {
    "previous_assignee_id": null,
    "new_assignee_id": "staff-002"
  }
}
```

### User Preference Settings

| Setting | Options |
|---------|---------|
| Event Toggle | Enable/disable per event type |
| Channel Selection | Email, Webhook, or Both |
| Webhook URL | User-provided endpoint (validated) |

---

## Admin Configuration

### Permission Requirements by Area

| Admin Area | Required Permission |
|------------|---------------------|
| `/admin/teams/` | Manage Teams |
| `/admin/teams/{id}/members/` | Manage Teams |
| `/admin/reports/` | Access Reports |
| `/admin/sla/` | System Administrator |
| `/admin/views/` | System Administrator |
| `/admin/notifications/` | System Administrator |
| `/admin/settings/` | System Administrator |

*Note: "System Administrator" is a separate administrative role beyond the three staff permissions.*

### `/admin/teams/` (Requires Manage Teams)
- Create, edit, delete teams
- View team membership list
- Toggle round-robin enabled/disabled

### `/admin/teams/{id}/members/` (Requires Manage Teams)
- Add/remove team members
- Toggle is_assignable per member
- View assignment statistics

### `/admin/reports/` (Requires Access Reports)
- View team-level performance reports
- View organization-wide analytics
- View SLA compliance reports
- Export report data (CSV, PDF)
- Schedule automated report delivery

### `/admin/sla/` (System Administrator)
- Create, edit, deactivate SLA rules
- Configure conditions, targets, business hours
- Set breach behavior (UI, notifications, webhooks)
- Reorder SLA priority (first match wins)

### `/admin/views/` (System Administrator)
- Manage system default views
- Create organization-wide shared views
- Set view visibility (all users, specific teams)

### `/admin/notifications/` (System Administrator)
- Configure global notification defaults
- Set default webhook endpoints
- Configure email templates

### `/admin/settings/` (System Administrator)
- System-wide configuration
- Business hours defaults
- Holiday calendar
- Default ticket values

---

## Security & Audit

### Audit Trail

- **Ticket Change Log**: All ticket modifications captured with actor, timestamp, old/new values
- **Contact Change Log**: All contact modifications captured with actor, timestamp, old/new values
- **System actions** logged with actor_staff_id = NULL and system identifier in message

### Permission Enforcement

- All permission checks MUST be enforced server-side
- UI hides/disables controls as convenience, not security
- API endpoints validate permission before executing action
- Failed permission checks logged for security monitoring

### Data Protection

- Phone numbers stored in normalized format, displayed in localized format
- Sensitive contact identifiers (DME IDs) access may require additional permissions
- Audit logs are immutable and append-only (no delete/update operations)

---

## Recommended Database Indexing and Relationships

### Primary Indexes (Unique)

- tickets.id
- contacts.id
- staff_admin_users.id
- teams.id
- team_memberships.id
- ticket_change_log_entries.id
- contact_change_log_entries.id
- ticket_comments.id
- contact_comments.id
- views.id
- sla_rules.id
- notification_preferences.id

### Foreign Key Indexes

- tickets.owning_team_id → teams.id
- tickets.assignee_id → staff_admin_users.id
- tickets.created_by_staff_id → staff_admin_users.id
- tickets.contact_id → contacts.id
- tickets.sla_id → sla_rules.id
- team_memberships.team_id → teams.id
- team_memberships.staff_admin_id → staff_admin_users.id
- ticket_change_log_entries.ticket_id → tickets.id
- ticket_comments.ticket_id → tickets.id
- contact_change_log_entries.contact_id → contacts.id
- contact_comments.contact_id → contacts.id
- views.owner_staff_id → staff_admin_users.id

### Query Performance Indexes

- tickets(status) — filter by status
- tickets(priority) — filter by priority
- tickets(owning_team_id, status) — team views
- tickets(assignee_id, status) — "My Active Tickets"
- tickets(created_by_staff_id) — "Tickets I Opened"
- tickets(created_at) — sorting, date range queries
- tickets(updated_at) — "Recently Updated"
- tickets(closed_at) — "Recently Closed"
- tickets(sla_due_at, sla_status) — SLA background job queries
- contacts(email) — email search
- contacts(organization_account) — organization search
- contacts(name) — partial name search (consider full-text index)
- contacts(phone_numbers) — normalized phone search (GIN index for array)
- team_memberships(team_id, is_assignable) — round-robin queries

### Composite Indexes

- tickets(owning_team_id, assignee_id) — "Unassigned for Team"
- tickets(sla_status, sla_due_at) — SLA monitoring
- ticket_change_log_entries(ticket_id, timestamp) — change history display
- contact_change_log_entries(contact_id, timestamp) — change history display

### Full-Text Search Indexes

- tickets.title — text search
- tickets.description — text search
- contacts.name — partial match search

### Constraints

- team_memberships: UNIQUE(team_id, staff_admin_id)
- notification_preferences: UNIQUE(staff_admin_id, event_type)

---

## Assumptions

The following assumptions were made where details were not explicitly specified:

1. **Authentication**: Staff users authenticate via the existing authentication system in the codebase. No new authentication mechanism required.

2. **Ticket Statuses**: Standard workflow statuses (Open, In Progress, Pending, Resolved, Closed) assumed based on common ticketing patterns.

3. **Ticket Priorities**: Four-level priority system (Low, Normal, High, Urgent) assumed as industry standard.

4. **SLA Approach Threshold**: Default approaching breach threshold is 1 hour or 25% of remaining time, whichever is greater.

5. **Rich Text**: Description and comment fields support rich text (bold, italic, lists, links) via a standard editor.

6. **Attachment Storage**: Attachments stored in existing file storage infrastructure.

7. **Time Zones**: All timestamps stored in UTC; displayed in user's configured timezone.

8. **Holidays**: Holiday calendar support is optional/configurable in SLA business hours calculation.

9. **Soft Delete**: Tickets and contacts use soft delete (is_deleted flag) rather than hard delete to preserve audit history.

10. **Tag Structure**: Tags are free-form strings; no predefined tag taxonomy required.

---

## Open Questions

No critical questions remain that would block specification completion. All core behaviors have been defined with reasonable defaults based on the detailed requirements provided.
