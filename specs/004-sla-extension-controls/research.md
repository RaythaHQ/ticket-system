# Research: SLA Extension Controls

**Feature**: 004-sla-extension-controls  
**Date**: 2026-01-03

## Technical Decisions

### 1. Business Day Calculation Algorithm

**Decision**: Calculate hours from current time to 4pm next business day, skipping weekends.

**Rationale**: 
- Uses organization timezone from `ICurrentOrganization.TimeZone`
- Leverages existing `DateTimeExtensions.UtcToTimeZone()` for conversions
- Simple weekday check (DayOfWeek != Saturday/Sunday)
- 4pm target provides reasonable business-day end time

**Alternatives Considered**:
- Full business hours calculation with start/end times: Overly complex for MVP, existing SLA service has this for rule-based SLAs
- Holiday calendar integration: Out of scope, documented as future enhancement
- Configurable target hour: Could add later via ENV variable if needed

**Implementation**:
```csharp
public int CalculateDefaultExtensionHours(DateTime currentUtc, string timezone)
{
    var tz = DateTimeExtensions.GetTimeZoneInfo(timezone ?? "Etc/UTC");
    var localNow = TimeZoneInfo.ConvertTimeFromUtc(currentUtc, tz);
    
    // Target 4pm next business day
    var targetDate = localNow.Date.AddDays(1);
    
    // Skip weekends
    while (targetDate.DayOfWeek == DayOfWeek.Saturday || 
           targetDate.DayOfWeek == DayOfWeek.Sunday)
    {
        targetDate = targetDate.AddDays(1);
    }
    
    var targetTime = targetDate.AddHours(16); // 4pm
    var hoursUntilTarget = (targetTime - localNow).TotalHours;
    
    return (int)Math.Ceiling(hoursUntilTarget);
}
```

### 2. Extension Tracking

**Decision**: Add `SlaExtensionCount` integer field to Ticket entity.

**Rationale**:
- Simple, single field tracks extension count
- Resets naturally when ticket is closed/reopened (documented in spec)
- No need for separate extension history table for MVP

**Alternatives Considered**:
- Full extension history table: Overkill for permission checks, change log already captures this
- JSON array of extension events: Harder to query, unnecessary complexity

### 3. Environment Variable Configuration

**Decision**: Use `SLA_MAX_EXTENSIONS` and `SLA_MAX_EXTENSION_HOURS` with defaults.

**Rationale**:
- Follows existing pattern of ENV-based configuration
- Easy to change per deployment without code changes
- Defaults (1 extension, 168 hours) are reasonable for most use cases

**Configuration Class**:
```csharp
public class SlaExtensionSettings
{
    public int MaxExtensions { get; set; } = 1;
    public int MaxExtensionHours { get; set; } = 168;
    
    public static SlaExtensionSettings FromEnvironment()
    {
        return new SlaExtensionSettings
        {
            MaxExtensions = int.TryParse(
                Environment.GetEnvironmentVariable("SLA_MAX_EXTENSIONS"), 
                out var max) ? max : 1,
            MaxExtensionHours = int.TryParse(
                Environment.GetEnvironmentVariable("SLA_MAX_EXTENSION_HOURS"), 
                out var hours) ? hours : 168
        };
    }
}
```

### 4. Inline UI Pattern

**Decision**: Expandable section within existing SLA card, no modal dialog.

**Rationale**:
- User explicitly requested no popup dialogs
- Keeps context visible (ticket info, current SLA)
- Progressive enhancement with vanilla JS for live preview
- Graceful degradation: form works without JS

**UI States**:
1. **Collapsed**: "Due: [date] | Status: [badge] | [Extend link]"
2. **Expanded**: Hours input + Apply button + Preview + Cancel link
3. **Disabled**: At limit message for non-privileged users

### 5. Live Preview API

**Decision**: Dedicated GET endpoint returning calculated due date.

**Rationale**:
- Keeps business logic server-side
- Debounced client-side calls (150ms) prevent excessive requests
- Returns formatted date string for immediate display

**Endpoint**:
```
GET /staff/tickets/{id}?handler=PreviewSlaExtension&hours=24
Response: { "dueDateFormatted": "Mon, Jan 6, 2026 at 4:00 PM", "dueDateUtc": "2026-01-06T16:00:00Z" }
```

### 6. Permission Check Flow

**Decision**: Check permissions in both UI (visibility) and command (enforcement).

**Rationale**:
- UI hides/disables options for better UX
- Command validates for security (defense in depth)
- Uses existing `ITicketPermissionService.CanManageTickets()`

**Flow**:
1. UI checks `CanManageTickets` to show "Unlimited" badge vs "X of Y used"
2. UI checks extension count vs max to enable/disable input
3. Command re-validates everything before applying

### 7. Change Log Format

**Decision**: Structured message with before/after values.

**Rationale**:
- Consistent with existing change log entries
- Auditable trail per FR-009

**Format**:
```
"Extended SLA by 24 hours. Due date changed from [old] to [new]."
```

### 8. Edge Case: No SLA Rule

**Decision**: Allow extension even without SLA rule; creates ad-hoc due date.

**Rationale**:
- Spec clarification explicitly requires this
- Enables workflow where tickets get SLA applied later
- Extension effectively becomes "set custom due date"

**Behavior**:
- `SlaDueAt` is set to `Now + ExtensionHours`
- `SlaRuleId` remains null (ad-hoc SLA)
- `SlaStatus` set to `ON_TRACK`

## Dependencies

### Existing Infrastructure Used

| Component | Usage |
|-----------|-------|
| `ICurrentOrganization.TimeZone` | Organization timezone for business day calc |
| `ITicketPermissionService.CanManageTickets()` | Permission check |
| `DateTimeExtensions` | Timezone conversion utilities |
| `OrganizationTimeZoneConverter` | Date formatting for display |
| `TicketChangeLogEntry` | Audit trail |

### New Components Required

| Component | Location | Purpose |
|-----------|----------|---------|
| `SlaExtensionSettings` | App.Application/Common/Models | ENV config binding |
| `ExtendTicketSla` | App.Application/Tickets/Commands | Main command |
| `GetSlaExtensionInfo` | App.Application/Tickets/Queries | UI data query |
| Migration | App.Infrastructure/Migrations | Add SlaExtensionCount column |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Timezone edge cases | Medium | Fallback to UTC if timezone invalid/null |
| Race conditions on extension count | Low | Optimistic concurrency check in handler |
| JS disabled users can't preview | Low | Form still works, submit shows result |

## Open Questions (Resolved)

1. ~~Should privileged users have datetime picker?~~ → No, hours-based for everyone
2. ~~Should there be a modal dialog?~~ → No, inline UI per user input
3. ~~What about tickets without SLA rule?~~ → Allow extension, creates ad-hoc due date

