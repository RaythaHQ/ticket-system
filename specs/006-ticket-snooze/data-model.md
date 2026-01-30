# Data Model: Ticket Snooze

**Feature Branch**: `006-ticket-snooze`  
**Date**: 2026-01-30

## Overview

This document defines the data model changes required for the Ticket Snooze feature.

---

## Entity Changes

### Ticket (Extended)

Add the following fields to the existing `Ticket` entity:

```csharp
// Snooze fields
public DateTime? SnoozedUntil { get; set; }
public DateTime? SnoozedAt { get; set; }
public Guid? SnoozedById { get; set; }
public virtual User? SnoozedBy { get; set; }
public string? SnoozedReason { get; set; }
public DateTime? UnsnoozedAt { get; set; }
```

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `SnoozedUntil` | `DateTime?` | Yes | When the ticket should automatically unsnooze. Null if not snoozed. Stored as UTC. |
| `SnoozedAt` | `DateTime?` | Yes | When the snooze was initiated. Stored as UTC. |
| `SnoozedById` | `Guid?` | Yes | Foreign key to User who snoozed the ticket. |
| `SnoozedBy` | `User?` | Yes | Navigation property to the user who snoozed. |
| `SnoozedReason` | `string?` | Yes | Optional note explaining why the ticket was snoozed. Max 500 chars. |
| `UnsnoozedAt` | `DateTime?` | Yes | When the ticket was last unsnoozed. Used for "recently unsnoozed" indicator. |

#### Computed Property

```csharp
[NotMapped]
public bool IsSnoozed => SnoozedUntil != null && SnoozedUntil > DateTime.UtcNow;

[NotMapped]
public bool IsRecentlyUnsnoozed => UnsnoozedAt != null 
    && DateTime.UtcNow - UnsnoozedAt.Value < TimeSpan.FromMinutes(30);
```

---

### OrganizationSettings (Extended)

Add snooze configuration to existing `OrganizationSettings` entity:

```csharp
public bool PauseSlaOnSnooze { get; set; } = true;
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `PauseSlaOnSnooze` | `bool` | `true` | Whether SLA timers pause while a ticket is snoozed. |

---

### NotificationEventType (Extended)

Add new event type to the `NotificationEventType` value object:

```csharp
public const string TICKET_UNSNOOZED = "ticket_unsnoozed";

public static NotificationEventType TicketUnsnoozed => 
    new("Ticket Unsnoozed", TICKET_UNSNOOZED);
```

Update `SupportedTypes` to include the new type.

---

### BuiltInEmailTemplate (Extended)

Add new email template to the `BuiltInEmailTemplate` value object:

```csharp
public const string TICKET_UNSNOOZED_EMAIL = "email_ticket_unsnoozed";

public static BuiltInEmailTemplate TicketUnsnoozedEmail => 
    new("Ticket Unsnoozed", TICKET_UNSNOOZED_EMAIL);
```

Update `SupportedTypes` to include the new template.

---

## New Domain Events

### TicketSnoozedEvent

```csharp
public class TicketSnoozedEvent : IAfterSaveChangesNotification
{
    public Ticket Ticket { get; }
    public DateTime SnoozedUntil { get; }
    public Guid SnoozedById { get; }
    public string? Reason { get; }
    
    public TicketSnoozedEvent(Ticket ticket, DateTime snoozedUntil, 
        Guid snoozedById, string? reason = null)
    {
        Ticket = ticket;
        SnoozedUntil = snoozedUntil;
        SnoozedById = snoozedById;
        Reason = reason;
    }
}
```

### TicketUnsnoozedEvent

```csharp
public class TicketUnsnoozedEvent : IAfterSaveChangesNotification
{
    public Ticket Ticket { get; }
    public Guid? UnsnoozedById { get; }  // null if auto-unsnoozed
    public bool WasAutoUnsnooze { get; }
    public TimeSpan SnoozeDuration { get; }
    
    public TicketUnsnoozedEvent(Ticket ticket, Guid? unsnoozedById, 
        bool wasAutoUnsnooze, TimeSpan snoozeDuration)
    {
        Ticket = ticket;
        UnsnoozedById = unsnoozedById;
        WasAutoUnsnooze = wasAutoUnsnooze;
        SnoozeDuration = snoozeDuration;
    }
}
```

---

## Database Configuration

### TicketConfiguration (Extended)

Add to `TicketConfiguration.cs`:

```csharp
// Snooze fields
builder.Property(t => t.SnoozedUntil);
builder.Property(t => t.SnoozedAt);
builder.Property(t => t.SnoozedById);
builder.Property(t => t.SnoozedReason).HasMaxLength(500);
builder.Property(t => t.UnsnoozedAt);

// Snooze relationship
builder.HasOne(t => t.SnoozedBy)
    .WithMany()
    .HasForeignKey(t => t.SnoozedById)
    .OnDelete(DeleteBehavior.SetNull);

