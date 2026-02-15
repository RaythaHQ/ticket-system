# Data Model: Scheduler System

**Branch**: `008-scheduler-system` | **Date**: 2026-02-15

## Entity Relationship Overview

```
User (existing) ──1:N──▶ SchedulerStaffMember
                              │
                              ├── has personal availability (JSON)
                              ├── has coverage zones (JSON)
                              │
                              └──M:N──▶ AppointmentType (via AppointmentTypeStaffEligibility)
                                            │
                                            ├── has mode (Virtual/InPerson/Either)
                                            ├── has duration, buffer, booking horizon overrides
                                            │
Contact (existing) ──1:N──▶ Appointment ◀──N:1── SchedulerStaffMember
                              │
                              ├── has code (APT-0001)
                              ├── has mode (Virtual/InPerson)
                              ├── has meeting link (virtual only)
                              ├── has status (state machine)
                              │
                              └──1:N──▶ AppointmentHistory

SchedulerConfiguration (singleton per org)
SchedulerEmailTemplate (3 types × 2 channels)
```

---

## Entities

### SchedulerStaffMember

Represents an active admin added to the scheduler system. Junction-like entity linking `User` to scheduler capabilities.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | Inherited from `BaseAuditableEntity` |
| UserId | Guid | FK → User, Unique, Required | One staff record per user |
| CanManageOthersCalendars | bool | Default: false | Secretary capability |
| IsActive | bool | Default: true | Soft disable without removing |
| AvailabilityJson | string? | | JSON: per-day hours, e.g. `{"monday":{"start":"09:00","end":"17:00"},...}` |
| CoverageZonesJson | string? | | JSON array of zipcodes, e.g. `["10001","10002"]`. Null = use org default |
| CreationTime | DateTime | | From `BaseAuditableEntity` |
| LastModificationTime | DateTime? | | From `BaseAuditableEntity` |

**Relationships**: One User → One SchedulerStaffMember. Many SchedulerStaffMembers ↔ Many AppointmentTypes (via junction).

**Indexes**: `UserId` (unique), `IsActive`

---

### AppointmentType

Configurable category of appointment with type-specific settings.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | Inherited from `BaseAuditableEntity` |
| Name | string | Required, MaxLength(200) | e.g. "Initial Consultation", "Home Visit" |
| Mode | string | Required, MaxLength(50) | `AppointmentMode` value object: "virtual", "in_person", "either" |
| DefaultDurationMinutes | int? | | Override org default. Null = use org default |
| BufferTimeMinutes | int? | | Override org default. Null = use org default |
| BookingHorizonDays | int? | | Override org default. Null = use org default |
| IsActive | bool | Default: true | |
| SortOrder | int | Default: 0 | Display ordering |
| CreationTime | DateTime | | From `BaseAuditableEntity` |
| LastModificationTime | DateTime? | | From `BaseAuditableEntity` |

**Relationships**: Many AppointmentTypes ↔ Many SchedulerStaffMembers (via junction). One AppointmentType → Many Appointments.

**Indexes**: `IsActive`, `SortOrder`

---

### AppointmentTypeStaffEligibility

Junction entity linking appointment types to eligible staff members.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | Inherited from `BaseAuditableEntity` |
| AppointmentTypeId | Guid | FK → AppointmentType, Required | |
| SchedulerStaffMemberId | Guid | FK → SchedulerStaffMember, Required | |
| CreationTime | DateTime | | From `BaseAuditableEntity` |

**Constraints**: Unique composite index on `(AppointmentTypeId, SchedulerStaffMemberId)`.

---

### Appointment

