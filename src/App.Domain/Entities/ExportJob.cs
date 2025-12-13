using App.Domain.ValueObjects;

namespace App.Domain.Entities;

public class ExportJob : BaseAuditableEntity
{
    // Request metadata
    public Guid RequesterUserId { get; set; }
    public virtual User? Requester { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    // Status tracking
    public ExportJobStatus Status { get; set; } = ExportJobStatus.Queued;
    public string? ProgressStage { get; set; }
    public int? ProgressPercent { get; set; }
    public int? RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Snapshot payload (JSON)
    public string SnapshotPayloadJson { get; set; } = "{}";
    
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

