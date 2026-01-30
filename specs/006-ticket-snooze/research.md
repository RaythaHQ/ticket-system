# Research: Ticket Snooze

**Feature Branch**: `006-ticket-snooze`  
**Date**: 2026-01-30

## Summary

This document captures research findings for implementing the Ticket Snooze feature, resolving all technical unknowns identified during planning.

---

## 1. Snooze Data Storage Approach

### Decision
Add snooze fields directly to the `Ticket` entity rather than creating a separate `TicketSnooze` entity.

### Rationale
- **Simpler queries**: Filtering snoozed tickets in views requires a single table scan, not a join
- **Consistency with existing patterns**: SLA fields (`SlaDueAt`, `SlaBreachedAt`, `SlaStatus`) are stored directly on Ticket
- **Atomic updates**: Snooze state changes are saved with other ticket updates in a single transaction
- **Performance**: Built-in views filter millions of tickets; avoiding joins is critical

### Alternatives Considered
- **Separate TicketSnooze entity**: More normalized but adds join overhead for every view query. Rejected for performance reasons.
- **Snooze history table**: Could track all snooze events, but changelog already provides this audit trail. Not needed.

---

## 2. Snooze Fields on Ticket Entity

### Decision
Add the following fields to `Ticket`:

| Field | Type | Purpose |
|-------|------|---------|
| `SnoozedUntil` | `DateTime?` | When the ticket should unsnooze (null = not snoozed) |
| `SnoozedAt` | `DateTime?` | When the snooze was initiated |
| `SnoozedById` | `Guid?` | User who snoozed the ticket |
| `SnoozedReason` | `string?` | Optional note explaining why ticket was snoozed |
| `UnsnoozedAt` | `DateTime?` | When the ticket was last unsnoozed (for "recently unsnoozed" indicator) |

### Rationale
- `SnoozedUntil` is the primary filter field - null means not snoozed
- `SnoozedAt` + `SnoozedById` provide audit context
- `UnsnoozedAt` enables the "recently unsnoozed" visual indicator (within 30 min)
- Matches the existing pattern of nullable DateTime fields on Ticket (e.g., `ResolvedAt`, `ClosedAt`)

---

## 3. View Condition: "Is Snoozed"

### Decision
Add `IsSnoozed` as a boolean filter condition following the existing `HasAttachments`/`HasContact` pattern.

### Implementation
1. Add to `FilterAttributes.All` in `FilterAttributeDefinition.cs`:
   ```csharp
   new()
   {
       Field = "IsSnoozed",
       Label = "Is Snoozed",
       Type = "boolean",
       Operators = OperatorDefinitions.BooleanOperators,
   }
   ```

2. Add case in `ViewFilterBuilder.BuildFilterBody()`:
   ```csharp
   "issnoozed" => BuildBooleanExpression(param, filter, 
       t => t.SnoozedUntil != null && t.SnoozedUntil > DateTime.UtcNow),
   ```

### Rationale
- Follows the exact pattern of existing boolean conditions
- Expression checks both that `SnoozedUntil` is set AND in the future
- Allows custom views to include/exclude snoozed tickets

---

## 4. Built-in Views Snooze Behavior

### Decision
- **Most built-in views**: Add `IsSnoozed = false` to default conditions, with UI checkbox to show snoozed
- **"All Tickets" view**: Show all tickets (no snooze filter), with dropdown to filter by snooze state
- **New "Snoozed" view**: Built-in view with `IsSnoozed = true` condition

### Implementation
1. Update `GetBuiltInViewConditionsAsync()` in `Index.cshtml.cs` to add snooze filter
2. Add UI toggle in view header (checkbox for most views, dropdown for "All Tickets")
3. Use query parameter `?showSnoozed=true` or `?snoozeFilter=all|snoozed|unsnoozed`

### Rationale
- Default behavior hides snoozed tickets for focused workflow
- Easy toggle for staff who need to see snoozed tickets
- "All Tickets" is comprehensive, so default includes everything

---

## 5. Notification Event Type

### Decision
Add new `NotificationEventType` values:
- `TICKET_UNSNOOZED` ("ticket_unsnoozed") - For auto-unsnooze and manual unsnooze by others

### Rationale
- Follows existing `NotificationEventType` value object pattern
- Single event type covers both scenarios (auto and manual unsnooze)
- Notification handler determines whether to send based on who triggered unsnooze

### Not Adding
- `TICKET_SNOOZED` - Not needed per spec (no notification when snoozing)

---

## 6. Background Job: Snooze Evaluation

### Decision
Create `SnoozeEvaluationJob` following the `SlaEvaluationJob` pattern.

### Implementation
- Runs every 5 minutes (configurable)
- Queries tickets where `SnoozedUntil <= DateTime.UtcNow`
- For each ticket:
  1. Clear snooze fields (`SnoozedUntil = null`)
  2. Set `UnsnoozedAt = DateTime.UtcNow`
  3. Add changelog entry
  4. Raise `TicketUnsnoozedEvent` domain event
- Process in batches of 100

### Rationale
- Matches existing job pattern exactly
- 5-minute interval acceptable per spec (SC-002: within 5 minutes)
- Domain event triggers notifications via event handler

---

## 7. SLA Pause During Snooze

### Decision
Pause SLA by extending `SlaDueAt` when ticket unsnoozes.

