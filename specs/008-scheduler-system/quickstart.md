# Quickstart: Scheduler System

**Branch**: `008-scheduler-system` | **Date**: 2026-02-15

## Prerequisites

- .NET 8+ SDK
- PostgreSQL database (existing ticket system DB)
- SMTP server configured (for email notifications)
- Repository cloned and on `008-scheduler-system` branch

## Implementation Order

The scheduler system should be built in this order, matching the user story priorities:

### Phase 1: Foundation (P1 stories — must ship together)

**Step 1: Domain Layer**
1. Add `AppointmentStatus` value object to `src/App.Domain/ValueObjects/`
2. Add `AppointmentMode` value object to `src/App.Domain/ValueObjects/`
3. Add all new entities to `src/App.Domain/Entities/`: `SchedulerStaffMember`, `AppointmentType`, `AppointmentTypeStaffEligibility`, `Appointment`, `AppointmentHistory`, `SchedulerConfiguration`, `SchedulerEmailTemplate`
4. Add domain events to `src/App.Domain/Events/`: `AppointmentCreatedEvent`, `AppointmentStatusChangedEvent`, `AppointmentRescheduledEvent`, `AppointmentCompletedEvent`
5. Update `BuiltInSystemPermission` in `Role.cs`: Add `ManageSchedulerSystem` permission
6. Update `SystemPermissions` enum: Add `ManageSchedulerSystem = 2048`
7. Add `Zipcode` field to `Contact` entity

**Step 2: Infrastructure Layer**
1. Add `IEntityTypeConfiguration` classes for all new entities in `src/App.Infrastructure/Persistence/Configurations/`
2. Update `AppDbContext.cs` with new `DbSet` properties
3. Update `IAppDbContext.cs` with new `DbSet` properties
4. Create EF migration: `dotnet ef migrations add AddSchedulerSystem --project src/App.Infrastructure --startup-project src/App.Web`
5. Create seed migration for default email templates
6. Run migrations: `dotnet ef database update --project src/App.Infrastructure --startup-project src/App.Web`

**Step 3: Application Layer — Admin CQRS**
1. Create `src/App.Application/SchedulerAdmin/` folder structure (Commands, Queries, DTOs)
2. Implement admin commands: `AddSchedulerStaff`, `RemoveSchedulerStaff`, `UpdateSchedulerStaffFlags`, `UpdateStaffAvailability`, `UpdateStaffCoverageZones`
3. Implement admin commands: `CreateAppointmentType`, `UpdateAppointmentType`, `UpdateAppointmentTypeEligibility`
4. Implement admin commands: `UpdateSchedulerConfiguration`, `UpdateSchedulerEmailTemplate`
5. Implement admin queries: `GetSchedulerStaff`, `GetSchedulerStaffById`, `GetAppointmentTypes`, `GetAppointmentTypeById`, `GetSchedulerConfiguration`, `GetSchedulerEmailTemplates`
6. Implement service interfaces: `ISchedulerPermissionService`, `IAppointmentCodeGenerator`, `ICoverageZoneValidator`, `IAvailabilityService`

**Step 4: Application Layer — Staff CQRS**
1. Create `src/App.Application/Scheduler/` folder structure
2. Implement staff commands: `CreateAppointment`, `UpdateAppointment`, `RescheduleAppointment`, `ChangeAppointmentStatus`, `CancelAppointment`, `MarkAppointmentNoShow`
3. Implement staff queries: `GetMySchedule`, `GetAppointments`, `GetAppointmentById`, `GetStaffAvailability`, `GetContactAppointments`

**Step 5: Infrastructure Services**
1. Implement `AppointmentCodeGenerator` in `src/App.Infrastructure/Services/`
2. Implement `CoverageZoneValidator`
3. Implement `AvailabilityService`
4. Implement `SchedulerNotificationService`

**Step 6: Web Layer — Shared Layout**
1. Extract shared staff base layout to `_StaffBaseLayout.cshtml`
2. Create `_SchedulerLayout.cshtml` with scheduler sidebar
3. Update existing `_Layout.cshtml` to use shared base
4. Add "Tickets | Scheduler" toggle to shared topbar
5. Add `RouteNames.Scheduler` to both Staff and Admin route constants

**Step 7: Web Layer — Admin Pages**
1. Create `src/App.Web/Areas/Admin/Pages/Scheduler/` structure
2. Build Staff management pages (Index, Add, Edit)
3. Build Appointment Types pages (Index, Create, Edit)
4. Build Configuration page
5. Build Email Templates pages (Index, Edit)
6. Add Scheduler to admin sidebar navigation (permission-gated)

**Step 8: Web Layer — Staff Pages**
1. Create `src/App.Web/Areas/Staff/Pages/Scheduler/` structure
2. Build My Schedule page (default landing)
3. Build All Appointments page
4. Build Create Appointment page (with type-based form adaptation)
5. Build Appointment Details page
6. Build Edit Appointment page

### Phase 2: Notifications (P2 stories)

**Step 9: Email Notifications**
1. Create `AppointmentNotification_RenderModel` render model
2. Implement event handlers: `AppointmentCreatedHandler_SendConfirmation`, `AppointmentCompletedHandler_SendPostMeeting`
3. Implement `AppointmentReminderJob` background service
4. Register background service in DI

**Step 10: Contact Integration**
1. Update Contact detail page to show Schedulings section
2. Use `GetContactAppointments` query
3. Implement conditional click-through (scheduler staff only)

### Phase 3: Reports (P3 story)

**Step 11: Reports**
1. Implement `GetSchedulerReports` query
2. Build Reports dashboard page in admin area

## Key Configuration

### Permission Check Pattern

```csharp
// In admin pages:
[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Index : BaseAdminPageModel { ... }

// In staff pages:
public class Index : BaseStaffPageModel
{
    // Check via service that current user is a scheduler staff member
    private readonly ISchedulerPermissionService _schedulerPermissions;
}
```

### Timezone Handling

All `DateTime` values stored as UTC. Display conversion:
```csharp
CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(appointment.ScheduledStartTime)
```

### Template Rendering

```csharp
var renderModel = new AppointmentNotification_RenderModel
{
    AppointmentCode = appointment.Code,
    MeetingLink = appointment.MeetingLink ?? "",
    AppointmentType = appointment.AppointmentType.Name,
    // ... etc
};

var wrappedModel = new Wrapper_RenderModel
{
    CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(currentOrganization),
    Target = renderModel,
};

var subject = renderEngine.RenderAsHtml(template.Subject, wrappedModel);
var content = renderEngine.RenderAsHtml(template.Content, wrappedModel);
```

## Verification

After implementation, verify:
1. Admin with permission sees Scheduler in admin sidebar
2. Staff member added to scheduler sees Scheduler in top nav
3. Creating a virtual appointment requires meeting link
4. Creating an in-person appointment validates coverage zones
5. Only eligible staff appear in assignee dropdown per type
6. Status transitions enforce the state machine
7. All times display in org timezone
8. Confirmation email sent on appointment creation
9. Reminder email sent before appointment
10. Contact detail page shows Schedulings section
