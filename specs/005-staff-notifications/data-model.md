# Data Model: Staff Notifications Center

**Feature**: 005-staff-notifications  
**Date**: 2026-01-23

## New Entities

### Notification

**File**: `src/App.Domain/Entities/Notification.cs`

Represents a recorded notification event for a staff user.

| Field | Type | Nullable | Default | Description |
|-------|------|----------|---------|-------------|
| `Id` | Guid | No | `Guid.NewGuid()` | Primary key |
| `RecipientUserId` | Guid | No | - | FK to User (the notification recipient) |
| `EventType` | string | No | - | Notification type (NotificationEventType.DeveloperName) |
| `Title` | string | No | - | Short title for display (max 200 chars) |
| `Message` | string | No | - | Full notification message (max 1000 chars) |
| `Url` | string? | Yes | null | Optional link to related resource |
| `TicketId` | long? | Yes | null | Optional FK to Ticket |
| `IsRead` | bool | No | false | Read/unread status |
| `CreatedAt` | DateTime | No | `DateTime.UtcNow` | When notification was created |
| `ReadAt` | DateTime? | Yes | null | When notification was marked as read |

**Entity Definition**:
```csharp
namespace App.Domain.Entities;

/// <summary>
/// Represents a notification event recorded for a staff user.
/// Notifications are created when events occur, regardless of user delivery preferences.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// The staff user who receives this notification.
    /// </summary>
    public Guid RecipientUserId { get; set; }
    public virtual User RecipientUser { get; set; } = null!;

    /// <summary>
    /// The type of notification event (from NotificationEventType).
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Short title for display in notification list.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Full notification message with details.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Optional URL to navigate to when notification is clicked.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional reference to the related ticket.
    /// </summary>
    public long? TicketId { get; set; }
    public virtual Ticket? Ticket { get; set; }

    /// <summary>
    /// Whether the notification has been read by the user.
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the notification was marked as read.
    /// Null if unread.
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
```

**Validation Rules**:
- `RecipientUserId` must reference a valid user
- `EventType` must be a valid NotificationEventType.DeveloperName
- `Title` max length: 200 characters
- `Message` max length: 1000 characters
- `Url` max length: 2000 characters
- `TicketId` if provided, must reference a valid ticket (soft constraint - ticket may be deleted)

**State Transitions**:
- **Created**: `IsRead = false`, `ReadAt = null`
- **Marked as Read**: `IsRead = true`, `ReadAt = DateTime.UtcNow`
- **Marked as Unread**: `IsRead = false`, `ReadAt = null`

## EF Core Configuration

**File**: `src/App.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs`

```csharp
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.Url)
            .HasMaxLength(2000);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Ticket)
            .WithMany()
            .HasForeignKey(n => n.TicketId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        // Primary query: user's unread notifications, newest first
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead_CreatedAt")
            .IsDescending(false, false, true);

        // Secondary: filter by type
        builder.HasIndex(n => new { n.RecipientUserId, n.EventType, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_RecipientUserId_EventType_CreatedAt")
            .IsDescending(false, false, true);
    }
}
```

## Database Migration

**Migration Name**: `AddNotifications`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Notifications",
        columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
            EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            TicketId = table.Column<long>(type: "bigint", nullable: true),
            IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Notifications", x => x.Id);
            table.ForeignKey(
                name: "FK_Notifications_Users_RecipientUserId",
                column: x => x.RecipientUserId,
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                name: "FK_Notifications_Tickets_TicketId",
                column: x => x.TicketId,
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        });

    migrationBuilder.CreateIndex(
        name: "IX_Notifications_RecipientUserId_IsRead_CreatedAt",
        table: "Notifications",
        columns: new[] { "RecipientUserId", "IsRead", "CreatedAt" });

    migrationBuilder.CreateIndex(
        name: "IX_Notifications_RecipientUserId_EventType_CreatedAt",
        table: "Notifications",
        columns: new[] { "RecipientUserId", "EventType", "CreatedAt" });

    migrationBuilder.CreateIndex(
        name: "IX_Notifications_TicketId",
        table: "Notifications",
        column: "TicketId");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "Notifications");
}
```

## DTO Definitions

### NotificationDto

**File**: `src/App.Application/Notifications/NotificationDto.cs`

```csharp
using CSharpVitamins;

