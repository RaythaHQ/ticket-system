# Feature Specification: Scheduler System with "Can Manage Scheduler System" Permission

**Feature Branch**: `008-scheduler-system`  
**Created**: 2026-02-15  
**Status**: Draft  
**Input**: User description: "New permission items: Can Manage Scheduler System — admin backend for managing scheduler staff, configuration, and reports; staff-facing scheduler area at /staff/scheduler sharing ticket layouts; scheduled items linked to Contacts; email notification templates with migration; timezone-aware; foundation for future public UI and text messages."

## Clarifications

### Session 2026-02-15

- Q: Do individual staff members have their own availability, or do all staff share org-wide hours? → A: Per-staff availability — each staff member sets their own available hours within the org-wide scheduling boundary.
- Q: What are the valid appointment status transitions? → A: Linear with terminal exits — Scheduled → Confirmed → In Progress → Completed, with Cancelled and No-Show reachable from any active status but terminal (no reopening).
- Q: Should cancellation policy enforcement be a hard block or soft warning? → A: Soft warning with override — staff see a warning when cancelling within the notice period but can proceed by providing a reason.
- Q: Should appointments have a user-visible identifier code? → A: Yes — auto-generated readable code (e.g., "APT-0001") visible in UI and emails.
- Q: Canonical terminology — "Scheduled Item" or "Appointment"? → A: "Appointment" is the canonical term. More intuitive and patient-friendly for UI and emails.
- Q: Virtual vs in-person appointment modes? → A: Yes — appointments have a mode (Virtual or In-Person). Virtual appointments have a meeting link field. In-person appointments use zipcode-based coverage zones.
- Q: Coverage zones for in-person appointments? → A: Yes — org-wide default coverage zones plus per-staff coverage zone overrides, based on zipcode.
- Q: Appointment type-specific settings and staff eligibility? → A: Yes — each appointment type has its own settings (duration, buffer, mode, eligible staff). Only eligible staff appear when scheduling a given type.
- Q: Should scheduler use the system-wide email templates or its own? → A: Scheduler owns its own email templates within the scheduler admin section, separate from the system-wide email template feature. Templates support scheduler-specific merge variables including meeting link.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Admin Configures and Manages Scheduler Staff (Priority: P1)

An organization administrator with the "Can Manage Scheduler System" permission opens the admin backend and sees a new "Scheduler" navigation item in the left sidebar. From this section, the admin can add active admins as scheduler staff members (selecting from a dropdown of active admins, similar to how Ticket Teams work today). The admin can also flag individual staff members as having the ability to manage other people's calendars (a "secretary" capability, meaning they can create, edit, and cancel appointments on behalf of other staff members). Each staff member can have coverage zones (zipcodes) assigned to them for in-person appointments, which override the org-wide default coverage zones. The admin can remove staff members from the scheduler and toggle flags at any time.

**Why this priority**: Without staff being configured in the system, no scheduling can take place. This is the foundational setup step for the entire scheduler system.

**Independent Test**: Can be fully tested by logging in as an admin with the "Can Manage Scheduler System" permission, navigating to the Scheduler admin section, adding a staff member from the dropdown, toggling their calendar-management flag, setting coverage zones, and confirming changes persist after page reload.

**Acceptance Scenarios**:

1. **Given** an admin with "Can Manage Scheduler System" permission is logged in, **When** they navigate to the admin area, **Then** they see a "Scheduler" item in the left sidebar navigation.
2. **Given** the admin is on the Scheduler staff management page, **When** they open the "Add Staff" dropdown, **Then** they see only active admins listed.
3. **Given** the admin selects an active admin from the dropdown, **When** they confirm the addition, **Then** that person appears in the scheduler staff list.
4. **Given** a staff member exists in the list, **When** the admin toggles "Can Manage Others' Calendars" on, **Then** the flag is saved and the staff member gains the ability to manage other staff calendars.
5. **Given** a staff member exists in the list, **When** the admin removes them, **Then** the person is removed from the scheduler staff and loses access to `/staff/scheduler`.
6. **Given** an admin without the "Can Manage Scheduler System" permission is logged in, **When** they navigate to the admin area, **Then** they do not see the "Scheduler" navigation item.
7. **Given** the admin is editing a staff member's profile, **When** they add coverage zone zipcodes, **Then** those zipcodes override the org-wide default for that staff member's in-person appointments.
8. **Given** a staff member has no custom coverage zones set, **When** the system checks their in-person eligibility, **Then** it falls back to the org-wide default coverage zones.

---

### User Story 2 - Admin Configures Scheduler Settings and Appointment Types (Priority: P1)

The admin navigates to the Scheduler configuration section within the admin backend. This section is divided into two major areas: **General Settings** and **Appointment Types**.