Core scheduled appointment entity. Uses numeric ID for human-readable codes.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | long | PK, ValueGeneratedNever | From `BaseNumericFullAuditableEntity`. Auto-generated via `INumericIdGenerator` |
| ContactId | long | FK → Contact, Required | Patient/contact for this appointment |
| AssignedStaffMemberId | Guid | FK → SchedulerStaffMember, Required | Staff member conducting the appointment |
| AppointmentTypeId | Guid | FK → AppointmentType, Required | |
| Mode | string | Required, MaxLength(50) | `AppointmentMode` value object: "virtual" or "in_person" (resolved from type) |
| MeetingLink | string? | MaxLength(2000) | URL for virtual appointments. Required when Mode = "virtual" |
| ScheduledStartTime | DateTime | Required | Stored as UTC |
| DurationMinutes | int | Required | In minutes |
| Status | string | Required, MaxLength(50) | `AppointmentStatus` value object |
| Notes | string? | | Free-text notes |
| CancellationReason | string? | MaxLength(1000) | Set when cancelled |
| CoverageZoneOverrideReason | string? | MaxLength(1000) | Set when in-person booked outside zone |
| CancellationNoticeOverrideReason | string? | MaxLength(1000) | Set when late cancel/reschedule |
| ReminderSentAt | DateTime? | | Null until reminder sent. Prevents duplicate reminders |
| CreatedByStaffId | Guid | FK → User | Staff member who created the appointment |
| CreationTime | DateTime | | From base |
| LastModificationTime | DateTime? | | From base |
| IsDeleted | bool | Default: false | Soft delete from `BaseNumericFullAuditableEntity` |

**Computed Properties (NotMapped)**:
- `Code`: `$"APT-{Id:D4}"` (e.g., "APT-0001234")
- `StatusValue`: `AppointmentStatus.From(Status)`
- `ModeValue`: `AppointmentMode.From(Mode)`

**Relationships**: Contact → many Appointments. SchedulerStaffMember → many Appointments. AppointmentType → many Appointments.

**Indexes**: `ContactId`, `AssignedStaffMemberId`, `AppointmentTypeId`, `Status`, `ScheduledStartTime`, `CreationTime`, partial index on `ReminderSentAt IS NULL AND Status IN ('scheduled','confirmed')` for reminder job.

---

### AppointmentHistory

Audit trail for appointment changes. Immutable log entries.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| AppointmentId | long | FK → Appointment, Required | |
| ChangeType | string | Required, MaxLength(50) | "created", "status_changed", "rescheduled", "cancelled", "edited", "coverage_override", "cancellation_notice_override" |
| OldValue | string? | | Previous value (e.g., old status, old datetime) |
| NewValue | string? | | New value |
| OverrideReason | string? | MaxLength(1000) | Reason for override (coverage zone, cancellation notice) |
| ChangedByUserId | Guid | FK → User, Required | |
| Timestamp | DateTime | Required | UTC |

**Indexes**: `AppointmentId`, `Timestamp`

---

### SchedulerConfiguration

Organization-wide scheduler settings. Singleton per organization (single row).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | Inherited from `BaseAuditableEntity` |
| AvailableHoursJson | string | Required | JSON: per-day schedule, e.g. `{"monday":{"start":"09:00","end":"17:00"},...}` |
| DefaultDurationMinutes | int | Required, Default: 30 | Fallback when type doesn't specify |
| DefaultBufferTimeMinutes | int | Required, Default: 15 | Fallback when type doesn't specify |
| DefaultBookingHorizonDays | int | Required, Default: 30 | Fallback when type doesn't specify |
| MinCancellationNoticeHours | int | Required, Default: 24 | Minimum notice for cancellation/reschedule |
| ReminderLeadTimeMinutes | int | Required, Default: 60 | How far before appointment to send reminder |
| DefaultCoverageZonesJson | string? | | JSON array of zipcodes. Null = no zone restriction |
| CreationTime | DateTime | | From `BaseAuditableEntity` |
| LastModificationTime | DateTime? | | From `BaseAuditableEntity` |

---

### SchedulerEmailTemplate

Scheduler-owned message templates. Separate from system-wide `EmailTemplate`.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | Inherited from `BaseAuditableEntity` |
| TemplateType | string | Required, MaxLength(50) | "confirmation", "reminder", "post_meeting" |
| Channel | string | Required, MaxLength(20) | "email" or "sms" |
| Subject | string? | MaxLength(500) | Email subject (Liquid template). Null for SMS |
| Content | string | Required | Body content (Liquid template) |
| IsActive | bool | Default: true | Email active in v1, SMS inactive |
| CreationTime | DateTime | | From `BaseAuditableEntity` |
| LastModificationTime | DateTime? | | From `BaseAuditableEntity` |

