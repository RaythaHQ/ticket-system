# Data Model: Ticket View CSV Export

**Feature Branch**: `002-ticket-enhancements`  
**Created**: 2025-12-13

---

## New Entities

### ExportJob

Represents a ticket export operation initiated by a user.

**Location**: `src/App.Domain/Entities/ExportJob.cs`

```csharp
public class ExportJob : BaseAuditableEntity
{
    // Request metadata
    public Guid RequesterUserId { get; set; }
    public virtual User Requester { get; set; }
    public DateTime RequestedAt { get; set; }
    
    // Status tracking
    public ExportJobStatus Status { get; set; } = ExportJobStatus.Queued;
    public string? ProgressStage { get; set; }  // "Queued", "Counting", "Exporting", "Uploading"
    public int? ProgressPercent { get; set; }
    public int? RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Snapshot payload (JSON)
    public string SnapshotPayloadJson { get; set; } = null!;
    
    // Result
    public Guid? MediaItemId { get; set; }
    public virtual MediaItem? MediaItem { get; set; }
    
    // Lifecycle
    public DateTime ExpiresAt { get; set; }
    public bool IsCleanedUp { get; set; }
    
    // Reference to background task
    public Guid? BackgroundTaskId { get; set; }
    public virtual BackgroundTask? BackgroundTask { get; set; }
}
```

**Relationships**:
- `Requester` → User (many-to-one)
- `MediaItem` → MediaItem (one-to-one, nullable)
- `BackgroundTask` → BackgroundTask (one-to-one, nullable)

---

### ExportJobStatus (Value Object)

**Location**: `src/App.Domain/ValueObjects/ExportJobStatus.cs`

```csharp
public class ExportJobStatus : ValueObject
{
    private ExportJobStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static ExportJobStatus Queued => new("Queued", "queued");
    public static ExportJobStatus Running => new("Running", "running");
    public static ExportJobStatus Completed => new("Completed", "completed");
    public static ExportJobStatus Failed => new("Failed", "failed");

    public string Label { get; private set; }
    public string DeveloperName { get; private set; }

    public static IEnumerable<ExportJobStatus> SupportedTypes
    {
        get
        {
            yield return Queued;
            yield return Running;
            yield return Completed;
            yield return Failed;
        }
    }

    // Standard From(), implicit/explicit operators, etc.
}
```

---

### ExportSnapshotPayload (Model)

Represents the captured view state at export request time.

**Location**: `src/App.Application/Exports/Models/ExportSnapshotPayload.cs`

```csharp
public record ExportSnapshotPayload
{
    public Guid? ViewId { get; init; }
    public List<ExportFilter> Filters { get; init; } = new();
    public string? SearchTerm { get; init; }
    public string? SortField { get; init; }
    public string? SortDirection { get; init; }
    public List<string> Columns { get; init; } = new();
    public ExportScope? Scope { get; init; }
}

public record ExportFilter
{
    public string Field { get; init; } = null!;
    public string Operator { get; init; } = null!;
    public string? Value { get; init; }
}

public record ExportScope
{
    public Guid? TeamId { get; init; }
    public Guid? AssignedToUserId { get; init; }
}
```

---

## Modified Entities

### SystemPermissions Enum

**Location**: `src/App.Domain/Entities/Role.cs`

**Add new flag**:
```csharp
[Flags]
public enum SystemPermissions
{
    None = 0,
    ManageSystemSettings = 1,
    ManageAuditLogs = 2,
    ManageAdministrators = 4,
    ManageTemplates = 8,
    ManageUsers = 16,
    ManageTeams = 32,
    ManageTickets = 64,
    AccessReports = 128,
    ManageSystemViews = 256,
    ImportExportTickets = 512,  // NEW
}
```

### BuiltInSystemPermission

**Location**: `src/App.Domain/Entities/Role.cs`

**Add new permission constant and static property**:
```csharp
public const string IMPORT_EXPORT_TICKETS_PERMISSION = "import_export_tickets";

public static BuiltInSystemPermission ImportExportTickets =>
    new("Import / Export Tickets", IMPORT_EXPORT_TICKETS_PERMISSION, SystemPermissions.ImportExportTickets);

// Update Permissions property to include new permission
// Update AllPermissionsAsEnum to include new permission
// Update From(SystemPermissions) to handle new flag
```

---

## Entity Configuration

### ExportJobConfiguration

**Location**: `src/App.Infrastructure/Persistence/Configurations/ExportJobConfiguration.cs`

```csharp
public class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.RequesterUserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => new { e.ExpiresAt, e.IsCleanedUp });
        
        // Relationships
        builder.HasOne(e => e.Requester)
            .WithMany()
            .HasForeignKey(e => e.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.MediaItem)
            .WithMany()
            .HasForeignKey(e => e.MediaItemId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(e => e.BackgroundTask)
            .WithMany()
            .HasForeignKey(e => e.BackgroundTaskId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Value object conversion
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
        
        // JSON column
        builder.Property(e => e.SnapshotPayloadJson)
            .HasColumnType("jsonb");
    }
}
```

---

## Database Changes

### New Table: ExportJobs

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| Id | uuid | No | Primary key |
| RequesterUserId | uuid | No | FK → Users.Id |
| RequestedAt | timestamp | No | |
| Status | varchar(50) | No | queued/running/completed/failed |
| ProgressStage | varchar(100) | Yes | |
| ProgressPercent | int | Yes | |
| RowCount | int | Yes | Set on completion |
| ErrorMessage | text | Yes | |
| CompletedAt | timestamp | Yes | |
| SnapshotPayloadJson | jsonb | No | |
| MediaItemId | uuid | Yes | FK → MediaItems.Id |
| ExpiresAt | timestamp | No | RequestedAt + 72 hours |
| IsCleanedUp | boolean | No | Default false |
| BackgroundTaskId | uuid | Yes | FK → BackgroundTasks.Id |
| CreationTime | timestamp | No | |
| LastModificationTime | timestamp | Yes | |
| CreatorUserId | uuid | Yes | |
| LastModifierUserId | uuid | Yes | |

**Indexes**:
- `IX_ExportJobs_RequesterUserId`
- `IX_ExportJobs_Status`
- `IX_ExportJobs_ExpiresAt_IsCleanedUp` (for cleanup query)

---

## IAppDbContext Extension

**Location**: `src/App.Application/Common/Interfaces/IRaythaDbContext.cs`

```csharp
// Add to interface
DbSet<ExportJob> ExportJobs { get; }
```

**Location**: `src/App.Infrastructure/Persistence/AppDbContext.cs`

```csharp
// Add implementation
public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
```

---

## Migration

```bash
dotnet ef migrations add AddExportJobs -p src/App.Infrastructure -s src/App.Web
```