**General Settings** include: available scheduling hours (mapped to the organization's timezone), default buffer time between appointments, cancellation and rescheduling rules (e.g., minimum notice period), how far in advance appointments can be booked (booking horizon), reminder lead times, and default coverage zones (a set of zipcodes defining the org-wide service area for in-person appointments).

**Appointment Types** are the heart of the scheduler configuration. Each appointment type (e.g., "Initial Consultation," "Follow-Up," "Home Visit") has its own type-specific settings:
- **Name** and **active/inactive status**
- **Mode**: Virtual, In-Person, or Either (determines whether a meeting link or location is required)
- **Default duration** (overrides the org-wide default for this type)
- **Buffer time** (overrides the org-wide default for this type; optional — falls back to org default if not set)
- **Eligible staff**: Which scheduler staff members can be assigned appointments of this type. Only eligible staff appear in the staff selection when creating an appointment of this type.
- **Booking horizon override** (optional — falls back to org default if not set)

The admin also manages **Scheduler Email Templates** from within the scheduler admin section (not the system-wide email templates). Three template types exist: Confirmation, Reminder, and Post-Meeting. Each template supports scheduler-specific merge variables including appointment code, meeting link, appointment type, date/time, staff name, contact name, and more. All time-related settings respect and display in the organization's configured timezone.

**Why this priority**: Configuration options establish the rules and constraints that govern how scheduling operates. Without appointment types, eligible staff assignments, and templates, the system has no structure.

**Independent Test**: Can be fully tested by accessing the Scheduler config page, creating an appointment type with specific settings and eligible staff, setting org-wide defaults and coverage zones, customizing email templates, and confirming all values persist and display correctly.

**Acceptance Scenarios**:

1. **Given** an admin is on the Scheduler configuration page, **When** they set available scheduling hours (e.g., 9:00 AM–5:00 PM), **Then** those hours are saved and displayed in the organization's timezone.
2. **Given** an admin creates a new appointment type with mode "Virtual," **When** they save it, **Then** it becomes available when creating appointments, and appointments of this type require a meeting link.
3. **Given** an admin creates a new appointment type with mode "In-Person," **When** they save it, **Then** appointments of this type use coverage zone validation based on the contact's zipcode.
4. **Given** an admin sets a default duration of 60 minutes on an appointment type, **When** a staff member creates an appointment of that type, **Then** the duration defaults to 60 minutes (overriding any org-wide default).
5. **Given** an admin assigns 3 staff members as eligible for a "Home Visit" type, **When** a staff member creates a "Home Visit" appointment, **Then** only those 3 eligible staff members appear in the assignee selection.
6. **Given** an admin sets a buffer time of 15 minutes at the org level but does not set a type-level buffer, **When** viewing the schedule, **Then** the org-level 15-minute buffer applies between consecutive appointments of that type.
7. **Given** an admin sets a minimum cancellation notice of 24 hours, **When** a staff member attempts to cancel an appointment with less than 24 hours' notice, **Then** the system displays a warning but allows the staff member to proceed by providing a reason, which is recorded in the audit history.
8. **Given** an admin sets default coverage zones as zipcodes "10001, 10002, 10003," **When** a staff member creates an in-person appointment for a contact in zipcode 10001, **Then** the scheduling is allowed.
9. **Given** an admin navigates to the Scheduler Email Templates section, **When** they edit the Confirmation template, **Then** they can customize the subject, body, and insert merge variables including `{{AppointmentCode}}`, `{{MeetingLink}}`, `{{AppointmentType}}`, `{{DateTime}}`, `{{StaffName}}`, and `{{ContactName}}`.
10. **Given** an admin views the Scheduler Email Templates, **When** they see all three templates (Confirmation, Reminder, Post-Meeting), **Then** each template is independent from the system-wide email templates and is managed entirely within the scheduler admin section.

---

### User Story 3 - Staff Member Accesses Scheduler Area (Priority: P1)

A user who has been added as a scheduler staff member navigates to `/staff/scheduler`. The area uses the same page styling and layout as the existing ticket system (shared layouts). The left sidebar shows scheduler-relevant navigation (e.g., "My Schedule," "All Appointments," scheduler-specific views) instead of Ticket and Task views. Below the scheduler views, the sidebar still displays "Contacts" and "Activity Log." At the top of the page, a navigation element shows "Tickets | Scheduler" — "Scheduler" is only visible to users with scheduler access, and the currently active section is visually highlighted. Staff can toggle between the ticket system and the scheduler using this top navigation.

**Why this priority**: This is the core workspace for scheduler staff — the entry point to all scheduling functionality. Without this, staff have nowhere to access and manage appointments.

**Independent Test**: Can be fully tested by logging in as a user who has been added as scheduler staff, navigating to `/staff/scheduler`, verifying the layout matches the ticket system styling, confirming the sidebar shows scheduler-specific navigation plus Contacts and Activity Log, and verifying the top "Tickets | Scheduler" toggle works and highlights the active section.

**Acceptance Scenarios**:

1. **Given** a user who is a scheduler staff member, **When** they navigate to `/staff/scheduler`, **Then** they see the scheduler area with the same layout and styling as the ticket system.
2. **Given** a scheduler staff member is on the scheduler area, **When** they look at the left sidebar, **Then** they see scheduler-specific navigation items (not Ticket/Task views), with "Contacts" and "Activity Log" still visible below.
3. **Given** a scheduler staff member is on the scheduler area, **When** they look at the top navigation, **Then** they see "Tickets | Scheduler" with "Scheduler" highlighted as the active section.
4. **Given** a staff user who is NOT a scheduler staff member, **When** they look at the top navigation, **Then** they see only "Tickets" (no "Scheduler" option).
5. **Given** a scheduler staff member is viewing the scheduler, **When** they click "Tickets" in the top navigation, **Then** they are taken to the ticket system and "Tickets" becomes highlighted.
6. **Given** a scheduler staff member is viewing tickets, **When** they click "Scheduler" in the top navigation, **Then** they are taken to `/staff/scheduler` and "Scheduler" becomes highlighted.

---

### User Story 4 - Staff Member Creates and Manages Appointments (Priority: P1)

A scheduler staff member can create a new appointment from the scheduler area. Every appointment must be associated with an existing Contact record (patient). The staff member selects a contact, chooses an appointment type, and the system adapts based on the type's configuration:

- **If the type's mode is Virtual**: A meeting link field is displayed. The staff member enters (or the system generates) a URL for the virtual meeting. This link is stored on the appointment and available as a merge variable in email templates.
- **If the type's mode is In-Person**: The system validates the contact's zipcode against the assigned staff member's coverage zones (or the org-wide default if the staff member has no custom zones). A warning is shown if the contact is outside the coverage area (soft block — staff can proceed with a reason).
- **If the type's mode is Either**: The staff member selects Virtual or In-Person when creating the appointment, and the corresponding fields appear.

When selecting a type, only staff members who are eligible for that appointment type appear in the assignee dropdown. The staff member picks a date and time (displayed in the organization's timezone), sets the duration (defaulting from the type's configured duration), and optionally adds notes. Staff can view, edit, reschedule, and cancel existing appointments. Each appointment has a lifecycle status: Scheduled, Confirmed, In Progress, Completed, Cancelled, No-Show. Staff members with the "Can Manage Others' Calendars" flag can create, edit, and manage appointments on behalf of other eligible staff members.

**Why this priority**: This is the core transactional functionality — without the ability to create and manage appointments, the scheduler provides no value.

**Independent Test**: Can be fully tested by creating virtual and in-person appointments for Contacts, verifying meeting links appear for virtual appointments, verifying coverage zone validation for in-person appointments, confirming only eligible staff appear per type, and verifying all times display in the organization's timezone.

**Acceptance Scenarios**:

1. **Given** a staff member is on the scheduler area, **When** they initiate creating a new appointment, **Then** they must select an existing Contact before proceeding.
2. **Given** a staff member is creating an appointment, **When** they search for a Contact, **Then** they can find and select from existing Contact records.
3. **Given** a staff member selects an appointment type, **When** they view the assignee dropdown, **Then** only staff members who are eligible for that type are listed.
4. **Given** a staff member selects a Virtual appointment type, **When** the form renders, **Then** a meeting link field is displayed and required.
5. **Given** a staff member selects an In-Person appointment type and the contact's zipcode is within the assigned staff member's coverage zone, **When** they save, **Then** the appointment is created successfully.
6. **Given** a staff member selects an In-Person appointment type and the contact's zipcode is outside the assigned staff member's coverage zone, **When** they attempt to save, **Then** the system displays a coverage zone warning but allows the staff member to proceed by providing a reason.
7. **Given** a staff member selects an "Either" mode appointment type, **When** the form renders, **Then** they can choose Virtual or In-Person, and the appropriate fields (meeting link or coverage validation) appear.
8. **Given** a staff member fills out appointment details (Contact, type, mode, date/time, duration), **When** they save, **Then** the appointment is created with status "Scheduled" and all times are stored respecting the organization's timezone.
9. **Given** an appointment exists, **When** a staff member views it, **Then** all times are displayed in the organization's timezone, and the meeting link is visible for virtual appointments.
10. **Given** a staff member has "Can Manage Others' Calendars" enabled, **When** they create an appointment, **Then** they can assign it to another eligible scheduler staff member.
11. **Given** a staff member does NOT have "Can Manage Others' Calendars" enabled, **When** they create an appointment, **Then** it is assigned to themselves and they cannot assign it to others.
12. **Given** an appointment exists with status "Scheduled," **When** a staff member changes its status to "Cancelled," **Then** the status is updated and a cancellation record is kept.
13. **Given** an appointment exists, **When** a staff member reschedules it, **Then** the original time is preserved in history and the new time is set.

---

### User Story 5 - Email Notifications for Scheduling Events (Priority: P2)

The scheduler system manages its own email templates, separate from the system-wide email template feature. This ensures scheduler notifications can evolve independently and include scheduler-specific merge variables without coupling to the broader template system.

Three email template types are required: (1) a **Confirmation** email sent to both the contact/patient and the assigned staff member when an appointment is created, (2) a **Reminder** email sent to both parties ahead of the appointment, and (3) a **Post-Meeting** email sent after the appointment is completed. Each template supports scheduler-specific merge variables, including: `{{AppointmentCode}}`, `{{MeetingLink}}` (for virtual appointments), `{{AppointmentType}}`, `{{AppointmentMode}}` (Virtual/In-Person), `{{DateTime}}`, `{{Duration}}`, `{{StaffName}}`, `{{StaffEmail}}`, `{{ContactName}}`, `{{ContactEmail}}`, `{{ContactZipcode}}`, and `{{Notes}}`. For virtual appointments, the meeting link is prominently included in the email body. For in-person appointments, the meeting link variable is omitted or renders as empty.

Templates are managed from the Scheduler admin section (under "Email Templates"), not from the system-wide email templates area. A data migration seeds default templates for all existing live organizations. The migration does not touch system-wide email templates.

**Why this priority**: Notifications are essential for a functional scheduling system — confirmations reduce no-shows, reminders ensure attendance, and post-meeting emails support follow-up workflows. This is P2 because the core scheduling mechanics must exist first.

**Independent Test**: Can be fully tested by creating a virtual appointment and verifying the confirmation email includes the meeting link, creating an in-person appointment and verifying the link is absent, advancing time to trigger a reminder, completing the appointment, and verifying the post-meeting email — all using the scheduler's own template system.

**Acceptance Scenarios**:

1. **Given** a new virtual appointment is created, **When** it is saved, **Then** a confirmation email is sent to both parties using the scheduler's Confirmation template, and the email body includes the meeting link.
2. **Given** a new in-person appointment is created, **When** it is saved, **Then** a confirmation email is sent to both parties, and the `{{MeetingLink}}` variable renders as empty (no broken link in the email).
3. **Given** an appointment is approaching (per the configured reminder lead time), **When** the reminder trigger fires, **Then** reminder emails are sent to both the contact and the staff member using the scheduler's Reminder template.
4. **Given** an appointment is marked as "Completed," **When** the status changes, **Then** a post-meeting email is sent to both the contact and the staff member using the scheduler's Post-Meeting template.
5. **Given** the system is already live with existing data, **When** the migration runs, **Then** the three default scheduler email templates are seeded without affecting any system-wide email templates.
6. **Given** an admin opens the Scheduler admin section and navigates to Email Templates, **When** they edit the Confirmation template, **Then** they can customize the subject and body using scheduler-specific merge variables.
7. **Given** the system has both email and a placeholder for text message notifications, **When** a scheduling event triggers notifications, **Then** the system dispatches via the email channel and the text message channel is stubbed for future activation without requiring architectural changes.

---

### User Story 6 - Contact Detail Page Shows Scheduling History (Priority: P2)

The existing Contact detail page, which currently shows Tickets, now also shows a "Schedulings" section. This section displays a list of all appointments associated with that Contact. For users who are not scheduler staff, the list is read-only with no ability to take action. For users who are scheduler staff, clicking an appointment opens its full detail view.

**Why this priority**: Connecting scheduling data to the Contact record provides a complete picture of a patient's interactions. This is P2 because it depends on appointments existing first.

**Independent Test**: Can be fully tested by navigating to a Contact's detail page, verifying the Schedulings section is visible, confirming a non-scheduler user sees only a read-only list, and confirming a scheduler staff user can click through to appointment details.

**Acceptance Scenarios**:

1. **Given** a user views a Contact's detail page, **When** the Contact has appointments, **Then** a "Schedulings" section is visible showing a list of those appointments.
2. **Given** a user views a Contact's detail page, **When** the Contact has no appointments, **Then** the "Schedulings" section shows an empty state message.
3. **Given** a non-scheduler-staff user views the Schedulings section, **When** they see the list, **Then** they can only view basic appointment information (date, time, type, status) and cannot click into details or take any actions.
4. **Given** a scheduler staff user views the Schedulings section, **When** they click on an appointment, **Then** they are taken to the full detail view of that appointment.

---

### User Story 7 - Admin Views Scheduling Reports (Priority: P3)

An admin with "Can Manage Scheduler System" permission accesses the Reports section within the Scheduler admin area. They can view metrics about past and active appointments, including: total appointments by status (scheduled, completed, cancelled, no-show), appointment volume over time, staff utilization (appointments per staff member), no-show and cancellation rates, and average appointment duration. Reports respect the organization's timezone for all date/time displays.

**Why this priority**: Reports provide operational insights and optimization data. This is P3 because the system must accumulate scheduling data first before reports are meaningful.

**Independent Test**: Can be fully tested by accessing the Scheduler admin Reports section, verifying each report renders correctly with existing scheduling data, confirming date/time displays use the organization's timezone, and verifying that report filters work as expected.

**Acceptance Scenarios**:

1. **Given** an admin navigates to the Scheduler Reports section, **When** the page loads, **Then** they see a dashboard of scheduling metrics.
2. **Given** appointments exist in the system, **When** the admin views the "Appointments by Status" report, **Then** they see accurate counts for each status category.
3. **Given** the admin views a time-based report, **When** dates and times are displayed, **Then** all values are in the organization's timezone.
4. **Given** the admin views the "Staff Utilization" report, **When** they review it, **Then** they can see appointment counts and utilization rates per staff member.
5. **Given** the admin views the "No-Show & Cancellation Rate" report, **When** they review it, **Then** they see accurate percentages based on historical data.

---

### Edge Cases

- What happens when a staff member is removed from the scheduler but has future appointments assigned to them? The system must prevent removal if unresolved appointments exist, or require reassignment first.
- What happens when a Contact is deleted or deactivated but has future appointments? The system must warn the user and handle associated appointments (cancel or reassign).
- What happens when a staff member with "Can Manage Others' Calendars" is demoted (flag removed) while they have pending edits to others' calendars? Changes already saved persist; the flag removal only affects future actions.
- What happens when the organization's timezone is changed after appointments have been created? Existing appointments must retain their absolute time (the display adjusts, but the appointment occurs at the same real-world moment).
- What happens when two staff members attempt to book the same time slot for the same staff calendar? The system must prevent double-booking for a given staff member's calendar and display a conflict warning.
- What happens when an appointment's Contact has no email address on file? The system should still create the appointment but skip email notification for the contact and log a warning.
- What happens when the email template migration runs on a system that has already been migrated (idempotency)? The migration must be idempotent — running it again should not create duplicate templates.
- What happens if a staff member tries to access `/staff/scheduler` after being removed from the scheduler staff list? They should be denied access and redirected, with "Scheduler" no longer visible in their top navigation.
- What happens when a virtual appointment is created but the staff member forgets to enter a meeting link? The meeting link field is required for virtual appointments — the system must block saving without it.
- What happens when a contact has no zipcode on file and an in-person appointment is being created? The system should warn that coverage zone validation cannot be performed and allow the staff member to proceed.
- What happens when a staff member is removed from an appointment type's eligible list but has future appointments of that type? Existing appointments are unaffected; the staff member simply cannot be assigned new appointments of that type.
- What happens when all eligible staff for an appointment type are fully booked? The system should display a "no available slots" message rather than showing an empty calendar.
- What happens when the org-wide default coverage zones are changed after appointments have been created? Existing in-person appointments are unaffected; the new zones apply only to future scheduling.
- What happens when an appointment type's mode is changed (e.g., from Virtual to In-Person) after appointments of that type already exist? Existing appointments retain their original mode; the change applies only to new appointments.

## Requirements *(mandatory)*

### Functional Requirements

**Permissions & Access Control**

- **FR-001**: System MUST provide a new permission item called "Can Manage Scheduler System" that grants access to the Scheduler admin section in the backend.
- **FR-002**: System MUST show a "Scheduler" navigation item in the admin left sidebar only to users who hold the "Can Manage Scheduler System" permission.
- **FR-003**: System MUST restrict access to `/staff/scheduler` to only users who have been added as scheduler staff members.
- **FR-004**: System MUST display "Tickets | Scheduler" in the top navigation for users who are scheduler staff, showing only "Tickets" for users who are not.
- **FR-005**: System MUST visually highlight the currently active section (Tickets or Scheduler) in the top navigation.

**Scheduler Staff Management**

- **FR-006**: System MUST allow admins with the "Can Manage Scheduler System" permission to add active admins as scheduler staff members via a dropdown selection (similar to Ticket Teams workflow).
- **FR-007**: System MUST only list active admins in the scheduler staff dropdown — inactive or non-admin users must not appear.
- **FR-008**: System MUST allow admins to flag a scheduler staff member with "Can Manage Others' Calendars" capability, allowing them to create, edit, and cancel appointments on behalf of other staff members.
- **FR-008a**: Per-staff coverage zone overrides — see FR-051 under "Coverage Zones" for full requirements.
- **FR-009**: System MUST allow admins to remove a staff member from the scheduler, revoking their access to `/staff/scheduler`.
- **FR-010**: System MUST prevent removal of a scheduler staff member who has future unresolved appointments unless those appointments are first reassigned or cancelled.
- **FR-010a**: When an admin user who is a scheduler staff member is deactivated (loses active admin status), the system MUST automatically set their `SchedulerStaffMember.IsActive` to `false`, which removes their access to `/staff/scheduler` and prevents new appointment assignments to them. Existing future appointments are NOT automatically cancelled — an admin must manually reassign or cancel them.

**Scheduler Configuration (General Settings)**

- **FR-011**: System MUST provide a configuration section in the Scheduler admin area for organization-wide scheduling settings.
- **FR-012**: System MUST allow configuration of org-wide available scheduling hours (the outer boundary), displayed and stored in the organization's timezone.
- **FR-012a**: System MUST allow each scheduler staff member to configure their own personal availability (days and hours) within the org-wide scheduling boundary. A staff member's bookable slots are the intersection of their personal availability and the org-wide hours.
- **FR-012b**: System MUST default a new staff member's personal availability to the org-wide hours until they customize it.
- **FR-013**: System MUST allow configuration of a default appointment duration (used as fallback when an appointment type does not specify its own duration).
- **FR-014**: System MUST allow configuration of a default buffer time between appointments (used as fallback when an appointment type does not specify its own buffer).
- **FR-016**: System MUST allow configuration of cancellation and rescheduling rules, including minimum notice periods. When a staff member attempts to cancel or reschedule within the minimum notice period, the system MUST display a soft warning but allow the staff member to proceed by providing a reason. The override reason is recorded in the scheduling audit history.
- **FR-017**: System MUST allow configuration of a default booking horizon — how far in advance appointments can be booked (used as fallback when an appointment type does not specify its own).
- **FR-018**: System MUST allow configuration of reminder lead times (how far before an appointment reminders are sent).

**Coverage Zones**

- **FR-050**: System MUST allow configuration of org-wide default coverage zones as a set of zipcodes defining the service area for in-person appointments.
- **FR-051**: System MUST allow per-staff coverage zone overrides — a staff member can have their own set of zipcodes that replace the org-wide default for them.
- **FR-052**: When scheduling an in-person appointment, the system MUST validate the contact's zipcode against the assigned staff member's coverage zones (or the org-wide default if the staff member has no custom zones).
- **FR-053**: If the contact's zipcode is outside the coverage zone, the system MUST display a soft warning but allow the staff member to proceed by providing a reason. The override reason is recorded in the scheduling audit history.

**Appointment Types**

- **FR-015**: System MUST allow admins to define and manage appointment types with the following type-specific settings: name, active/inactive status, mode (Virtual / In-Person / Either), default duration, buffer time (optional override), booking horizon (optional override), and eligible staff members.
- **FR-015a**: Each appointment type MUST have a mode setting: "Virtual" (meeting link required), "In-Person" (coverage zone validation applies), or "Either" (staff chooses per appointment).
- **FR-015b**: Each appointment type MUST have an eligible staff list — only staff members on this list appear as assignees when creating an appointment of this type.
- **FR-015c**: When a type-specific setting (duration, buffer, booking horizon) is not configured, the system MUST fall back to the corresponding org-wide default.
- **FR-015d**: System MUST allow a staff member to be eligible for multiple appointment types.

**Staff Scheduler Area**

- **FR-019**: System MUST serve the scheduler staff area at `/staff/scheduler`, sharing the same page layout and styling as the existing ticket system.
- **FR-020**: System MUST display scheduler-specific navigation in the left sidebar (e.g., "My Schedule," "All Appointments") instead of Ticket and Task views when in the scheduler area.
- **FR-021**: System MUST continue to display "Contacts" and "Activity Log" in the left sidebar below the scheduler-specific navigation.
- **FR-022**: System MUST allow staff to toggle between Tickets and Scheduler via the top navigation without losing context in either area.

**Appointments**

- **FR-023**: System MUST require every appointment to be associated with an existing Contact record (ContactID). Appointments cannot exist without a linked Contact.
- **FR-023a**: System MUST auto-generate a unique, human-readable appointment code for each appointment (e.g., "APT-0001"). This code is displayed in the UI, included in email notifications, and serves as the primary reference identifier for staff and contacts.
- **FR-024**: System MUST allow staff to search for and select a Contact when creating an appointment.
- **FR-024a**: Each appointment MUST have a mode: Virtual or In-Person. This is determined by the appointment type's mode setting (or chosen by staff if the type's mode is "Either").
- **FR-024b**: For Virtual appointments, the system MUST provide a meeting link field where staff can enter (or paste) a URL for the virtual meeting. This field is required for virtual appointments.
- **FR-024c**: The meeting link MUST be stored on the appointment record and available as a merge variable (`{{MeetingLink}}`) in scheduler email templates.
- **FR-024d**: For In-Person appointments, the system MUST validate the contact's zipcode against the assigned staff member's coverage zones per FR-052 and FR-053.
- **FR-024e**: When creating an appointment, the staff assignee dropdown MUST only show staff members who are eligible for the selected appointment type (per FR-015b).
- **FR-025**: System MUST support the following appointment statuses: Scheduled, Confirmed, In Progress, Completed, Cancelled, No-Show. Valid transitions follow a linear progression: Scheduled → Confirmed → In Progress → Completed. Cancelled and No-Show are terminal statuses reachable from any active status (Scheduled, Confirmed, or In Progress) but cannot be reopened. Skipping forward (e.g., Scheduled → In Progress) is not permitted; backward transitions (e.g., Completed → Scheduled) are not permitted.
- **FR-026**: System MUST store and display all appointment times in the organization's configured timezone.
- **FR-027**: System MUST allow staff to create, view, edit, reschedule, and cancel appointments.
- **FR-028**: System MUST preserve rescheduling history (original date/time) when an appointment is rescheduled.
- **FR-029**: System MUST prevent double-booking for a given staff member's calendar (no overlapping appointments for the same staff member).
- **FR-030**: Staff members with "Can Manage Others' Calendars" MUST be able to create and manage appointments on other eligible staff members' calendars.
- **FR-031**: Staff members without "Can Manage Others' Calendars" MUST only be able to create and manage appointments on their own calendar.

