# Quickstart: SLA Extension Controls

**Feature**: 004-sla-extension-controls  
**Date**: 2026-01-03

## Overview

This feature adds the ability to extend ticket SLA due dates by a specified number of hours, with permission-based restrictions for non-privileged users.

## Prerequisites

- .NET 8 SDK
- PostgreSQL database
- Existing ticket-system development environment

## Configuration

### Environment Variables

Add these to your `.env` or environment configuration:

```bash
# Maximum times non-privileged users can extend an SLA (default: 1)
SLA_MAX_EXTENSIONS=1

# Maximum hours non-privileged users can extend by (default: 168 = 7 days)
SLA_MAX_EXTENSION_HOURS=168
```

## Database Setup

After implementing the migration, apply it:

```bash
cd src/App.Web
dotnet ef database update
```

## Key Files

### Domain Layer
- `src/App.Domain/Entities/Ticket.cs` - Added `SlaExtensionCount` property

### Application Layer
- `src/App.Application/Common/Models/SlaExtensionSettings.cs` - ENV config binding
- `src/App.Application/Tickets/Commands/ExtendTicketSla.cs` - Main command
- `src/App.Application/Tickets/Queries/GetSlaExtensionInfo.cs` - Extension state query
- `src/App.Application/Common/Interfaces/ISlaService.cs` - Added calculation methods
- `src/App.Application/SlaRules/Services/SlaService.cs` - Business day calculation

### Infrastructure Layer
- `src/App.Infrastructure/Migrations/YYYYMMDD_AddSlaExtensionCount.cs` - Database migration

### Web Layer
- `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml` - Inline extension UI
- `src/App.Web/Areas/Staff/Pages/Tickets/Details.cshtml.cs` - Page handlers

## Usage

### Extending an SLA

1. Navigate to a ticket detail page: `/staff/tickets/{id}`
2. In the SLA card (sidebar), click "Extend"
3. Enter hours (default pre-filled targeting 4pm next business day)
4. See live preview of resulting due date
5. Click "Apply" to extend

### Permission Behavior

| User Type | Behavior |
|-----------|----------|
| With "Manage Tickets" permission | Unlimited extensions, unlimited hours |
| Without "Manage Tickets" permission | Limited to `SLA_MAX_EXTENSIONS` extensions and `SLA_MAX_EXTENSION_HOURS` hours |

### Tickets Without SLA Rule

If a ticket has no SLA rule assigned:
- Extension is still available
- Extension creates an ad-hoc SLA due date
- `SlaRuleId` remains null

## Testing

### Manual Testing Checklist

1. **Basic Extension**
   - Open a ticket with active SLA
   - Click Extend, enter hours, verify due date updates
   - Verify change log entry created

2. **Default Hours Calculation**
   - Open extension on Friday afternoon
   - Verify default targets Monday 4pm (skipping weekend)

3. **Non-Privileged User Limits**
   - Log in as user without "Manage Tickets" permission
   - Extend SLA once
   - Verify second extension is blocked
   - Verify UI shows "1 of 1 extensions used"

4. **Privileged User**
   - Log in as user with "Manage Tickets" permission
   - Verify "Unlimited" badge shown
   - Verify can extend multiple times

5. **Closed Ticket**
   - Open a closed ticket
   - Verify extension option is disabled

6. **No SLA Rule**
   - Open ticket without SLA rule
   - Verify extension option is available
   - Extend and verify ad-hoc due date is set

### Unit Tests to Write

```csharp
// ExtendTicketSla.Command Tests
[Fact] public async Task Handle_ValidExtension_UpdatesDueDate()
[Fact] public async Task Handle_ValidExtension_IncrementsExtensionCount()
[Fact] public async Task Handle_ValidExtension_CreatesChangeLogEntry()
[Fact] public async Task Handle_ZeroHours_ReturnsValidationError()
[Fact] public async Task Handle_NegativeHours_ReturnsValidationError()
[Fact] public async Task Handle_ClosedTicket_ReturnsBusinessError()
[Fact] public async Task Handle_AtLimit_WithoutPermission_ReturnsForbidden()
[Fact] public async Task Handle_AtLimit_WithPermission_Succeeds()
[Fact] public async Task Handle_ExceedsMaxHours_WithoutPermission_ReturnsForbidden()
[Fact] public async Task Handle_ExceedsMaxHours_WithPermission_Succeeds()
[Fact] public async Task Handle_NoSlaRule_CreatesAdHocDueDate()

// SlaService.CalculateDefaultExtensionHours Tests
[Fact] public void CalculateDefaultExtensionHours_Monday_ReturnsToTuesday4pm()
[Fact] public void CalculateDefaultExtensionHours_Friday_ReturnsToMonday4pm()
[Fact] public void CalculateDefaultExtensionHours_Saturday_ReturnsToMonday4pm()
[Fact] public void CalculateDefaultExtensionHours_NullTimezone_UsesUtc()
```

## API Reference

See [contracts/api.md](./contracts/api.md) for detailed API documentation.

## Troubleshooting

### Extension Not Showing

1. Check ticket status is not Closed or Resolved
2. Check user has permission to edit the ticket
3. Verify `CanEditTicket` returns true for current user

### Wrong Default Hours

1. Check organization timezone is set in Settings
2. Verify timezone is valid IANA format (e.g., "America/New_York")
3. Check current server time is correct

### Preview Not Updating

1. Ensure JavaScript is enabled
2. Check browser console for errors
3. Verify `/staff/tickets/{id}?handler=PreviewSlaExtension` endpoint is working

## Architecture Notes

- **Inline UI**: Extension happens within the SLA card, no modal dialog
- **Progressive Enhancement**: Form works without JS; JS adds live preview
- **Audit Trail**: All extensions logged via existing change log system
- **Permission Model**: Uses existing `ITicketPermissionService.CanManageTickets()`

