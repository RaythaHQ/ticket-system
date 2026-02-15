# Research: Scheduler System

**Branch**: `008-scheduler-system` | **Date**: 2026-02-15

## R-001: Permission System Extension

**Decision**: Add `ManageSchedulerSystem` to the existing `BuiltInSystemPermission` value object and `SystemPermissions` flags enum.

**Rationale**: The codebase already uses a well-established pattern for permissions via `BuiltInSystemPermission` (in `Role.cs`) with a backing `SystemPermissions` [Flags] enum. The highest existing flag value is `EditWikiArticles = 1024`, so the next flag is `ManageSchedulerSystem = 2048`.

**Implementation**:
1. Add `MANAGE_SCHEDULER_SYSTEM_PERMISSION = "manage_scheduler_system"` constant
2. Add `ManageSchedulerSystem = 2048` to `SystemPermissions` enum
3. Add static property `ManageSchedulerSystem`
4. Add to `From(SystemPermissions)`, `Permissions`, and `AllPermissionsAsEnum`

**Alternatives considered**:
- Separate permissions (e.g., ManageSchedulerConfig + ManageSchedulerStaff): Rejected as over-granular for v1. A single permission controls all admin-side scheduler functions. Can be split in future versions if needed.

## R-002: Contact Zipcode Field

**Decision**: Add a `Zipcode` string field to the `Contact` entity with a database migration.

**Rationale**: The existing `Contact` entity has an `Address` field (free-text) but no structured zipcode. Coverage zone validation requires exact zipcode matching. A dedicated field is queryable, reliable, and supports future features.

**Implementation**:
1. Add `public string? Zipcode { get; set; }` to `Contact.cs`
2. EF migration: `ALTER TABLE "Contacts" ADD "Zipcode" varchar(20) NULL`
3. Add index on `Zipcode` for efficient coverage zone queries
4. Update `ContactDto`, `ContactListItemDto`, `CreateContact.Command`, `UpdateContact.Command`

**Alternatives considered**:
- Parse from Address field: Rejected — unstructured, unreliable, not queryable.
- Separate ContactAddress entity with structured fields: Over-engineered for current need. Can refactor later.

## R-003: Scheduler-Owned Email Templates

**Decision**: Create a new `SchedulerEmailTemplate` entity separate from the existing `EmailTemplate` system, but reuse the same `IRenderEngine` (Fluid/Liquid) for rendering.

**Rationale**: The spec explicitly requires scheduler templates to be managed within the scheduler admin section, separate from system-wide email templates. Scheduler templates need unique merge variables (`{{MeetingLink}}`, `{{AppointmentMode}}`, `{{AppointmentCode}}`) that don't apply to ticket templates. Separation prevents polluting the global template list and allows independent evolution.

**Implementation**:
1. `SchedulerEmailTemplate` entity with: `TemplateType` (confirmation/reminder/post_meeting), `Channel` (email/sms), `Subject`, `Content`, `IsActive`
2. Default templates seeded via migration (with `WHERE NOT EXISTS` idempotency)
3. Rendering: Build a `SchedulerNotification_RenderModel` and use existing `IRenderEngine.RenderAsHtml()` with `Wrapper_RenderModel`
4. Admin UI: CRUD pages under `Admin/Scheduler/EmailTemplates/`
5. Text message templates stored with `Channel = "sms"` but inactive (foundation for future)

**Alternatives considered**:
- Extend `BuiltInEmailTemplate`: Rejected — couples scheduler to system-wide template lifecycle, pollutes global admin UI, requires shared merge variable context.
- External template service: Over-engineered, adds infrastructure complexity.

## R-004: Staff Layout Architecture (Tickets | Scheduler Toggle)

**Decision**: Create `_SchedulerLayout.cshtml` that shares the same outer chrome (topbar, user section, footer scripts) but overrides the sidebar content with scheduler-specific navigation.

