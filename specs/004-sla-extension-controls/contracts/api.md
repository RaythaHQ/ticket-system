# API Contracts: SLA Extension Controls

**Feature**: 004-sla-extension-controls  
**Date**: 2026-01-03

## Commands

### ExtendTicketSla

**File**: `src/App.Application/Tickets/Commands/ExtendTicketSla.cs`

Extends the SLA due date for a ticket by a specified number of hours.

#### Command

```csharp
public class Command : IRequest<CommandResponseDto<long>>
{
    /// <summary>
    /// The ticket ID to extend SLA for.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Number of hours to extend the SLA by.
    /// Must be positive (> 0).
    /// </summary>
    public int ExtensionHours { get; set; }
}
```

#### Validation Rules

| Rule | Error Message |
|------|---------------|
| `Id` must exist | "Ticket not found." |
| `ExtensionHours > 0` | "Extension hours must be greater than zero." |
| Ticket status != Closed/Resolved | "Cannot extend SLA on closed or resolved tickets." |
| User has permission OR within limits | "You have reached the maximum number of SLA extensions (X)." |
| ExtensionHours <= MaxExtensionHours (if no permission) | "Extension cannot exceed X hours." |
| Result date must be in future | "Extension would result in a due date in the past." |

#### Response

```csharp
CommandResponseDto<long>
{
    Success = true,
    Result = ticketId // The ticket ID
}
```

#### Side Effects

1. Updates `Ticket.SlaDueAt` = current due date + extension hours (or now + hours if no current due)
2. Increments `Ticket.SlaExtensionCount`
3. Updates `Ticket.SlaStatus` to `ON_TRACK` if currently breached and new date is in future
4. Creates `TicketChangeLogEntry` with:
   - Message: "Extended SLA by {hours} hours. Due date changed from {old} to {new}."
   - FieldChangesJson: `{"SlaDueAt": {"OldValue": "...", "NewValue": "..."}}`
5. Triggers audit log via existing `AuditBehavior`

---

## Queries

### GetSlaExtensionInfo

**File**: `src/App.Application/Tickets/Queries/GetSlaExtensionInfo.cs`

Returns extension state and capabilities for a ticket.

#### Query

```csharp
public class Query : IRequest<QueryResponseDto<SlaExtensionInfoDto>>
{
    /// <summary>
    /// The ticket ID to get extension info for.
    /// </summary>
    public long TicketId { get; set; }
}
```

#### Response

```csharp
public record SlaExtensionInfoDto
{
    public DateTime? CurrentSlaDueAt { get; init; }
    public string CurrentSlaDueAtFormatted { get; init; }
    public int ExtensionCount { get; init; }
    public int MaxExtensions { get; init; }
    public int MaxExtensionHours { get; init; }
    public bool HasUnlimitedExtensions { get; init; }
    public bool CanExtend { get; init; }
    public string? CannotExtendReason { get; init; }
    public int DefaultExtensionHours { get; init; }
    public bool HasSlaRule { get; init; }
}
```

#### Example Response (non-privileged user, 0 extensions used)

```json
{
  "currentSlaDueAt": "2026-01-04T22:00:00Z",
  "currentSlaDueAtFormatted": "Sat, Jan 4, 2026 at 4:00 PM",
  "extensionCount": 0,
  "maxExtensions": 1,
  "maxExtensionHours": 168,
  "hasUnlimitedExtensions": false,
  "canExtend": true,
  "cannotExtendReason": null,
  "defaultExtensionHours": 24,
  "hasSlaRule": true
}
```

#### Example Response (non-privileged user, at limit)

```json
{
  "currentSlaDueAt": "2026-01-05T22:00:00Z",
  "currentSlaDueAtFormatted": "Sun, Jan 5, 2026 at 4:00 PM",
  "extensionCount": 1,
  "maxExtensions": 1,
  "maxExtensionHours": 168,
  "hasUnlimitedExtensions": false,
  "canExtend": false,
  "cannotExtendReason": "Maximum extensions (1) reached.",
  "defaultExtensionHours": 24,
  "hasSlaRule": true
}
```

#### Example Response (privileged user)

```json
{
  "currentSlaDueAt": "2026-01-05T22:00:00Z",
  "currentSlaDueAtFormatted": "Sun, Jan 5, 2026 at 4:00 PM",
  "extensionCount": 3,
  "maxExtensions": 1,
  "maxExtensionHours": 168,
  "hasUnlimitedExtensions": true,
  "canExtend": true,
  "cannotExtendReason": null,
  "defaultExtensionHours": 24,
  "hasSlaRule": true
}
```

---

## Page Handlers

### Details.cshtml.cs Handlers

#### OnPostExtendSla

**Route**: `POST /staff/tickets/{id}?handler=ExtendSla`

Extends the SLA by the specified hours.

**Form Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `extensionHours` | int | Hours to extend by |

**Response**: Redirect to ticket details with success/error message.

---

#### OnGetPreviewSlaExtension

**Route**: `GET /staff/tickets/{id}?handler=PreviewSlaExtension&hours={hours}`

Calculates and returns the projected due date for live preview.

**Query Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `hours` | int | Hours to extend by |

**Response** (JSON):
```json
{
  "dueDateUtc": "2026-01-06T22:00:00Z",
  "dueDateFormatted": "Mon, Jan 6, 2026 at 4:00 PM",
  "valid": true,
  "error": null
}
```

**Error Response** (JSON):
```json
{
  "dueDateUtc": null,
  "dueDateFormatted": null,
  "valid": false,
  "error": "Hours must be greater than zero"
}
```

---

## Service Interface Changes

### ISlaService (Modified)

**File**: `src/App.Application/Common/Interfaces/ISlaService.cs`

```csharp
/// <summary>
/// Calculates the default number of hours to extend an SLA to reach
/// 4pm the next business day in the organization's timezone.
/// </summary>
/// <param name="currentSlaDueAt">Current SLA due date, or null to calculate from now.</param>
/// <param name="timezone">Organization timezone (IANA format), defaults to UTC.</param>
/// <returns>Number of hours to extend by.</returns>
int CalculateDefaultExtensionHours(DateTime? currentSlaDueAt, string? timezone);

/// <summary>
/// Calculates the new SLA due date after extending by the specified hours.
/// </summary>
/// <param name="currentSlaDueAt">Current SLA due date, or null to start from now.</param>
/// <param name="extensionHours">Hours to add.</param>
/// <returns>New due date in UTC.</returns>
DateTime CalculateExtendedDueDate(DateTime? currentSlaDueAt, int extensionHours);
```

---

## Environment Variables

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `SLA_MAX_EXTENSIONS` | int | 1 | Maximum SLA extensions for non-privileged users |
| `SLA_MAX_EXTENSION_HOURS` | int | 168 | Maximum extension hours for non-privileged users |

---

## Error Codes

| Code | HTTP Status | Condition |
|------|-------------|-----------|
| `TICKET_NOT_FOUND` | 404 | Ticket ID does not exist |
| `TICKET_CLOSED` | 400 | Ticket is closed or resolved |
| `EXTENSION_LIMIT_REACHED` | 403 | Non-privileged user at max extensions |
| `EXTENSION_HOURS_EXCEEDED` | 400 | Extension hours > max allowed |
| `INVALID_EXTENSION_HOURS` | 400 | Hours <= 0 |
| `PAST_DUE_DATE` | 400 | Extension would result in past date |

