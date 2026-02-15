# Implementation Plan: Scheduler System

**Branch**: `008-scheduler-system` | **Date**: 2026-02-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-scheduler-system/spec.md`

## Summary

Add a comprehensive scheduler system to the existing ticket platform. This includes a new "Can Manage Scheduler System" admin permission, an admin backend for configuring scheduler staff, appointment types (virtual/in-person/either), coverage zones, and scheduler-owned email templates. Staff members added to the scheduler get access to `/staff/scheduler` — a new area sharing the ticket system's layout but with scheduler-specific sidebar navigation. The top navigation becomes "Tickets | Scheduler" for dual-area users. Appointments are linked to existing Contacts, have a 6-status lifecycle with enforced transitions, and support auto-generated codes (APT-0001). Email notifications use scheduler-specific Liquid templates with merge variables including `{{MeetingLink}}`. A background job handles reminders. The foundation supports future public UI and text messages.

## Technical Context

**Language/Version**: C# / .NET 8+ (ASP.NET Core)  
**Primary Dependencies**: ASP.NET Core Razor Pages, EF Core, Mediator (CQRS), FluentValidation, Fluid (Liquid templates), Bootstrap 5, Bootstrap Icons  
**Storage**: PostgreSQL via EF Core  
**Testing**: xUnit (unit + integration)  
**Target Platform**: Linux server (ASP.NET Core Kestrel)  
**Project Type**: Web application (monolithic Clean Architecture: Domain → Application → Infrastructure → Web)  
**Performance Goals**: Page loads < 2s, email dispatch < 2 min, background reminder job < 30s per batch  
**Constraints**: Single-tenant, org-timezone-aware, no REST API in v1, no third-party integrations in v1  
**Scale/Scope**: ~15 new domain entities/value objects, ~30 CQRS commands/queries, ~20 Razor Pages (admin + staff), 3 email templates, 1 background job, 1 EF migration

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Gate | Status | Notes |
|---|------|--------|-------|
| 1 | **Clean Architecture & Dependency Rule** | PASS | Scheduler entities in `App.Domain`, CQRS handlers in `App.Application`, DB configs + background job in `App.Infrastructure`, Razor Pages in `App.Web`. No cross-layer violations. |
| 2 | **CQRS & Mediator-Driven Use Cases** | PASS | All scheduler operations modeled as Commands/Queries with Validators and Handlers. Domain events for notifications. `CommandResponseDto<T>` / `QueryResponseDto<T>` return types. |
| 3 | **Razor Pages First, Minimal JavaScript** | PASS | All admin and staff scheduler UI as Razor Pages. Minimal JS only for form interactions (e.g., type-based field toggling, contact search). No SPA frameworks. |
| 4 | **Explicit Data Access & Performance** | PASS | All queries via `IAppDbContext`, async with CancellationToken, projections for list views, `AsNoTracking()` for reads. Batch processing in reminder background job. |
| 5 | **Security, Testing & Observability** | PASS | FluentValidation on all commands. Authorization via `[Authorize(Policy)]` and permission checks. Structured logging with `ILogger`. Audit trail via `AppointmentHistory` entity. |
| 6 | **BuiltIn Value Objects** | PASS | `AppointmentStatus` and `AppointmentMode` follow `ValueObject` pattern with static properties, `From()` factory, `DeveloperName`/`Label`. |
| 7 | **Staff Area UI Pattern** | PASS | Scheduler area shares existing staff layout CSS, card/table/badge classes. New `_SchedulerLayout.cshtml` inherits from shared base. |
| 8 | **Route Constants** | PASS | New `RouteNames.Scheduler.*` nested class in Staff area. New `RouteNames.Scheduler.*` in Admin area. |
| 9 | **GUID vs ShortGuid** | PASS | Domain entities use `Guid`. DTOs, Commands, Queries, PageModels use `ShortGuid`. |
| 10 | **Alert/Message Display** | PASS | Uses `SetSuccessMessage()` / `SetErrorMessage()` from `BasePageModel`. |
| 11 | **Admin Area Page Layout** | PASS | Admin scheduler pages use `_Partials/PageHeading`, `TableCreateAndSearchBar`, card patterns. |

**Post-Phase 1 Re-check**: All gates still PASS. The scheduler-owned email template entity is a new pattern (separate from `BuiltInEmailTemplate`) but justified — see Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/008-scheduler-system/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (CQRS command/query catalog)
│   └── commands-queries.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/App.Domain/
├── Entities/
│   ├── SchedulerStaffMember.cs          # Staff member in scheduler system
│   ├── AppointmentType.cs               # Configurable appointment categories
│   ├── AppointmentTypeStaffEligibility.cs # Junction: which staff can do which type
│   ├── Appointment.cs                   # Core scheduled appointment entity
│   ├── AppointmentHistory.cs            # Audit trail for appointment changes
│   ├── SchedulerConfiguration.cs        # Org-wide scheduler settings
│   └── SchedulerEmailTemplate.cs        # Scheduler-owned email templates
├── ValueObjects/
│   ├── AppointmentStatus.cs             # Scheduled/Confirmed/InProgress/Completed/Cancelled/NoShow
│   └── AppointmentMode.cs              # Virtual/InPerson/Either
├── Events/
│   ├── AppointmentCreatedEvent.cs
│   ├── AppointmentStatusChangedEvent.cs
│   ├── AppointmentRescheduledEvent.cs
│   └── AppointmentCompletedEvent.cs
└── Entities/Role.cs                     # Updated: new ManageSchedulerSystem permission

src/App.Application/
├── Scheduler/
│   ├── Commands/
│   │   ├── CreateAppointment.cs
│   │   ├── UpdateAppointment.cs
│   │   ├── RescheduleAppointment.cs
│   │   ├── ChangeAppointmentStatus.cs
│   │   ├── CancelAppointment.cs
│   │   └── MarkAppointmentNoShow.cs
│   ├── Queries/
│   │   ├── GetAppointments.cs           # List with filters
│   │   ├── GetAppointmentById.cs
│   │   ├── GetMySchedule.cs             # Current staff's schedule
│   │   ├── GetStaffAvailability.cs      # Available slots for a staff member
│   │   └── GetContactAppointments.cs    # Appointments for a contact
│   ├── EventHandlers/
│   │   ├── AppointmentCreatedHandler_SendConfirmation.cs
│   │   └── AppointmentCompletedHandler_SendPostMeeting.cs
│   ├── Services/
│   │   ├── ISchedulerNotificationService.cs
│   │   ├── IAppointmentCodeGenerator.cs
│   │   ├── ICoverageZoneValidator.cs
│   │   └── IAvailabilityService.cs
│   ├── DTOs/
│   │   ├── AppointmentDto.cs
│   │   ├── AppointmentListItemDto.cs
│   │   └── StaffScheduleDto.cs
│   └── RenderModels/
│       └── AppointmentNotification_RenderModel.cs
├── SchedulerAdmin/
│   ├── Commands/
│   │   ├── AddSchedulerStaff.cs
│   │   ├── RemoveSchedulerStaff.cs
│   │   ├── UpdateSchedulerStaffFlags.cs
│   │   ├── UpdateStaffAvailability.cs
│   │   ├── UpdateStaffCoverageZones.cs
│   │   ├── CreateAppointmentType.cs
│   │   ├── UpdateAppointmentType.cs
│   │   ├── UpdateAppointmentTypeEligibility.cs
│   │   ├── UpdateSchedulerConfiguration.cs
│   │   └── UpdateSchedulerEmailTemplate.cs
│   ├── Queries/
│   │   ├── GetSchedulerStaff.cs
│   │   ├── GetSchedulerStaffById.cs
│   │   ├── GetAppointmentTypes.cs
│   │   ├── GetAppointmentTypeById.cs
│   │   ├── GetSchedulerConfiguration.cs
│   │   ├── GetSchedulerEmailTemplates.cs
│   │   └── GetSchedulerReports.cs
│   └── DTOs/
│       ├── SchedulerStaffDto.cs
│       ├── AppointmentTypeDto.cs
│       ├── SchedulerConfigurationDto.cs
│       └── SchedulerReportDto.cs
└── Common/
    └── Interfaces/
        └── IAppDbContext.cs             # Updated: new DbSet properties

src/App.Infrastructure/
├── Persistence/
│   └── Configurations/
│       ├── SchedulerStaffMemberConfiguration.cs
│       ├── AppointmentTypeConfiguration.cs
│       ├── AppointmentTypeStaffEligibilityConfiguration.cs
│       ├── AppointmentConfiguration.cs
│       ├── AppointmentHistoryConfiguration.cs
│       ├── SchedulerConfigurationConfiguration.cs
│       └── SchedulerEmailTemplateConfiguration.cs
├── Migrations/
│   ├── YYYYMMDDHHMMSS_AddSchedulerSystem.cs           # Schema: all new tables + Contact.Zipcode
│   └── YYYYMMDDHHMMSS_SeedSchedulerEmailTemplates.cs  # Seed 6 default templates (idempotent)
├── BackgroundTasks/
│   └── AppointmentReminderJob.cs        # Background job for reminders
└── Services/
    ├── AppointmentCodeGenerator.cs
    ├── CoverageZoneValidator.cs
    ├── AvailabilityService.cs
    └── SchedulerNotificationService.cs

src/App.Web/
├── Areas/
│   ├── Admin/Pages/
│   │   └── Scheduler/                   # New admin section
│   │       ├── Staff/
│   │       │   ├── Index.cshtml         # List scheduler staff + Add staff dropdown
│   │       │   └── Edit.cshtml          # Edit staff (flags, zones, availability)
│   │       ├── AppointmentTypes/
│   │       │   ├── Index.cshtml         # List appointment types
│   │       │   ├── Create.cshtml        # Create type with settings + eligible staff
│   │       │   └── Edit.cshtml          # Edit type
│   │       ├── Configuration/
│   │       │   └── Index.cshtml         # General settings + default coverage zones
│   │       ├── EmailTemplates/
│   │       │   ├── Index.cshtml         # List scheduler templates
│   │       │   └── Edit.cshtml          # Edit template (subject, body, merge vars)
│   │       └── Reports/
│   │           └── Index.cshtml         # Scheduling reports dashboard
│   └── Staff/Pages/
│       └── Scheduler/                   # New staff section
│           ├── Index.cshtml             # My Schedule (default landing)
│           ├── AllAppointments.cshtml    # All Appointments list
│           ├── Create.cshtml            # Create appointment
│           ├── Details.cshtml           # Appointment detail view
│           ├── Edit.cshtml              # Edit appointment
│           └── Shared/
│               └── _SchedulerLayout.cshtml  # Layout override for scheduler sidebar
└── wwwroot/
    └── js/
        └── scheduler-create.js          # Type-based form field toggling (appointment create page)
```

