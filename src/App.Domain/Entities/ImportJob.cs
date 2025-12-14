using App.Domain.ValueObjects;

namespace App.Domain.Entities;

public class ImportJob : BaseAuditableEntity
{
    // Request metadata
    public Guid RequesterUserId { get; set; }
    public virtual User? Requester { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    // Import configuration
    public ImportEntityType EntityType { get; set; } = ImportEntityType.Contacts;
    public ImportMode Mode { get; set; } = ImportMode.Upsert;
    public bool IsDryRun { get; set; }

    // Source file
    public Guid SourceMediaItemId { get; set; }
    public virtual MediaItem? SourceMediaItem { get; set; }

    // Status tracking
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Queued;
    public string? ProgressStage { get; set; }
    public int? ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Results
    public int TotalRows { get; set; }
    public int RowsProcessed { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsSkipped { get; set; }
    public int RowsWithErrors { get; set; }

    // Error file (if any validation errors)
    public Guid? ErrorMediaItemId { get; set; }
    public virtual MediaItem? ErrorMediaItem { get; set; }

    // Lifecycle
    public DateTime ExpiresAt { get; set; }
    public bool IsCleanedUp { get; set; }

    // Reference to background task
    public Guid? BackgroundTaskId { get; set; }
    public virtual BackgroundTask? BackgroundTask { get; set; }
}