### Implementation
On unsnooze:
1. Calculate snooze duration: `UnsnoozedAt - SnoozedAt`
2. Extend SLA: `SlaDueAt = SlaDueAt + snoozeDuration`
3. Record extension in changelog

### Configuration
- Organization-level setting: `PauseSlaOnSnooze` (default: true)
- Stored in `OrganizationSettings` entity

### Rationale
- Simple approach: just extend the due date
- No need to track "paused time" separately
- Matches user expectation: if I snoozed for 2 hours, I get 2 hours back

---

## 8. Snooze Preset Calculations

### Decision
Use organization timezone and default business hours (9am-5pm) for presets.

### Presets
| Preset | Logic |
|--------|-------|
| Later Today | Now + 3 hours, or 9am tomorrow if less than 3 hours until 5pm |
| Tomorrow | 9am tomorrow in org timezone |
| In 3 Days | 9am in 3 calendar days |
| Next Week | 9am on next Monday (or first business day) |

### Implementation
- Use `ICurrentOrganization.TimeZone` for timezone
- Default business hours: 9am start (no org-level business hours config exists outside SLA rules)
- Calculate in organization timezone, store as UTC

### Rationale
- Simple defaults work for most organizations
- Organization timezone already exists
- Can enhance later if org-level business hours are needed

---

## 9. Environment Variable Configuration

### Decision
Add `SNOOZE_MAX_DURATION_DAYS` environment variable (default: 90).

### Implementation
Create `SnoozeConfiguration` class in Infrastructure:
```csharp
public class SnoozeConfiguration : ISnoozeConfiguration
{
    private readonly IConfiguration _configuration;
    
    public int MaxDurationDays => 
        int.TryParse(_configuration["SNOOZE_MAX_DURATION_DAYS"], out var days) 
            ? days : 90;
}
```

### Rationale
- Follows existing pattern (EmailerConfiguration, SecurityConfiguration)
- Simple env var with sensible default
- No database storage needed for this setting

---

## 10. Assignment Constraint Enforcement

### Decision
Enforce snooze-requires-assignee at command level, with auto-unsnooze on unassignment.

### Implementation
1. **SnoozeTicket command**: Validate `AssigneeId != null`, reject with error if not
2. **AssignTicket/UpdateTicket commands**: When changing assignee:
   - If new assignee is individual: preserve snooze
   - If new assignee is null or team-only: auto-unsnooze first
3. Auto-unsnooze sets `UnsnoozedAt`, clears snooze fields, adds changelog entry

### Rationale
- Validation at command boundary is clean and testable
- Auto-unsnooze on unassignment prevents orphaned snoozed tickets
- Changelog provides audit trail of why ticket was unsnoozed

---

## 11. Email Template

### Decision
Add new email template: `TicketUnsnoozedEmail` ("email_ticket_unsnoozed")

### Template Variables
- `TicketId`, `TicketTitle`, `TicketUrl`
- `UnsnoozedBy` (null if auto-unsnoozed)
- `SnoozedDuration`
- `RecipientName`

### Rationale
- Follows existing template pattern
- Provides context for why notification was sent

---

## 12. Migration Strategy

### Decision
Two migrations:
1. Schema migration: Add snooze fields to Ticket table
2. Data migration: Enable snooze notifications for all existing users

### Implementation
Schema migration:
```sql
ALTER TABLE "Tickets" ADD COLUMN "SnoozedUntil" timestamp with time zone;
ALTER TABLE "Tickets" ADD COLUMN "SnoozedAt" timestamp with time zone;
ALTER TABLE "Tickets" ADD COLUMN "SnoozedById" uuid;
ALTER TABLE "Tickets" ADD COLUMN "SnoozedReason" text;
ALTER TABLE "Tickets" ADD COLUMN "UnsnoozedAt" timestamp with time zone;
-- Index for background job query
CREATE INDEX "IX_Tickets_SnoozedUntil" ON "Tickets" ("SnoozedUntil") 
    WHERE "SnoozedUntil" IS NOT NULL;
```

Data migration:
```sql
-- Insert notification preferences for all users who don't have one
INSERT INTO "NotificationPreferences" ("Id", "StaffAdminId", "EventType", "EmailEnabled", "InAppEnabled")
SELECT gen_random_uuid(), "Id", 'ticket_unsnoozed', true, true
FROM "Users"
WHERE "Id" NOT IN (
    SELECT "StaffAdminId" FROM "NotificationPreferences" 
    WHERE "EventType" = 'ticket_unsnoozed'
);
```

### Rationale
- Index on `SnoozedUntil` WHERE NOT NULL is critical for background job performance
- Data migration ensures all users get notifications enabled by default

---

## Summary of Decisions

| Topic | Decision |
|-------|----------|
| Data storage | Fields on Ticket entity (no separate table) |
| View condition | `IsSnoozed` boolean following existing pattern |
| Built-in views | Default hide snoozed, with toggle |
| Notification | New `TICKET_UNSNOOZED` event type |
| Background job | `SnoozeEvaluationJob` every 5 min |
| SLA pause | Extend `SlaDueAt` on unsnooze |
| Presets | Org timezone, 9am default start |
| Config | `SNOOZE_MAX_DURATION_DAYS` env var |
| Assignment | Validate at command, auto-unsnooze on unassign |
| Email | New `TicketUnsnoozedEmail` template |
| Migration | Schema + data migration for defaults |