**Contact Integration**

- **FR-032**: System MUST display a "Schedulings" section on the Contact detail page showing all appointments for that Contact.
- **FR-033**: System MUST show the Schedulings section as a read-only list for non-scheduler-staff users (no action buttons, no click-through to detail).
- **FR-034**: System MUST allow scheduler staff users to click on an appointment in the Contact's Schedulings list to navigate to the full appointment detail view.
- **FR-034a**: When a Contact has future active appointments (status: Scheduled, Confirmed, or In Progress), the system MUST prevent deletion of that Contact and display a warning listing the affected appointments. The Contact can only be deleted after all associated future appointments are cancelled, completed, or reassigned.

**Scheduler Email Templates (Scheduler-Owned)**

- **FR-035**: The scheduler system MUST manage its own email templates, separate from the system-wide email template feature. Templates are created, edited, and stored within the scheduler admin section.
- **FR-035a**: System MUST provide three email template types: Confirmation, Reminder, and Post-Meeting. Each template has a subject line and body that support merge variables.
- **FR-035b**: Scheduler email templates MUST support the following merge variables at minimum: `{{AppointmentCode}}`, `{{MeetingLink}}`, `{{AppointmentType}}`, `{{AppointmentMode}}`, `{{DateTime}}`, `{{Duration}}`, `{{StaffName}}`, `{{StaffEmail}}`, `{{ContactName}}`, `{{ContactEmail}}`, `{{ContactZipcode}}`, and `{{Notes}}`.
- **FR-035c**: For virtual appointments, `{{MeetingLink}}` MUST render the meeting URL. For in-person appointments, `{{MeetingLink}}` MUST render as empty (not as a broken or placeholder link).
- **FR-036**: System MUST send a confirmation email to both the contact and the assigned staff member when an appointment is created.
- **FR-037**: System MUST send a reminder email to both the contact and the assigned staff member at the configured reminder lead time before the appointment.
- **FR-038**: System MUST send a post-meeting email to both the contact and the assigned staff member when an appointment is marked as "Completed."
- **FR-039**: System MUST include a data migration that seeds default scheduler email templates for all existing live organizations (not only during initial setup). This migration does not affect system-wide email templates.
- **FR-040**: The email template migration MUST be idempotent — running it multiple times must not create duplicate templates.

