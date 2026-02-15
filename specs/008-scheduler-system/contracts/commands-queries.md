# CQRS Commands & Queries: Scheduler System

**Branch**: `008-scheduler-system` | **Date**: 2026-02-15

> No REST API in v1. All operations via Mediator CQRS from Razor Page code-behinds.

---

## Admin Commands (SchedulerAdmin)

### AddSchedulerStaff

```
Command {
  UserId: ShortGuid          // Active admin to add
}
Validator:
  - UserId must exist, be active, be admin
  - User must not already be a scheduler staff member
Response: CommandResponseDto<ShortGuid>  // SchedulerStaffMember.Id
```

### RemoveSchedulerStaff

```
Command {
  SchedulerStaffMemberId: ShortGuid
}
Validator:
  - Staff member must exist
  - Must NOT have future unresolved appointments (status: Scheduled, Confirmed, InProgress)
Response: CommandResponseDto<ShortGuid>
```

### UpdateSchedulerStaffFlags

```
Command {
  SchedulerStaffMemberId: ShortGuid
  CanManageOthersCalendars: bool
}
Response: CommandResponseDto<ShortGuid>
```

### UpdateStaffAvailability

```
Command {
  SchedulerStaffMemberId: ShortGuid
  Availability: Dictionary<string, DaySchedule>  // "monday" → { Start: "09:00", End: "17:00" }
}
Validator:
  - Hours must fall within org-wide available hours
  - Start must be before End for each day
Response: CommandResponseDto<ShortGuid>
```

### UpdateStaffCoverageZones

```
Command {
  SchedulerStaffMemberId: ShortGuid
  Zipcodes: List<string>     // Empty list = clear custom zones (use org default)
}
Validator:
  - Zipcodes must be non-empty strings, max 20 chars each
Response: CommandResponseDto<ShortGuid>
```

### CreateAppointmentType

```
Command {
  Name: string
  Mode: string               // "virtual", "in_person", "either"
  DefaultDurationMinutes: int?
  BufferTimeMinutes: int?
  BookingHorizonDays: int?
  EligibleStaffMemberIds: List<ShortGuid>
}
Validator:
  - Name required, max 200
  - Mode must be valid AppointmentMode
  - Duration/buffer/horizon must be positive if provided
  - Eligible staff IDs must be active scheduler staff members
Response: CommandResponseDto<ShortGuid>
```

### UpdateAppointmentType

```
Command {
  AppointmentTypeId: ShortGuid
  Name: string
  Mode: string
  DefaultDurationMinutes: int?
  BufferTimeMinutes: int?
  BookingHorizonDays: int?
  IsActive: bool
}
Validator:
  - Same as Create minus eligible staff (separate command)
Response: CommandResponseDto<ShortGuid>
```

### UpdateAppointmentTypeEligibility

```
Command {
  AppointmentTypeId: ShortGuid
  EligibleStaffMemberIds: List<ShortGuid>  // Full replacement
}
Validator:
  - All IDs must be active scheduler staff members
Response: CommandResponseDto<ShortGuid>
```

### UpdateSchedulerConfiguration

```
Command {
  AvailableHours: Dictionary<string, DaySchedule>
  DefaultDurationMinutes: int
  DefaultBufferTimeMinutes: int
  DefaultBookingHorizonDays: int
  MinCancellationNoticeHours: int
  ReminderLeadTimeMinutes: int
  DefaultCoverageZones: List<string>
}
Validator:
  - Duration, buffer, horizon, notice, reminder must be positive
  - Available hours: start < end for each day
Response: CommandResponseDto<ShortGuid>
```

### UpdateSchedulerEmailTemplate

```
Command {
  TemplateId: ShortGuid
  Subject: string?            // Null for SMS
  Content: string
}
Validator:
  - Content required
  - Subject required for email channel
Response: CommandResponseDto<ShortGuid>
```

---

## Admin Queries (SchedulerAdmin)

### GetSchedulerStaff

```
Query {
  Search: string?
  OrderBy: string?
  PageNumber: int
  PageSize: int
}
Response: PaginatedList<SchedulerStaffListItemDto>
  - Id, UserFullName, UserEmail, CanManageOthersCalendars, IsActive, AppointmentTypeCount, CoverageZoneCount
```

### GetSchedulerStaffById

```
Query {
  SchedulerStaffMemberId: ShortGuid
}
Response: SchedulerStaffDto
  - Full details including availability, coverage zones, eligible types
```

### GetAppointmentTypes

```
Query {
  IncludeInactive: bool
  OrderBy: string?
}
Response: List<AppointmentTypeListItemDto>
  - Id, Name, Mode, Duration, EligibleStaffCount, IsActive, SortOrder
```

### GetAppointmentTypeById

```
Query {
  AppointmentTypeId: ShortGuid
}
Response: AppointmentTypeDto
  - Full details including eligible staff list
```

### GetSchedulerConfiguration

```
Query { }
Response: SchedulerConfigurationDto
  - All org-wide settings
```

### GetSchedulerEmailTemplates

```
Query {
  Channel: string?            // Filter by "email" or "sms"
}
Response: List<SchedulerEmailTemplateDto>
  - Id, TemplateType, Channel, Subject, ContentPreview, IsActive
```