namespace App.Application.Notifications;

/// <summary>
/// DTO for a notification in the notification center.
/// </summary>
public record NotificationDto
{
    public ShortGuid Id { get; init; }
    public ShortGuid RecipientUserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string EventTypeLabel { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Url { get; init; }
    public long? TicketId { get; init; }
    public string? TicketTitle { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedAtFormatted { get; init; } = string.Empty;
    public string CreatedAtRelative { get; init; } = string.Empty;
    public DateTime? ReadAt { get; init; }
}
```

### NotificationListItemDto

**File**: `src/App.Application/Notifications/NotificationDto.cs` (same file, additional record)

```csharp
/// <summary>
/// Simplified DTO for notification list display.
/// </summary>
public record NotificationListItemDto
{
    public ShortGuid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string EventTypeLabel { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Url { get; init; }
    public long? TicketId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedAtRelative { get; init; } = string.Empty;
}
```

## DbContext Changes

**File**: `src/App.Application/Common/Interfaces/IAppDbContext.cs`

Add to interface:
```csharp
DbSet<Notification> Notifications { get; }
```

**File**: `src/App.Infrastructure/Persistence/AppDbContext.cs`

Add to class:
```csharp
public DbSet<Notification> Notifications => Set<Notification>();
```

## Relationships Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                            User                                  │
├─────────────────────────────────────────────────────────────────┤
│ Id: Guid (PK)                                                    │
│ ...existing fields...                                            │
└─────────────────────────────────────────────────────────────────┘
         │
         │ 1:N (Cascade Delete)
         ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Notification                              │
├─────────────────────────────────────────────────────────────────┤
│ Id: Guid (PK)                                                    │
│ RecipientUserId: Guid (FK) ────────────────────┐                │
│ EventType: string                              │                │
│ Title: string                                  │                │
│ Message: string                                │                │
│ Url: string?                                   │                │
│ TicketId: long? (FK) ──────────────────────────┼───┐            │
│ IsRead: bool                                   │   │            │
│ CreatedAt: DateTime                            │   │            │
│ ReadAt: DateTime?                              │   │            │
└─────────────────────────────────────────────────┴───┼────────────┘
                                                      │
                                                      │ N:1 (SetNull on Delete)
                                                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                           Ticket                                 │
├─────────────────────────────────────────────────────────────────┤
│ Id: long (PK)                                                    │
│ ...existing fields...                                            │
└─────────────────────────────────────────────────────────────────┘
```

## Indexes

| Index Name | Columns | Purpose |
|------------|---------|---------|
| `IX_Notifications_RecipientUserId_IsRead_CreatedAt` | (RecipientUserId, IsRead, CreatedAt DESC) | Primary query: user's unread notifications |
| `IX_Notifications_RecipientUserId_EventType_CreatedAt` | (RecipientUserId, EventType, CreatedAt DESC) | Filter by notification type |
| `IX_Notifications_TicketId` | (TicketId) | FK index for ticket relationship |

## Constraints

| Constraint | Type | Description |
|------------|------|-------------|
| `FK_Notifications_Users_RecipientUserId` | Foreign Key | Must reference valid user; cascades on delete |
| `FK_Notifications_Tickets_TicketId` | Foreign Key | Optional; sets null on ticket delete |
| `Title` max 200 chars | Application | Enforced in command validation |
| `Message` max 1000 chars | Application | Enforced in command validation |

## Migration Strategy

1. Create table with all columns and indexes
2. No data migration needed (new table)
3. Start recording notifications after deployment
4. Existing notifications (pre-feature) won't appear - this is expected behavior