**Notification Foundation for Text Messages**

- **FR-041**: System MUST design the notification dispatch mechanism to support multiple channels (email and text message at minimum) even though text messages are not active in v1.
- **FR-042**: System MUST define equivalent text message templates alongside email templates so that activating text messages in a future version requires only enabling the channel, not restructuring the notification system.

**Reports**

- **FR-043**: System MUST provide a Reports section within the Scheduler admin area accessible to admins with "Can Manage Scheduler System" permission.
- **FR-044**: System MUST display reports for: total appointments by status, appointment volume over time, staff utilization per staff member, no-show and cancellation rates, and average appointment duration.
- **FR-045**: All report date/time displays MUST use the organization's configured timezone.

**Future Foundation**

- **FR-046**: System MUST architect the scheduling data model and business logic to support a future public-facing UI (`/public/scheduler`) without requiring structural changes — even though v1 does not include the public UI.

### Key Entities

- **Scheduler Staff Member**: Represents an active admin who has been added to the scheduler system. Key attributes: linked user/admin identity, "Can Manage Others' Calendars" flag, active/inactive status, date added, personal availability schedule (days and hours, must fall within org-wide boundary), coverage zones (set of zipcodes, optional — falls back to org default), eligible appointment types (many-to-many relationship).
- **Appointment** (formerly referred to as "Scheduled Item"): A time-bound event linking a staff member to a Contact. Key attributes: appointment code (auto-generated, human-readable, e.g., "APT-0001"), associated Contact (ContactID), assigned staff member, appointment type, mode (Virtual / In-Person), meeting link (for virtual appointments), date/time (in org timezone), duration, status (Scheduled → Confirmed → In Progress → Completed; Cancelled and No-Show are terminal from any active status), notes, created-by user, rescheduling history.
- **Appointment Type**: A configurable category of appointment with type-specific settings. Key attributes: name, active/inactive status, mode (Virtual / In-Person / Either), default duration, buffer time (optional override), booking horizon (optional override), eligible staff members (many-to-many). Examples: "Initial Consultation" (Virtual), "Home Visit" (In-Person), "Follow-Up" (Either).
- **Coverage Zone**: A set of zipcodes defining a service area for in-person appointments. Exists at two levels: org-wide default and per-staff override. When a staff member has no custom zones, the org-wide default applies.
- **Scheduler Configuration**: Organization-wide settings governing scheduling behavior. Key attributes: available hours (start/end per day of week), default appointment duration, default buffer time, default booking horizon, minimum cancellation notice period, reminder lead time(s), default coverage zones (zipcodes).
- **Scheduler Email Template**: A message template owned by the scheduler system (separate from system-wide email templates). Key attributes: template type (Confirmation / Reminder / Post-Meeting), subject, body with merge variables (including `{{MeetingLink}}`), active status. Text message equivalents are defined alongside but inactive in v1.
- **Scheduling History / Audit Entry**: A record of changes to an appointment. Key attributes: appointment reference, change type (created, rescheduled, cancelled, status change, coverage zone override), old value, new value, changed-by user, timestamp, override reason (if applicable).