**Rationale**: The current `_Layout.cshtml` in `Staff/Pages/Shared/` has the sidebar content (Tickets, Tasks, Views, Contacts, Activity Log) hardcoded. Rather than adding complex conditional logic to one layout, a separate scheduler layout file inherits the same CSS, scripts, and outer structure but replaces the sidebar `<nav>` contents.

**Implementation**:
1. Modify the existing `_Layout.cshtml` topbar (line ~282): Replace hardcoded "Home" with a conditional "Tickets | Scheduler" toggle. "Scheduler" link only visible if `CurrentUser` is a scheduler staff member.
2. Create `_SchedulerLayout.cshtml` that:
   - Uses the same `<head>`, CSS, scripts as the ticket layout
   - Has scheduler sidebar: "My Schedule", "All Appointments" (top), then "Contacts" and "Activity Log" (bottom)
   - Sets `Layout` to a shared base partial (or duplicates outer chrome — prefer extraction to `_StaffBase.cshtml`)
3. Scheduler pages set `Layout = "_SchedulerLayout"` in their `_ViewStart.cshtml`

**Alternatives considered**:
- Single layout with `if/else` blocks: Leads to a bloated, hard-to-maintain layout file. Rejected.
- Fully separate layout (copy-paste): Would drift from ticket layout over time. Rejected in favor of shared base extraction.

**Best approach**: Extract the common outer chrome (head, topbar, user section, scripts) into `_StaffBaseLayout.cshtml`. Then `_Layout.cshtml` (tickets) and `_SchedulerLayout.cshtml` both extend this base with their respective sidebar content. This keeps the toggle in the shared topbar while allowing each area to own its sidebar.

## R-005: Appointment Entity Design

**Decision**: Use `BaseNumericFullAuditableEntity` (long ID) for `Appointment`, consistent with `Ticket` and `Contact`.

**Rationale**: Appointments need human-readable numeric IDs (like tickets: `APT-0001`) for easy reference in UI, emails, and conversations. The existing `INumericIdGenerator` service can be reused for auto-generating appointment IDs (minimum 7 digits).

**Implementation**:
- `Appointment` inherits from `BaseNumericFullAuditableEntity`
- Auto-generated `Code` property formatted as `APT-{Id}` (padded)
- Status stored as string with `AppointmentStatus` value object (following `TicketStatus` pattern)
- Mode stored as string with `AppointmentMode` value object
- Times stored as UTC, displayed via org timezone conversion

**Alternatives considered**:
- GUID ID with separate code field: Inconsistent with Ticket/Contact pattern. Rejected.
- String-based code with UUID: Would break the numeric ID convention. Rejected.

## R-006: Appointment Status State Machine

**Decision**: Implement status transitions as a validated progression: Scheduled → Confirmed → In Progress → Completed, with Cancelled and No-Show as terminal exits from any active state.

**Rationale**: Per spec clarification, skipping forward and backward transitions are not permitted. This prevents data integrity issues (e.g., going directly from Scheduled to Completed without confirmation).

**Implementation**:
1. `AppointmentStatus` value object with transition validation method
2. `ChangeAppointmentStatus` command validator enforces legal transitions
3. Terminal states (Cancelled, No-Show) prevent any further transition
4. Transition matrix:

| From \ To | Scheduled | Confirmed | In Progress | Completed | Cancelled | No-Show |
|-----------|-----------|-----------|-------------|-----------|-----------|---------|
| Scheduled | - | ✅ | ❌ | ❌ | ✅ | ✅ |
| Confirmed | ❌ | - | ✅ | ❌ | ✅ | ✅ |
| In Progress | ❌ | ❌ | - | ✅ | ✅ | ✅ |
| Completed | ❌ | ❌ | ❌ | - | ❌ | ❌ |
| Cancelled | ❌ | ❌ | ❌ | ❌ | - | ❌ |
| No-Show | ❌ | ❌ | ❌ | ❌ | ❌ | - |

## R-007: Background Reminder Job

**Decision**: Implement `AppointmentReminderJob` as a `BackgroundService` following the same pattern as `SlaEvaluationJob` and `SnoozeEvaluationJob`.