### GetSchedulerReports

```
Query {
  DateFrom: DateTime?
  DateTo: DateTime?
}
Response: SchedulerReportDto
  - AppointmentsByStatus: Dictionary<string, int>
  - AppointmentVolumeByDate: List<DateCountPair>
  - StaffUtilization: List<StaffUtilizationItem>
  - NoShowRate: decimal
  - CancellationRate: decimal
  - AverageAppointmentDurationMinutes: decimal
```

---

## Staff Commands (Scheduler)

### CreateAppointment

```
Command {
  ContactId: long
  AppointmentTypeId: ShortGuid
  AssignedStaffMemberId: ShortGuid
  Mode: string                // "virtual" or "in_person" (for "either" types)
  MeetingLink: string?        // Required for virtual
  ScheduledStartTime: DateTime
  DurationMinutes: int
  Notes: string?
}
Validator:
  - Contact must exist
  - AppointmentType must exist and be active
  - Staff member must be eligible for this type
  - If current user doesn't have CanManageOthersCalendars, AssignedStaffMemberId must be self
  - Mode must match type's mode (or be a valid choice for "either")
  - MeetingLink required when mode = "virtual"
  - ScheduledStartTime within booking horizon
  - No time overlap with staff member's existing appointments (+ buffer)
  - Time within staff member's availability and org hours
Raises: AppointmentCreatedEvent
Response: CommandResponseDto<long>  // Appointment.Id
```

### UpdateAppointment

```
Command {
  AppointmentId: long
  Notes: string?
  MeetingLink: string?
}
Validator:
  - Appointment must exist, not terminal
  - If virtual, meeting link must remain non-empty
Response: CommandResponseDto<long>
```

### RescheduleAppointment

```
Command {
  AppointmentId: long
  NewScheduledStartTime: DateTime
  NewDurationMinutes: int?    // Optional, keeps current if null
  RescheduleReason: string?
  CancellationNoticeOverrideReason: string?  // If within notice period
}
Validator:
  - Appointment must exist, status must be active
  - New time must not overlap with staff's other appointments
  - New time within availability and org hours
  - If within cancellation notice period, override reason required
Raises: AppointmentRescheduledEvent
Creates: AppointmentHistory entry with old/new times
Response: CommandResponseDto<long>
```

### ChangeAppointmentStatus

```
Command {
  AppointmentId: long
  NewStatus: string
}
Validator:
  - Appointment must exist
  - Transition must be valid per state machine (AppointmentStatus.CanTransitionTo)
Raises: AppointmentStatusChangedEvent (and AppointmentCompletedEvent if → Completed)
Creates: AppointmentHistory entry
Response: CommandResponseDto<long>
```

### CancelAppointment

```
Command {
  AppointmentId: long
  CancellationReason: string
  CancellationNoticeOverrideReason: string?  // If within notice period
}
Validator:
  - Appointment must exist, status must be active
  - CancellationReason required
  - If within notice period, override reason required
Raises: AppointmentStatusChangedEvent (→ Cancelled)
Creates: AppointmentHistory entry
Response: CommandResponseDto<long>
```

### MarkAppointmentNoShow

```
Command {
  AppointmentId: long
}
Validator:
  - Appointment must exist, status must be active
Raises: AppointmentStatusChangedEvent (→ NoShow)
Creates: AppointmentHistory entry
Response: CommandResponseDto<long>
```

---

## Staff Queries (Scheduler)

### GetMySchedule

```
Query {
  Date: DateTime?             // Default: today
  ViewType: string            // "day", "week"
}
Response: StaffScheduleDto
  - StaffMemberId, StaffName
  - Appointments: List<AppointmentListItemDto>  // Sorted by ScheduledStartTime
  - AvailableSlots: List<TimeSlot>
```

### GetAppointments

```
Query {
  Search: string?             // Searches code, contact name
  StaffMemberId: ShortGuid?   // Filter by staff
  AppointmentTypeId: ShortGuid?
  Status: string?
  DateFrom: DateTime?
  DateTo: DateTime?
  OrderBy: string?
  PageNumber: int
  PageSize: int
}
Response: PaginatedList<AppointmentListItemDto>
  - Id, Code, ContactName, StaffName, TypeName, Mode, ScheduledStartTime, Duration, Status
```

### GetAppointmentById

```
Query {
  AppointmentId: long
}
Response: AppointmentDto
  - Full details including history, contact info, staff info, type info
```

### GetStaffAvailability

```
Query {
  StaffMemberId: ShortGuid
  Date: DateTime
  AppointmentTypeId: ShortGuid
}
Response: StaffAvailabilityDto
  - Date, StaffName
  - AvailableSlots: List<TimeSlot>  // { Start, End, DurationMinutes }
  - BookedSlots: List<BookedSlot>   // { Start, End, AppointmentCode }
```

### GetContactAppointments

```
Query {
  ContactId: long
  PageNumber: int
  PageSize: int
}
Response: PaginatedList<AppointmentListItemDto>
  - Same as GetAppointments but filtered to a specific contact
  - Used by Contact detail page Schedulings section
```
