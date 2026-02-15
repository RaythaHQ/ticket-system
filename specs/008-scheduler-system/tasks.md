# Tasks: Scheduler System

**Input**: Design documents from `/specs/008-scheduler-system/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/commands-queries.md, quickstart.md

**Tests**: Not explicitly requested in the feature specification. Test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Domain Layer)

**Purpose**: Create all domain entities, value objects, events, and permission updates that the entire scheduler system depends on.

- [x] T001 [P] Create `AppointmentStatus` value object with status constants (SCHEDULED, CONFIRMED, IN_PROGRESS, COMPLETED, CANCELLED, NO_SHOW), `From()` factory, `CanTransitionTo()` method, `IsTerminal`/`IsActive` properties, and `AppointmentStatusNotFoundException` in `src/App.Domain/ValueObjects/AppointmentStatus.cs`
- [x] T002 [P] Create `AppointmentMode` value object with mode constants (VIRTUAL, IN_PERSON, EITHER), `From()` factory, `RequiresMeetingLink`/`RequiresCoverageValidation` properties in `src/App.Domain/ValueObjects/AppointmentMode.cs`
- [x] T003 [P] Create `SchedulerStaffMember` entity inheriting `BaseAuditableEntity` with UserId (Guid, unique), CanManageOthersCalendars (bool), IsActive (bool), AvailabilityJson (string?), CoverageZonesJson (string?), [NotMapped] helpers for Availability and CoverageZones, and navigation properties in `src/App.Domain/Entities/SchedulerStaffMember.cs`
- [x] T004 [P] Create `AppointmentType` entity inheriting `BaseAuditableEntity` with Name, Mode (string), DefaultDurationMinutes (int?), BufferTimeMinutes (int?), BookingHorizonDays (int?), IsActive (bool), SortOrder (int), and navigation properties in `src/App.Domain/Entities/AppointmentType.cs`
- [x] T005 [P] Create `AppointmentTypeStaffEligibility` junction entity inheriting `BaseAuditableEntity` with AppointmentTypeId and SchedulerStaffMemberId foreign keys in `src/App.Domain/Entities/AppointmentTypeStaffEligibility.cs`
- [x] T006 [P] Create `Appointment` entity inheriting `BaseNumericFullAuditableEntity` with ContactId (long), AssignedStaffMemberId (Guid), AppointmentTypeId (Guid), Mode (string), MeetingLink (string?), ScheduledStartTime (DateTime), DurationMinutes (int), Status (string), Notes, CancellationReason, CoverageZoneOverrideReason, CancellationNoticeOverrideReason, ReminderSentAt (DateTime?), CreatedByStaffId (Guid), [NotMapped] Code/StatusValue/ModeValue properties, and navigation properties in `src/App.Domain/Entities/Appointment.cs`
- [x] T007 [P] Create `AppointmentHistory` entity with Id (Guid), AppointmentId (long), ChangeType (string), OldValue, NewValue, OverrideReason, ChangedByUserId (Guid), Timestamp (DateTime) in `src/App.Domain/Entities/AppointmentHistory.cs`
- [x] T008 [P] Create `SchedulerConfiguration` entity inheriting `BaseAuditableEntity` with AvailableHoursJson, DefaultDurationMinutes, DefaultBufferTimeMinutes, DefaultBookingHorizonDays, MinCancellationNoticeHours, ReminderLeadTimeMinutes, DefaultCoverageZonesJson, and [NotMapped] helpers in `src/App.Domain/Entities/SchedulerConfiguration.cs`
- [x] T009 [P] Create `SchedulerEmailTemplate` entity inheriting `BaseAuditableEntity` with TemplateType (string), Channel (string), Subject (string?), Content (string), IsActive (bool) in `src/App.Domain/Entities/SchedulerEmailTemplate.cs`
- [x] T010 [P] Create domain events: `AppointmentCreatedEvent`, `AppointmentStatusChangedEvent` (with OldStatus/NewStatus), `AppointmentRescheduledEvent` (with OldTime/NewTime), `AppointmentCompletedEvent` — all inheriting `BaseEvent` and implementing `IAfterSaveChangesNotification` in `src/App.Domain/Events/`
- [x] T011 Add `Zipcode` (string?) property to `Contact` entity and add `Appointments` navigation collection (`ICollection<Appointment>`) in `src/App.Domain/Entities/Contact.cs`
- [x] T012 Add `ManageSchedulerSystem` permission: add `MANAGE_SCHEDULER_SYSTEM_PERMISSION = "manage_scheduler_system"` constant, add `ManageSchedulerSystem = 2048` to `SystemPermissions` enum, add static property, update `From(SystemPermissions)`, `Permissions` enumerable, and `AllPermissionsAsEnum` in `src/App.Domain/Entities/Role.cs`

**Checkpoint**: All domain types compiled. No database changes yet.

---

## Phase 2: Foundational (Infrastructure & Shared Services)

**Purpose**: Database migrations, DbContext registration, EF configurations, and shared application-layer interfaces that ALL user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T013 [P] Create `SchedulerStaffMemberConfiguration` with UserId unique index, IsActive index, FK to User in `src/App.Infrastructure/Persistence/Configurations/SchedulerStaffMemberConfiguration.cs`
- [x] T014 [P] Create `AppointmentTypeConfiguration` with Name max length, Mode max length, IsActive and SortOrder indexes in `src/App.Infrastructure/Persistence/Configurations/AppointmentTypeConfiguration.cs`
- [x] T015 [P] Create `AppointmentTypeStaffEligibilityConfiguration` with unique composite index on (AppointmentTypeId, SchedulerStaffMemberId), FKs with cascade delete in `src/App.Infrastructure/Persistence/Configurations/AppointmentTypeStaffEligibilityConfiguration.cs`
- [x] T016 [P] Create `AppointmentConfiguration` with ValueGeneratedNever for Id, max lengths, FKs (Contact, SchedulerStaffMember, AppointmentType, CreatedByStaff), indexes on ContactId, AssignedStaffMemberId, AppointmentTypeId, Status, ScheduledStartTime, CreationTime, partial index for reminder job, soft delete filter in `src/App.Infrastructure/Persistence/Configurations/AppointmentConfiguration.cs`
- [x] T017 [P] Create `AppointmentHistoryConfiguration` with FKs, indexes on AppointmentId and Timestamp in `src/App.Infrastructure/Persistence/Configurations/AppointmentHistoryConfiguration.cs`
- [x] T018 [P] Create `SchedulerConfigurationConfiguration` in `src/App.Infrastructure/Persistence/Configurations/SchedulerConfigurationConfiguration.cs`
- [x] T019 [P] Create `SchedulerEmailTemplateConfiguration` with unique composite index on (TemplateType, Channel) in `src/App.Infrastructure/Persistence/Configurations/SchedulerEmailTemplateConfiguration.cs`
- [x] T020 Add 7 new DbSet properties (SchedulerStaffMembers, AppointmentTypes, AppointmentTypeStaffEligibilities, Appointments, AppointmentHistories, SchedulerConfigurations, SchedulerEmailTemplates) to `src/App.Application/Common/Interfaces/IAppDbContext.cs` and corresponding properties to `src/App.Infrastructure/Persistence/AppDbContext.cs`
- [x] T021 Create EF Core migration for all new scheduler tables and the Contact.Zipcode column: run `dotnet ef migrations add AddSchedulerSystem` in `src/App.Infrastructure/`
- [x] T022 Create idempotent seed migration for 6 default scheduler email templates (3 types × 2 channels: email active, sms inactive) using `migrationBuilder.Sql()` with `WHERE NOT EXISTS` — include Liquid template content with merge variables for each template type in `src/App.Infrastructure/Migrations/`
- [x] T023 [P] Create `ISchedulerPermissionService` interface with `CanManageSchedulerSystem()` and `IsSchedulerStaff()` methods in `src/App.Application/Common/Interfaces/ISchedulerPermissionService.cs`, and implement it in `src/App.Infrastructure/Services/SchedulerPermissionService.cs`
- [x] T024 [P] Create `IAppointmentCodeGenerator` interface in `src/App.Application/Scheduler/Services/IAppointmentCodeGenerator.cs` and implement `AppointmentCodeGenerator` (using `INumericIdGenerator` pattern, generates long IDs, formats as `APT-{Id:D4}`) in `src/App.Infrastructure/Services/AppointmentCodeGenerator.cs`
- [x] T025 [P] Create `ICoverageZoneValidator` interface with `ValidateAsync(long contactId, Guid staffMemberId)` returning `(bool IsValid, string? WarningMessage)` in `src/App.Application/Scheduler/Services/ICoverageZoneValidator.cs` and implement it in `src/App.Infrastructure/Services/CoverageZoneValidator.cs`
- [x] T026 [P] Create `IAvailabilityService` interface with `GetAvailableSlotsAsync(Guid staffMemberId, DateTime date, Guid appointmentTypeId)` in `src/App.Application/Scheduler/Services/IAvailabilityService.cs` and implement slot calculation (org hours ∩ staff hours − existing appointments − buffers) in `src/App.Infrastructure/Services/AvailabilityService.cs`
- [x] T027 [P] Create scheduler DTOs: `SchedulerStaffDto`, `SchedulerStaffListItemDto`, `AppointmentTypeDto`, `AppointmentTypeListItemDto`, `AppointmentDto`, `AppointmentListItemDto`, `SchedulerConfigurationDto`, `SchedulerEmailTemplateDto`, `StaffScheduleDto`, `StaffAvailabilityDto`, `SchedulerReportDto` in `src/App.Application/SchedulerAdmin/DTOs/` and `src/App.Application/Scheduler/DTOs/`
- [x] T028 [P] Create `AppointmentNotification_RenderModel` with all merge variable properties (AppointmentCode, MeetingLink, AppointmentType, AppointmentMode, DateTime, Duration, StaffName, StaffEmail, ContactName, ContactEmail, ContactZipcode, Notes) in `src/App.Application/Scheduler/RenderModels/AppointmentNotification_RenderModel.cs`
- [x] T029 Register all new services (`ISchedulerPermissionService`, `IAppointmentCodeGenerator`, `ICoverageZoneValidator`, `IAvailabilityService`) in DI container in `src/App.Web/` startup/DI configuration
- [x] T029a [P] Register authorization policy for `MANAGE_SCHEDULER_SYSTEM_PERMISSION` in auth policy configuration (required before any admin scheduler page can gate on this permission) in `src/App.Web/` startup
- [x] T029b [P] Add scheduler staff access check as a page filter or middleware for all `/staff/scheduler` pages — redirect non-scheduler-staff to ticket dashboard (required before any staff scheduler page is created) in `src/App.Web/` startup
- [x] T030 Add Scheduler route constants to Staff area `RouteNames.cs` (Scheduler.Index, Scheduler.AllAppointments, Scheduler.Create, Scheduler.Details, Scheduler.Edit) in `src/App.Web/Areas/Staff/Pages/Shared/RouteNames.cs`
- [x] T031 Add Scheduler route constants to Admin area `RouteNames.cs` (Scheduler.Index, Scheduler.Staff.Index/Add/Edit, Scheduler.AppointmentTypes.Index/Create/Edit, Scheduler.Configuration.Index, Scheduler.EmailTemplates.Index/Edit, Scheduler.Reports.Index) in `src/App.Web/Areas/Admin/Pages/Shared/RouteNames.cs`

**Checkpoint**: Foundation ready — database migrated, all interfaces defined, DI configured, routes registered. User story implementation can now begin.

---

## Phase 3: User Story 1 — Admin Configures and Manages Scheduler Staff (Priority: P1) 🎯 MVP

**Goal**: Admin with "Can Manage Scheduler System" permission can add active admins as scheduler staff, toggle "Can Manage Others' Calendars" flag, set coverage zones, and remove staff.

**Independent Test**: Log in as admin with the permission, navigate to Scheduler admin section, add a staff member, toggle flags, set coverage zones, remove a staff member — confirm all persist after page reload.

### Implementation for User Story 1

- [x] T032 [US1] Create `AddSchedulerStaff` command with Command (UserId: ShortGuid), Validator (user exists, is active admin, not already scheduler staff), Handler (creates SchedulerStaffMember, defaults availability to org hours) in `src/App.Application/SchedulerAdmin/Commands/AddSchedulerStaff.cs`
- [x] T033 [P] [US1] Create `RemoveSchedulerStaff` command with Validator (staff exists, no future active appointments), Handler (deletes SchedulerStaffMember) in `src/App.Application/SchedulerAdmin/Commands/RemoveSchedulerStaff.cs`
- [x] T034 [P] [US1] Create `UpdateSchedulerStaffFlags` command (SchedulerStaffMemberId, CanManageOthersCalendars) in `src/App.Application/SchedulerAdmin/Commands/UpdateSchedulerStaffFlags.cs`
- [x] T035 [P] [US1] Create `UpdateStaffAvailability` command with Validator (hours within org boundary, start < end), Handler (updates AvailabilityJson) in `src/App.Application/SchedulerAdmin/Commands/UpdateStaffAvailability.cs`
- [x] T036 [P] [US1] Create `UpdateStaffCoverageZones` command (SchedulerStaffMemberId, Zipcodes: List<string>), Handler (updates CoverageZonesJson) in `src/App.Application/SchedulerAdmin/Commands/UpdateStaffCoverageZones.cs`
- [x] T037 [P] [US1] Create `GetSchedulerStaff` query with search, pagination, returns PaginatedList<SchedulerStaffListItemDto> in `src/App.Application/SchedulerAdmin/Queries/GetSchedulerStaff.cs`
- [x] T038 [P] [US1] Create `GetSchedulerStaffById` query returning full SchedulerStaffDto with availability, zones, eligible types in `src/App.Application/SchedulerAdmin/Queries/GetSchedulerStaffById.cs`
- [x] T039 [US1] Add "Scheduler" section to admin sidebar navigation, gated by `MANAGE_SCHEDULER_SYSTEM_PERMISSION` authorization check, with sub-items (Staff, Appointment Types, Configuration, Email Templates, Reports) in admin layout file (`src/App.Web/Areas/Admin/Pages/Shared/` sidebar partial or layout)
- [x] T040 [US1] Create admin Scheduler Staff Index page with staff list table (Name, Email, Can Manage Others, Coverage Zones count, Actions), Add Staff dropdown of active admins, Remove button in `src/App.Web/Areas/Admin/Pages/Scheduler/Staff/Index.cshtml` and `Index.cshtml.cs`
- [x] T041 [US1] Create admin Scheduler Staff Edit page with form for CanManageOthersCalendars toggle, personal availability per day-of-week, coverage zones zipcode input in `src/App.Web/Areas/Admin/Pages/Scheduler/Staff/Edit.cshtml` and `Edit.cshtml.cs`

**Checkpoint**: Admin can manage scheduler staff (add, edit flags/availability/zones, remove). Permission-gated admin sidebar visible.

---

## Phase 4: User Story 2 — Admin Configures Scheduler Settings and Appointment Types (Priority: P1)

**Goal**: Admin configures org-wide scheduling settings (hours, defaults, coverage zones), creates appointment types with mode/duration/eligible staff, and manages scheduler email templates.

**Independent Test**: Access Scheduler config page, create an appointment type with "Virtual" mode and 3 eligible staff, set org-wide defaults, edit an email template — confirm all values persist.

### Implementation for User Story 2

- [x] T042 [P] [US2] Create `UpdateSchedulerConfiguration` command with Validator (positive values, start < end for hours), Handler (upserts singleton SchedulerConfiguration) in `src/App.Application/SchedulerAdmin/Commands/UpdateSchedulerConfiguration.cs`
- [x] T043 [P] [US2] Create `GetSchedulerConfiguration` query returning SchedulerConfigurationDto (creates default record if none exists) in `src/App.Application/SchedulerAdmin/Queries/GetSchedulerConfiguration.cs`
- [x] T044 [P] [US2] Create `CreateAppointmentType` command with Validator (name required, valid mode, eligible staff are active scheduler staff), Handler (creates type + eligibility junction records) in `src/App.Application/SchedulerAdmin/Commands/CreateAppointmentType.cs`
- [x] T045 [P] [US2] Create `UpdateAppointmentType` command (name, mode, duration, buffer, horizon, isActive) in `src/App.Application/SchedulerAdmin/Commands/UpdateAppointmentType.cs`
- [x] T046 [P] [US2] Create `UpdateAppointmentTypeEligibility` command (full replacement of eligible staff list) in `src/App.Application/SchedulerAdmin/Commands/UpdateAppointmentTypeEligibility.cs`
- [x] T047 [P] [US2] Create `GetAppointmentTypes` query (list with eligible staff count, supports IncludeInactive filter) in `src/App.Application/SchedulerAdmin/Queries/GetAppointmentTypes.cs`
- [x] T048 [P] [US2] Create `GetAppointmentTypeById` query returning full AppointmentTypeDto with eligible staff list in `src/App.Application/SchedulerAdmin/Queries/GetAppointmentTypeById.cs`
- [x] T049 [P] [US2] Create `UpdateSchedulerEmailTemplate` command (TemplateId, Subject, Content) with Validator (content required, subject required for email channel) in `src/App.Application/SchedulerAdmin/Commands/UpdateSchedulerEmailTemplate.cs`
- [x] T050 [P] [US2] Create `GetSchedulerEmailTemplates` query with optional channel filter in `src/App.Application/SchedulerAdmin/Queries/GetSchedulerEmailTemplates.cs`
- [x] T051 [US2] Create admin Scheduler Configuration page with form for available hours per day, default duration/buffer/horizon, min cancellation notice, reminder lead time, default coverage zones in `src/App.Web/Areas/Admin/Pages/Scheduler/Configuration/Index.cshtml` and `Index.cshtml.cs`
- [x] T052 [US2] Create admin Appointment Types Index page with table (Name, Mode, Duration, Eligible Staff count, Active status, Actions) and Create button in `src/App.Web/Areas/Admin/Pages/Scheduler/AppointmentTypes/Index.cshtml` and `Index.cshtml.cs`
- [x] T053 [US2] Create admin Appointment Types Create page with form for name, mode dropdown (Virtual/In-Person/Either), duration, buffer, horizon overrides, multi-select eligible staff checkboxes in `src/App.Web/Areas/Admin/Pages/Scheduler/AppointmentTypes/Create.cshtml` and `Create.cshtml.cs`
- [x] T054 [US2] Create admin Appointment Types Edit page (same form as Create, pre-populated) in `src/App.Web/Areas/Admin/Pages/Scheduler/AppointmentTypes/Edit.cshtml` and `Edit.cshtml.cs`
- [x] T055 [US2] Create admin Email Templates Index page listing 3 email templates (Confirmation, Reminder, Post-Meeting) with subject preview and Edit button in `src/App.Web/Areas/Admin/Pages/Scheduler/EmailTemplates/Index.cshtml` and `Index.cshtml.cs`
- [x] T056 [US2] Create admin Email Templates Edit page with subject and content textarea, merge variables reference panel listing all available variables in `src/App.Web/Areas/Admin/Pages/Scheduler/EmailTemplates/Edit.cshtml` and `Edit.cshtml.cs`

**Checkpoint**: Admin can configure all scheduler settings, manage appointment types with eligible staff, and customize email templates.

---

## Phase 5: User Story 3 — Staff Member Accesses Scheduler Area (Priority: P1)

**Goal**: Scheduler staff members see "Tickets | Scheduler" toggle in top navigation, can navigate to `/staff/scheduler` with scheduler-specific sidebar (My Schedule, All Appointments, then Contacts and Activity Log).

**Independent Test**: Log in as scheduler staff, verify "Tickets | Scheduler" toggle appears, click "Scheduler" to navigate to `/staff/scheduler`, verify sidebar shows scheduler navigation, click "Tickets" to return.

### Implementation for User Story 3

- [x] T057 [US3] Extract shared staff base layout from existing `_Layout.cshtml` into `_StaffBaseLayout.cshtml` containing the outer HTML shell (head, topbar, user section, footer scripts) — the sidebar content becomes a `RenderSection("Sidebar")` in `src/App.Web/Areas/Staff/Pages/Shared/_StaffBaseLayout.cshtml`
- [x] T058 [US3] Refactor existing `_Layout.cshtml` to use `_StaffBaseLayout.cshtml` as its Layout, moving the ticket/task sidebar content into the Sidebar section — verify no visual regression in `src/App.Web/Areas/Staff/Pages/Shared/_Layout.cshtml`
- [x] T059 [US3] Update the shared topbar breadcrumb in `_StaffBaseLayout.cshtml` to replace "Home" with a "Tickets | Scheduler" toggle — "Scheduler" link only visible if current user is scheduler staff (inject `ISchedulerPermissionService`), highlight active section based on current URL path
- [x] T060 [US3] Create `_SchedulerLayout.cshtml` using `_StaffBaseLayout.cshtml` as Layout, with scheduler-specific sidebar: "My Schedule" and "All Appointments" in a Scheduler nav section, then "Contacts" and "Activity Log" in a Manage section below in `src/App.Web/Areas/Staff/Pages/Scheduler/Shared/_SchedulerLayout.cshtml`
- [x] T061 [US3] Create `_ViewStart.cshtml` in `src/App.Web/Areas/Staff/Pages/Scheduler/` that sets `Layout = "Shared/_SchedulerLayout"`
- [x] T062 [US3] Create `_ViewImports.cshtml` in `src/App.Web/Areas/Staff/Pages/Scheduler/` with necessary using directives and tag helpers
- [x] T063 [US3] Create Scheduler Index page (My Schedule — default landing) with placeholder content showing today's schedule as default view in `src/App.Web/Areas/Staff/Pages/Scheduler/Index.cshtml` and `Index.cshtml.cs` — page sets `ViewData["ActiveMenu"] = "MySchedule"` and checks scheduler staff access
- [x] T064 [US3] Create Scheduler All Appointments page with placeholder table structure in `src/App.Web/Areas/Staff/Pages/Scheduler/AllAppointments.cshtml` and `AllAppointments.cshtml.cs` — page sets `ViewData["ActiveMenu"] = "AllAppointments"`

**Checkpoint**: Staff can toggle between Tickets and Scheduler. Scheduler area has correct layout, sidebar, and navigation. Non-scheduler staff see only "Tickets".

---

## Phase 6: User Story 4 — Staff Member Creates and Manages Appointments (Priority: P1)

**Goal**: Staff creates appointments linked to Contacts with type-based form adaptation (virtual=meeting link, in-person=coverage validation), only eligible staff in assignee dropdown, full status lifecycle management, double-booking prevention.

**Independent Test**: Create a virtual appointment (verify meeting link required), create an in-person appointment (verify coverage zone validation), verify only eligible staff shown per type, change status through Scheduled→Confirmed→InProgress→Completed, reschedule an appointment and verify history preserved.

### Implementation for User Story 4

- [x] T065 [P] [US4] Create `CreateAppointment` command with full Validator (contact exists, type active, staff eligible, mode matches type, meeting link required for virtual, no time overlap, within availability/hours/horizon), Handler (generates code via IAppointmentCodeGenerator, validates coverage zone via ICoverageZoneValidator, creates appointment + history entry, raises AppointmentCreatedEvent) in `src/App.Application/Scheduler/Commands/CreateAppointment.cs`
- [x] T066 [P] [US4] Create `UpdateAppointment` command (notes, meeting link updates only — no status/time changes) in `src/App.Application/Scheduler/Commands/UpdateAppointment.cs`
- [x] T067 [P] [US4] Create `RescheduleAppointment` command with Validator (active status, no overlap at new time, within availability, cancellation notice check), Handler (preserves old time in AppointmentHistory, updates ScheduledStartTime/DurationMinutes) in `src/App.Application/Scheduler/Commands/RescheduleAppointment.cs`
- [x] T068 [P] [US4] Create `ChangeAppointmentStatus` command with Validator (validates transition via `AppointmentStatus.CanTransitionTo()`), Handler (updates status, creates history, raises AppointmentStatusChangedEvent/AppointmentCompletedEvent) in `src/App.Application/Scheduler/Commands/ChangeAppointmentStatus.cs`
- [x] T069 [P] [US4] Create `CancelAppointment` command with Validator (active status, reason required, cancellation notice check with optional override reason), Handler in `src/App.Application/Scheduler/Commands/CancelAppointment.cs`
- [x] T070 [P] [US4] Create `MarkAppointmentNoShow` command with Validator (active status), Handler in `src/App.Application/Scheduler/Commands/MarkAppointmentNoShow.cs`
- [x] T071 [P] [US4] Create `GetMySchedule` query with date and view type (day/week), returns staff's appointments sorted by time plus available slots in `src/App.Application/Scheduler/Queries/GetMySchedule.cs`
- [x] T072 [P] [US4] Create `GetAppointments` query with search (code, contact name), filters (staff, type, status, date range), pagination in `src/App.Application/Scheduler/Queries/GetAppointments.cs`
- [x] T073 [P] [US4] Create `GetAppointmentById` query returning full AppointmentDto with history, contact info, staff info, type info in `src/App.Application/Scheduler/Queries/GetAppointmentById.cs`
- [x] T074 [P] [US4] Create `GetStaffAvailability` query for a specific staff/date/type, returns available and booked slots in `src/App.Application/Scheduler/Queries/GetStaffAvailability.cs`
- [x] T075 [US4] Build Scheduler My Schedule page fully — show today's appointments in timeline/list view, link to appointment details, show available slots using `GetMySchedule` query in `src/App.Web/Areas/Staff/Pages/Scheduler/Index.cshtml` and `Index.cshtml.cs`
- [x] T076 [US4] Build Scheduler All Appointments page fully — searchable/filterable table with Code, Contact, Staff, Type, Mode, DateTime (org tz), Status columns, pagination, using `GetAppointments` query in `src/App.Web/Areas/Staff/Pages/Scheduler/AllAppointments.cshtml` and `AllAppointments.cshtml.cs`
- [x] T077 [US4] Create Scheduler Create Appointment page — Contact search/select, appointment type dropdown, dynamic mode selector (for "Either" types), meeting link field (shown for virtual), assignee dropdown (filtered by type eligibility and CanManageOthersCalendars), date/time picker (org tz), duration (defaulted from type), notes in `src/App.Web/Areas/Staff/Pages/Scheduler/Create.cshtml` and `Create.cshtml.cs`
- [x] T078 [US4] Add JavaScript for Create Appointment form: type selection triggers mode field visibility (virtual=show meeting link, in-person=hide it, either=show mode chooser), type selection triggers assignee dropdown reload (only eligible staff) in `src/App.Web/wwwroot/js/scheduler-create.js` and include in Create page
- [x] T079 [US4] Create Scheduler Appointment Details page — all appointment info, status badge, meeting link (clickable for virtual), contact info, staff info, history timeline, status change buttons (only valid transitions shown), reschedule/cancel/no-show actions in `src/App.Web/Areas/Staff/Pages/Scheduler/Details.cshtml` and `Details.cshtml.cs`
- [x] T080 [US4] Create Scheduler Edit Appointment page — edit notes, meeting link (for virtual), with validation in `src/App.Web/Areas/Staff/Pages/Scheduler/Edit.cshtml` and `Edit.cshtml.cs`

- [x] T080a [US4] Update existing Contact delete logic to check for future active appointments (Scheduled, Confirmed, In Progress) via `IAppDbContext.Appointments` — if any exist, block deletion and return a validation error listing appointment codes, in the relevant Contact delete command under `src/App.Application/Contacts/Commands/`

**Checkpoint**: Full appointment CRUD lifecycle works. Staff can create virtual/in-person appointments, manage status transitions, reschedule with history, and all times display in org timezone.

---

## Phase 7: User Story 5 — Email Notifications for Scheduling Events (Priority: P2)

**Goal**: Confirmation email on appointment creation, reminder email before appointment, post-meeting email on completion — all using scheduler-owned templates with merge variables including `{{MeetingLink}}`.

**Independent Test**: Create a virtual appointment → verify confirmation email sent to both parties with meeting link. Advance time → verify reminder sent. Mark completed → verify post-meeting email sent.

### Implementation for User Story 5

- [x] T081 [P] [US5] Create `ISchedulerNotificationService` interface with `SendConfirmationAsync`, `SendReminderAsync`, `SendPostMeetingAsync` methods in `src/App.Application/Scheduler/Services/ISchedulerNotificationService.cs` and implement it in `src/App.Infrastructure/Services/SchedulerNotificationService.cs` — loads scheduler email template by type, builds AppointmentNotification_RenderModel, renders via IRenderEngine, sends via IEmailer to both contact and staff
- [x] T081a [US5] Register `ISchedulerNotificationService` → `SchedulerNotificationService` in DI container in `src/App.Web/` startup configuration
- [x] T082 [US5] Create `AppointmentCreatedHandler_SendConfirmation` event handler that listens for `AppointmentCreatedEvent` and calls `ISchedulerNotificationService.SendConfirmationAsync` — skips contact email if no email on file, logs warning in `src/App.Application/Scheduler/EventHandlers/AppointmentCreatedHandler_SendConfirmation.cs`
- [x] T083 [P] [US5] Create `AppointmentCompletedHandler_SendPostMeeting` event handler that listens for `AppointmentCompletedEvent` and calls `SendPostMeetingAsync` in `src/App.Application/Scheduler/EventHandlers/AppointmentCompletedHandler_SendPostMeeting.cs`
- [x] T084 [US5] Create `AppointmentReminderJob` background service inheriting `BackgroundService` — runs every 5 minutes, queries appointments where status is active AND ScheduledStartTime minus now <= ReminderLeadTimeMinutes AND ReminderSentAt IS NULL, sends reminder via ISchedulerNotificationService, updates ReminderSentAt, processes in batches of 100 with scoped DbContext in `src/App.Infrastructure/BackgroundTasks/AppointmentReminderJob.cs`
- [x] T085 [US5] Register `AppointmentReminderJob` as hosted service in DI container in `src/App.Web/` startup configuration

**Checkpoint**: All three notification types work. Confirmation on create, reminders via background job, post-meeting on completion. Virtual appointments include meeting link in emails.

---

## Phase 8: User Story 6 — Contact Detail Page Shows Scheduling History (Priority: P2)

**Goal**: Contact detail page shows a "Schedulings" section with appointment list. Non-scheduler staff see read-only list. Scheduler staff can click through to appointment detail.

**Independent Test**: View a Contact's detail page with appointments → see Schedulings section. As non-scheduler user → see read-only list. As scheduler staff → click appointment to see details.

### Implementation for User Story 6

- [x] T086 [P] [US6] Create `GetContactAppointments` query with ContactId, pagination, returns PaginatedList<AppointmentListItemDto> (code, type, mode, date/time in org tz, status) in `src/App.Application/Scheduler/Queries/GetContactAppointments.cs`
- [x] T087 [US6] Update existing Contact Details page to add a "Schedulings" section below existing Tickets section — call `GetContactAppointments` query, render table with appointment code, type, date/time, status. If contact has no appointments show empty state. If current user is scheduler staff, make rows clickable (link to scheduler appointment details). If not scheduler staff, render as plain read-only list with no action buttons in `src/App.Web/Areas/Staff/Pages/Contacts/Details.cshtml` and `Details.cshtml.cs`

**Checkpoint**: Contact detail page shows scheduling history. Access control enforced (read-only vs clickable).

---

## Phase 9: User Story 7 — Admin Views Scheduling Reports (Priority: P3)

**Goal**: Admin sees scheduling metrics: appointments by status, volume over time, staff utilization, no-show/cancellation rates, average duration — all in org timezone.

**Independent Test**: Access Scheduler Reports as admin → verify dashboard shows accurate metrics matching actual appointment data.

### Implementation for User Story 7

- [x] T088 [P] [US7] Create `GetSchedulerReports` query with optional DateFrom/DateTo filters, returns SchedulerReportDto with: AppointmentsByStatus (dict), AppointmentVolumeByDate (list), StaffUtilization (list with name/count/rate), NoShowRate (decimal), CancellationRate (decimal), AverageAppointmentDurationMinutes (decimal) — all date displays converted to org timezone in `src/App.Application/SchedulerAdmin/Queries/GetSchedulerReports.cs`
- [x] T089 [US7] Create admin Scheduler Reports dashboard page with date range filter, status breakdown cards, volume chart data, staff utilization table, rate metrics display in `src/App.Web/Areas/Admin/Pages/Scheduler/Reports/Index.cshtml` and `Index.cshtml.cs`

**Checkpoint**: Reports page shows accurate scheduling metrics with org timezone date display.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements affecting multiple user stories.

- [ ] T090 Verify authorization policy for `MANAGE_SCHEDULER_SYSTEM_PERMISSION` correctly gates all admin scheduler pages (policy registered in T029a)
- [ ] T091 Verify scheduler staff access filter correctly blocks non-scheduler-staff from all `/staff/scheduler` pages (filter registered in T029b)
- [ ] T092 Verify all appointment times render correctly in org timezone across all pages (create form, details, list views, contact schedulings, reports, email templates)
- [ ] T093 Verify soft warning UX for coverage zone override and cancellation notice override — confirm override reasons are stored in AppointmentHistory
- [ ] T094 Verify the email template migration is idempotent — run migration twice and confirm no duplicates
- [ ] T095 Review and verify all admin pages follow the established admin layout patterns (PageHeading partial, card styling, Save button pattern per constitution)
- [ ] T096 Review and verify all staff scheduler pages follow the Staff Area UI Pattern (staff-card, staff-table, staff-badge classes per constitution)
- [ ] T096a Update the existing user deactivation logic to check if the deactivated user is a scheduler staff member — if so, set `SchedulerStaffMember.IsActive = false` and log a warning about any future active appointments, in the relevant user update command under `src/App.Application/Users/Commands/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — Can start immediately after foundational
- **US2 (Phase 4)**: Depends on Phase 2 — Can run in PARALLEL with US1
- **US3 (Phase 5)**: Depends on Phase 2 — Can run in PARALLEL with US1 and US2
- **US4 (Phase 6)**: Depends on US1 (staff must exist), US2 (types must exist), and US3 (scheduler area must exist)
- **US5 (Phase 7)**: Depends on US4 (appointments must exist to send notifications)
- **US6 (Phase 8)**: Depends on US4 (appointments must exist to show on contact page)
- **US7 (Phase 9)**: Depends on US4 (need appointment data for reports)
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### User Story Dependencies