**Structure Decision**: Follows existing vertical-slice organization. Scheduler features live alongside existing Tickets/Teams features in each layer. The Staff scheduler area uses a layout override (`_SchedulerLayout.cshtml`) that inherits the shared staff styling but swaps the sidebar navigation. The Admin scheduler section follows the exact same patterns as the existing Teams/SLA admin pages.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Scheduler-owned email templates (separate from `BuiltInEmailTemplate` / `EmailTemplate` system) | Scheduler templates need scheduler-specific merge variables (`{{MeetingLink}}`, `{{AppointmentMode}}`, etc.) and must evolve independently. Coupling to the system-wide template feature would require modifying the shared rendering pipeline and pollute the global template list. | Extending `BuiltInEmailTemplate` was considered but rejected because: (1) it would add scheduler-specific merge variables to the global rendering context, (2) scheduler templates are managed in a different admin section, (3) the spec explicitly requires separation to avoid coupling. The scheduler templates still use the same `IRenderEngine` (Fluid/Liquid) for rendering — only storage and management are separate. |
| `Contact.Zipcode` field addition | Coverage zone validation for in-person appointments requires matching the contact's zipcode against staff/org coverage zones. | Parsing zipcode from the existing `Address` string field was considered but rejected because: (1) address format is not structured/standardized, (2) parsing is unreliable, (3) the zipcode needs to be independently queryable for zone matching. A dedicated field is cleaner and enables future features (geolocation, reporting by area). |