**Rationale**: The existing codebase has a well-established pattern for periodic background jobs using `BackgroundService`, scoped services, and batch processing. The reminder job checks for upcoming appointments within the configured reminder lead time and sends notifications.

**Implementation**:
1. Runs every 5 minutes (configurable)
2. Queries appointments where: status is active (Scheduled/Confirmed), `ScheduledStartTime - now <= reminderLeadTime`, and no reminder has been sent yet
3. Add `ReminderSentAt` nullable DateTime to `Appointment` entity to prevent duplicate reminders
4. Processes in batches (100 per batch)
5. Sends emails via `IEmailer` using scheduler email templates
6. Logs all activity via `ILogger`

**Alternatives considered**:
- Hangfire/Quartz scheduled jobs: Adds external dependency. Rejected per constitution (minimize dependencies).
- Queue-based (BackgroundTask table): Over-complex for a simple periodic check. The existing `BackgroundService` pattern is simpler and proven.

## R-008: Availability and Booking Slot Calculation

**Decision**: Bookable slots = intersection of (org-wide hours) AND (staff personal availability) MINUS (existing appointments + buffer times).

**Rationale**: The spec requires per-staff availability within org-wide boundaries. This three-layer model ensures no appointments are booked outside business hours, respects individual staff schedules, and prevents double-booking.

**Implementation**:
1. `IAvailabilityService.GetAvailableSlotsAsync(staffId, date, appointmentTypeId)`:
   - Load org-wide hours for the day of week
   - Load staff personal availability for the day
   - Compute intersection = bookable window
   - Load existing appointments for that staff on that date
   - Subtract appointment blocks (duration + type-specific or org-default buffer)
   - Return remaining slots aligned to type's default duration
2. Availability stored as JSON: `{ "monday": { "start": "09:00", "end": "17:00" }, ... }`

## R-009: Coverage Zone Validation

**Decision**: Exact zipcode matching against a list. Staff coverage zones override org defaults.

**Rationale**: The spec defines coverage zones as zipcode-based with exact matching. This is the simplest model that works for v1. The resolution order: check staff-specific zones first → fall back to org-wide default zones → if contact has no zipcode, skip validation with a warning.

**Implementation**:
1. `ICoverageZoneValidator.ValidateAsync(contactId, staffMemberId)`: Returns `(isValid, reason)`
2. Coverage zones stored as JSON arrays of zipcode strings
3. Soft warning (not hard block) — UI shows warning, staff provides override reason
4. Override reason stored in `AppointmentHistory`

## R-010: Appointment Type Eligible Staff

**Decision**: Many-to-many relationship via `AppointmentTypeStaffEligibility` junction entity.

**Rationale**: Each appointment type has a configurable list of eligible staff members. A staff member can be eligible for multiple types. The junction entity follows the existing `TeamMembership` pattern.

**Implementation**:
1. `AppointmentTypeStaffEligibility`: `AppointmentTypeId` + `SchedulerStaffMemberId`
2. Admin UI: Multi-select checkboxes on appointment type create/edit page
3. Appointment creation form: Staff dropdown filtered by selected type's eligible list
4. Removing a staff member from eligibility does NOT affect existing appointments

## R-011: Scheduler Admin Sidebar Integration

**Decision**: Add "Scheduler" menu item to the existing admin sidebar, gated by `ManageSchedulerSystem` permission.

**Rationale**: The admin sidebar in `_SidebarLayout.cshtml` already conditionally shows menu items based on permissions (e.g., Teams requires `ManageTeams`). The scheduler follows the same pattern.

**Implementation**:
1. Add a new collapsible "Scheduler" section in the admin sidebar
2. Sub-items: Staff, Appointment Types, Configuration, Email Templates, Reports
3. Gated by `AuthorizationService.AuthorizeAsync(User, BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)`
4. Admin RouteNames: Add `RouteNames.Scheduler.*` nested class