```
Phase 1 (Setup) ──▶ Phase 2 (Foundation) ──┬──▶ US1 (Staff Mgmt)  ──┐
                                            ├──▶ US2 (Config/Types) ──┼──▶ US4 (Appointments) ──┬──▶ US5 (Notifications)
                                            └──▶ US3 (Scheduler UI) ──┘                        ├──▶ US6 (Contact Page)
                                                                                                └──▶ US7 (Reports)
                                                                                                         │
                                                                                                         ▼
                                                                                                   Phase 10 (Polish)
```

### Within Each User Story

- Commands/queries (Application layer) before Razor Pages (Web layer)
- Models/DTOs before services
- Services before pages
- Index/list pages before create/edit pages

### Parallel Opportunities

- **Phase 1**: ALL tasks (T001–T012) can run in parallel — each creates a separate file
- **Phase 2**: T013–T019 (configs) in parallel, T023–T028 (services/DTOs) in parallel. T020–T022 (DbContext/migrations) sequential.
- **US1 + US2 + US3**: Can ALL run in parallel after Phase 2 (different admin sections and different areas)
- **Within US4**: All commands (T065–T070) in parallel, all queries (T071–T074) in parallel, then pages sequentially

---

## Parallel Example: Phase 1 (Setup)

```
# All domain types can be created simultaneously:
Task T001: AppointmentStatus value object
Task T002: AppointmentMode value object
Task T003: SchedulerStaffMember entity
Task T004: AppointmentType entity
Task T005: AppointmentTypeStaffEligibility entity
Task T006: Appointment entity
Task T007: AppointmentHistory entity
Task T008: SchedulerConfiguration entity
Task T009: SchedulerEmailTemplate entity
Task T010: Domain events (4 files)
# Then sequentially:
Task T011: Update Contact entity
Task T012: Update Role.cs permissions
```

## Parallel Example: US1 + US2 + US3

```
# After Phase 2, these three user stories can proceed in parallel:
Developer A: US1 — T032-T041 (Admin Staff Management)
Developer B: US2 — T042-T056 (Admin Config & Types)  
Developer C: US3 — T057-T064 (Staff Scheduler Area)
# All three converge before US4 can begin
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US3 + US4)

1. Complete Phase 1: Setup (all domain types)
2. Complete Phase 2: Foundational (migrations, DI, routes)
3. Complete US1 + US2 + US3 in parallel
4. Complete US4: Create & Manage Appointments
5. **STOP and VALIDATE**: Full scheduling workflow end-to-end
6. Deploy/demo — core scheduler is functional

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 + US2 + US3 → Admin config + Staff area ready
3. US4 → Full appointment management → **Deploy (MVP!)**
4. US5 → Email notifications → Deploy
5. US6 → Contact integration → Deploy
6. US7 → Reports → Deploy
7. Each delivery adds value without breaking previous functionality

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently testable after its phase completes
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All DateTime values stored as UTC, displayed via org timezone converter
- Follow existing codebase patterns: CQRS nested types, FluentValidation, BasePageModel, ValueObject