**Constraints**: Unique composite index on `(TemplateType, Channel)`.

**Merge Variables Available**:
- `{{AppointmentCode}}` — e.g., "APT-0001234"
- `{{MeetingLink}}` — URL or empty for in-person
- `{{AppointmentType}}` — Type name
- `{{AppointmentMode}}` — "Virtual" or "In-Person"
- `{{DateTime}}` — Formatted in org timezone
- `{{Duration}}` — e.g., "30 minutes"
- `{{StaffName}}` — Assigned staff full name
- `{{StaffEmail}}` — Assigned staff email
- `{{ContactName}}` — Contact full name
- `{{ContactEmail}}` — Contact email
- `{{ContactZipcode}}` — Contact zipcode
- `{{Notes}}` — Appointment notes

---

## Existing Entity Modifications

### Contact (existing — `src/App.Domain/Entities/Contact.cs`)

**Add field**:

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Zipcode | string? | MaxLength(20) | Postal/zip code for coverage zone validation |

**Add collection**:
- `public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();`

### Role / BuiltInSystemPermission (existing — `src/App.Domain/Entities/Role.cs`)

**Add permission**:
- Constant: `MANAGE_SCHEDULER_SYSTEM_PERMISSION = "manage_scheduler_system"`
- Enum flag: `ManageSchedulerSystem = 2048`
- Static property: `ManageSchedulerSystem => new("Manage Scheduler System", MANAGE_SCHEDULER_SYSTEM_PERMISSION, SystemPermissions.ManageSchedulerSystem)`
- Add to `From()`, `Permissions`, `AllPermissionsAsEnum`

### IAppDbContext (existing — `src/App.Application/Common/Interfaces/IAppDbContext.cs`)

**Add DbSets**:
```csharp
// Scheduler entities
public DbSet<SchedulerStaffMember> SchedulerStaffMembers { get; }
public DbSet<AppointmentType> AppointmentTypes { get; }
public DbSet<AppointmentTypeStaffEligibility> AppointmentTypeStaffEligibilities { get; }
public DbSet<Appointment> Appointments { get; }
public DbSet<AppointmentHistory> AppointmentHistories { get; }
public DbSet<SchedulerConfiguration> SchedulerConfigurations { get; }
public DbSet<SchedulerEmailTemplate> SchedulerEmailTemplates { get; }
```

---

## Value Objects

### AppointmentStatus

```
Constants:
  SCHEDULED = "scheduled"
  CONFIRMED = "confirmed"
  IN_PROGRESS = "in_progress"
  COMPLETED = "completed"
  CANCELLED = "cancelled"
  NO_SHOW = "no_show"

Static Properties:
  Scheduled => ("Scheduled", "scheduled")
  Confirmed => ("Confirmed", "confirmed")
  InProgress => ("In Progress", "in_progress")
  Completed => ("Completed", "completed")
  Cancelled => ("Cancelled", "cancelled")
  NoShow => ("No-Show", "no_show")

Methods:
  From(string developerName) → AppointmentStatus
  CanTransitionTo(AppointmentStatus target) → bool
  IsTerminal → bool (Cancelled or NoShow)
  IsActive → bool (Scheduled, Confirmed, or InProgress)
```

### AppointmentMode

```
Constants:
  VIRTUAL = "virtual"
  IN_PERSON = "in_person"
  EITHER = "either"

Static Properties:
  Virtual => ("Virtual", "virtual")
  InPerson => ("In-Person", "in_person")
  Either => ("Either", "either")

Methods:
  From(string developerName) → AppointmentMode
  RequiresMeetingLink → bool (true for Virtual)
  RequiresCoverageValidation → bool (true for InPerson)
```

---

## Migrations

### Migration 1: AddSchedulerSystem
- Creates all 7 new tables with indexes and foreign keys
- Adds `Zipcode` column to `Contacts` table
- Permission is code-based (added to `Role.cs`), no data migration needed

### Migration 2: SeedSchedulerEmailTemplates
- Inserts 6 default templates (3 types × 2 channels) using idempotent SQL (`WHERE NOT EXISTS`)
- Email templates: active (`IsActive = true`)
- SMS templates: inactive (`IsActive = false`) — foundation for future