// Partial index for background job
builder.HasIndex(t => t.SnoozedUntil)
    .HasFilter("\"SnoozedUntil\" IS NOT NULL");
```

---

## Migration Script

### Schema Migration

```sql
-- Add snooze columns to Tickets table
ALTER TABLE "Tickets" ADD COLUMN "SnoozedUntil" timestamp with time zone;
ALTER TABLE "Tickets" ADD COLUMN "SnoozedAt" timestamp with time zone;
ALTER TABLE "Tickets" ADD COLUMN "SnoozedById" uuid;
ALTER TABLE "Tickets" ADD COLUMN "SnoozedReason" character varying(500);
ALTER TABLE "Tickets" ADD COLUMN "UnsnoozedAt" timestamp with time zone;

-- Foreign key for SnoozedBy
ALTER TABLE "Tickets" ADD CONSTRAINT "FK_Tickets_Users_SnoozedById" 
    FOREIGN KEY ("SnoozedById") REFERENCES "Users" ("Id") ON DELETE SET NULL;

-- Partial index for efficient background job queries
CREATE INDEX "IX_Tickets_SnoozedUntil" ON "Tickets" ("SnoozedUntil") 
    WHERE "SnoozedUntil" IS NOT NULL;

-- Add PauseSlaOnSnooze to OrganizationSettings
ALTER TABLE "OrganizationSettings" ADD COLUMN "PauseSlaOnSnooze" boolean NOT NULL DEFAULT true;
```

### Data Migration (Notification Preferences)

```sql
-- Enable snooze notifications for all existing users
INSERT INTO "NotificationPreferences" 
    ("Id", "StaffAdminId", "EventType", "EmailEnabled", "InAppEnabled", "WebhookEnabled")
SELECT 
    gen_random_uuid(), 
    u."Id", 
    'ticket_unsnoozed', 
    true, 
    true, 
    false
FROM "Users" u
WHERE NOT EXISTS (
    SELECT 1 FROM "NotificationPreferences" np 
    WHERE np."StaffAdminId" = u."Id" AND np."EventType" = 'ticket_unsnoozed'
);
```

---

## Entity Relationships Diagram

```
┌─────────────────────────────────────────────────────────┐
│                         Ticket                          │
├─────────────────────────────────────────────────────────┤
│ Id (long)                                               │
│ Title, Description, Status, Priority, ...              │
│ AssigneeId (Guid?) ─────────────────────┐              │
│ SnoozedUntil (DateTime?)                │              │
│ SnoozedAt (DateTime?)                   │              │
│ SnoozedById (Guid?) ────────────────────┼──────┐       │
│ SnoozedReason (string?)                 │      │       │
│ UnsnoozedAt (DateTime?)                 │      │       │
│ SlaDueAt (DateTime?)                    │      │       │
│ ...                                     │      │       │
└─────────────────────────────────────────┼──────┼───────┘
                                          │      │
                                          ▼      │
                                    ┌─────────┐  │
                                    │  User   │◄─┘
                                    ├─────────┤
                                    │ Id      │
                                    │ Name    │
                                    │ ...     │
                                    └─────────┘
```

---

## State Transitions

### Snooze State Machine

```
                    ┌──────────────────┐
                    │    Not Snoozed   │
                    │ (SnoozedUntil=∅) │
                    └────────┬─────────┘
                             │
              Snooze Command │
              (with future   │
               datetime)     │
                             ▼
                    ┌──────────────────┐
                    │     Snoozed      │
                    │(SnoozedUntil≠∅)  │
                    └───┬──────────┬───┘
                        │          │
         Auto-unsnooze  │          │ Manual unsnooze
         (job detects   │          │ or unassign/
          time passed)  │          │ close ticket
                        │          │
                        ▼          ▼
                    ┌──────────────────┐
                    │  Just Unsnoozed  │
                    │ (UnsnoozedAt set)│
                    └────────┬─────────┘
                             │
                   30 min    │
                   passes    │
                             ▼
                    ┌──────────────────┐
                    │    Not Snoozed   │
                    │ (normal state)   │
                    └──────────────────┘
```

### Snooze Field Values by State

| State | SnoozedUntil | SnoozedAt | SnoozedById | UnsnoozedAt |
|-------|--------------|-----------|-------------|-------------|
| Never snoozed | null | null | null | null |
| Currently snoozed | future DateTime | DateTime | Guid | null |
| Just unsnoozed (<30min) | null | null | null | DateTime |
| Unsnoozed (>30min ago) | null | null | null | DateTime (stale) |

---

## Indexes

| Index | Columns | Filter | Purpose |
|-------|---------|--------|---------|
| `IX_Tickets_SnoozedUntil` | `SnoozedUntil` | `WHERE SnoozedUntil IS NOT NULL` | Background job query |
| (existing) | `AssigneeId` | - | View filtering |
| (existing) | `Status` | - | View filtering |