## Assumptions

- **Appointment model is 1:1 in v1**: Each appointment involves one staff member and one Contact (patient). Group appointments are out of scope for v1.
- **Staff-created appointments only in v1**: Since there is no public UI in v1, all appointments are created by scheduler staff on behalf of contacts. The data model should accommodate self-service booking in the future.
- **Active admins only**: Only users with an active admin role can be added as scheduler staff. If an admin is deactivated, they should be flagged/handled in the scheduler staff list.
- **Organization timezone is already configured**: The system assumes the organization has a timezone set in its existing configuration. The scheduler uses this value and does not introduce its own timezone setting.
- **Scheduler-owned email templates**: The scheduler manages its own email templates, entirely separate from the system-wide email template feature. This allows scheduler templates to include scheduler-specific merge variables (e.g., `{{MeetingLink}}`) and evolve independently.
- **Meeting links are user-provided in v1**: Staff manually enter/paste meeting URLs (e.g., Zoom, Teams links). Auto-generation of meeting links via third-party API integration is out of scope for v1.
- **Coverage zones are zipcode-based**: Coverage zone matching uses exact zipcode comparison (contact's zipcode must be in the staff member's or org's zone list). Radius-based or geographic proximity matching is out of scope for v1.
- **Contacts have an optional zipcode field**: The system assumes the Contact record has (or can have) a zipcode field. If a contact has no zipcode, coverage zone validation is skipped with a warning.
- **No REST API endpoints in v1**: While the data model supports future API exposure, no new REST API endpoints are built for the scheduler in v1.
- **No third-party integrations in v1**: Calendar sync (Google Calendar, Outlook, etc.), auto-generated meeting links, and other integrations are out of scope.
- **Reminder scheduling**: The system uses a background job or equivalent mechanism to check for upcoming appointments and send reminders. The exact trigger timing is governed by the configured reminder lead time.
- **Text messages are stubbed**: Text message templates are defined and stored but the sending channel is inactive. No SMS provider integration is required for v1.
- **Default appointment statuses**: The six statuses (Scheduled, Confirmed, In Progress, Completed, Cancelled, No-Show) are system-defined and not configurable by the admin in v1.
- **Type-specific settings are optional overrides**: Appointment type settings (duration, buffer, booking horizon) override org-wide defaults only when explicitly set. This keeps configuration lightweight — admins only customize what differs from the defaults.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Admins can configure the scheduler system (staff, settings, appointment types with eligible staff) and have scheduling operational within 15 minutes of enabling the permission.
- **SC-002**: Staff members can create a new appointment (search for contact, select type, pick time, save) in under 2 minutes, with the correct fields appearing based on appointment mode (virtual or in-person).
- **SC-003**: 100% of appointment times displayed in the system match the organization's configured timezone — no timezone mismatches across any screen.
- **SC-004**: Confirmation emails are delivered to both parties within 2 minutes of appointment creation, and virtual appointment emails include a working meeting link.
- **SC-005**: Reminder emails are delivered to both parties within the configured lead time window (no missed reminders for valid appointments).
- **SC-006**: The Contact detail page loads the Schedulings section without adding more than 1 second to the existing page load time.
- **SC-007**: Zero double-bookings occur for any individual staff member's calendar when appointments are created through the system.
- **SC-008**: The email template migration seeds default scheduler templates on existing live organizations without affecting any system-wide email templates or requiring manual intervention.
- **SC-009**: Staff can toggle between Tickets and Scheduler via the top navigation in a single click, with the correct section loading in under 2 seconds.
- **SC-010**: Reports accurately reflect scheduling data — appointment counts by status match actual records with 100% accuracy.
- **SC-011**: When creating an appointment, only staff members eligible for the selected appointment type appear in the assignee dropdown — zero ineligible staff are ever shown.
- **SC-012**: Coverage zone validation correctly warns on 100% of in-person appointments where the contact's zipcode is outside the staff member's zone, while still allowing override with a reason.
- **SC-013**: The `{{MeetingLink}}` merge variable renders the correct URL in virtual appointment emails and renders as empty (not broken) in in-person appointment emails.
