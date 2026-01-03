# Data Model: SLA Extension Controls

**Feature**: 004-sla-extension-controls  
**Date**: 2026-01-03

## Entity Changes

### Ticket (Modified)

**File**: `src/App.Domain/Entities/Ticket.cs`

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `SlaExtensionCount` | `int` | `0` | Number of times this ticket's SLA has been extended |

**Change Details**:
```csharp
// Add to Ticket.cs after existing SLA fields

/// <summary>
/// Number of times the SLA due date has been extended for this ticket.
/// Used for enforcing extension limits for non-privileged users.
/// </summary>
public int SlaExtensionCount { get; set; } = 0;
```

**Validation Rules**:
- Value must be >= 0
- Incremented by ExtendTicketSla command
- Reset to 0 when ticket status changes to Closed (future consideration)

**State Transitions**:
- Initial: 0
- On each successful extension: +1
- On ticket close/reopen: Stays as-is (per spec assumption, reset is optional)

## Database Migration

**Migration Name**: `AddSlaExtensionCount`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "SlaExtensionCount",
        table: "Tickets",
        type: "integer",
        nullable: false,
        defaultValue: 0);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "SlaExtensionCount",
        table: "Tickets");
}
```

## DTO Changes

### TicketDto (Modified)

**File**: `src/App.Application/Tickets/TicketDto.cs`

```csharp
// Add to TicketDto record

/// <summary>
/// Number of times this ticket's SLA has been extended.
/// </summary>
public int SlaExtensionCount { get; init; }
```

**Projection Update**:
```csharp
// In GetProjection() method
SlaExtensionCount = entity.SlaExtensionCount,
```

## New Models

### SlaExtensionSettings

**File**: `src/App.Application/Common/Models/SlaExtensionSettings.cs`

```csharp
namespace App.Application.Common.Models;

/// <summary>
/// Configuration settings for SLA extension limits.
/// Values sourced from environment variables.
/// </summary>
public class SlaExtensionSettings
{
    /// <summary>
    /// Maximum number of times non-privileged users can extend an SLA.
    /// Default: 1
    /// </summary>
    public int MaxExtensions { get; set; } = 1;
    
    /// <summary>
    /// Maximum hours non-privileged users can extend an SLA by.
    /// Default: 168 (7 days)
    /// </summary>
    public int MaxExtensionHours { get; set; } = 168;
    
    /// <summary>
    /// Creates settings from environment variables.
    /// Uses defaults if variables are not set or invalid.
    /// </summary>
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

### SlaExtensionInfoDto

**File**: `src/App.Application/Tickets/Queries/GetSlaExtensionInfo.cs` (nested record)

```csharp
public record SlaExtensionInfoDto
{
    /// <summary>
    /// Current SLA due date (UTC).
    /// </summary>
    public DateTime? CurrentSlaDueAt { get; init; }
    
    /// <summary>
    /// Current SLA due date formatted for display.
    /// </summary>
    public string CurrentSlaDueAtFormatted { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of times this ticket's SLA has been extended.
    /// </summary>
    public int ExtensionCount { get; init; }
    
    /// <summary>
    /// Maximum extensions allowed for non-privileged users.
    /// </summary>
    public int MaxExtensions { get; init; }
    
    /// <summary>
    /// Maximum hours allowed for non-privileged users.
    /// </summary>
    public int MaxExtensionHours { get; init; }
    
    /// <summary>
    /// True if current user has unlimited extension capability.
    /// </summary>
    public bool HasUnlimitedExtensions { get; init; }
    
    /// <summary>
    /// True if the current user can extend the SLA.
    /// False if at limit or ticket is closed/resolved.
    /// </summary>
    public bool CanExtend { get; init; }
    
    /// <summary>
    /// Reason why extension is not allowed, if applicable.
    /// </summary>
    public string? CannotExtendReason { get; init; }
    
    /// <summary>
    /// Default hours to extend by (targets 4pm next business day).
    /// </summary>
    public int DefaultExtensionHours { get; init; }
    
    /// <summary>
    /// Whether the ticket has an SLA rule assigned.
    /// </summary>
    public bool HasSlaRule { get; init; }
}
```

## Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                           Ticket                                 │
├─────────────────────────────────────────────────────────────────┤
│ Id: long (PK)                                                   │
│ ...existing fields...                                           │
│                                                                 │
│ SlaRuleId: Guid? ──────────────┐ FK to SlaRule (nullable)      │
│ SlaDueAt: DateTime?            │                                │
│ SlaBreachedAt: DateTime?       │                                │
│ SlaStatus: string?             │                                │
│ SlaExtensionCount: int ◄───────┴─ NEW: Tracks extensions       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    TicketChangeLogEntry                         │
├─────────────────────────────────────────────────────────────────┤
│ Records SLA extension events:                                   │
│ - Message: "Extended SLA by X hours"                            │
│ - FieldChangesJson: {"SlaDueAt": {"Old": "...", "New": "..."}}  │
└─────────────────────────────────────────────────────────────────┘
```

## Indexes

No new indexes required. Existing ticket queries don't filter by `SlaExtensionCount`.

## Constraints

| Constraint | Type | Description |
|------------|------|-------------|
| `SlaExtensionCount >= 0` | Application | Enforced in command, not database CHECK |

## Migration Strategy

1. Add column with default value 0 (existing tickets unaffected)
2. No data migration needed
3. Column is NOT NULL with default, so rollback is safe

