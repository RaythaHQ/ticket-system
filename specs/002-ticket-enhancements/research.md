# Research: Ticket View CSV Export

**Feature Branch**: `002-ticket-enhancements`  
**Created**: 2025-12-13

---

## Technical Decisions

### 1. Background Task Integration

**Decision**: Use existing `IBackgroundTaskQueue` and `IExecuteBackgroundTask` infrastructure.

**Rationale**: 
- The codebase has a mature background task system using PostgreSQL with `FOR UPDATE SKIP LOCKED` for job dequeuing
- Jobs implement `IExecuteBackgroundTask.Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)`
- Status tracking via `BackgroundTaskStatus` (Enqueued, Processing, Complete, Error)
- Progress tracking via `PercentComplete` and `StatusInfo` fields

**Implementation**:
```csharp
public class TicketExportJob : IExecuteBackgroundTask
{
    public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
    {
        // Parse args to get exportJobId
        // Load ExportJob, process tickets, generate CSV, upload to storage
    }
}
```

**Alternatives Considered**:
- Hangfire: Rejected - adds external dependency when existing infrastructure is sufficient
- Direct async processing: Rejected - need persistence for progress tracking and retry

---

### 2. File Storage Integration

**Decision**: Use existing `IFileStorageProvider` abstraction with `SaveAndGetDownloadUrlAsync`.

**Rationale**:
- Three providers already implemented: Local, S3, Azure Blob
- MediaItem entity tracks: FileName, Length, ContentType, FileStorageProvider, ObjectKey
- Existing download mechanism via MediaItem controller/endpoints

**Implementation**:
- Generate unique ObjectKey: `exports/{exportJobId}/{timestamp}-tickets.csv`
- Content type: `text/csv; charset=utf-8`
- Create MediaItem record after upload completes
- Link MediaItem to ExportJob via `MediaItemId` foreign key

**Alternatives Considered**:
- Separate export file storage: Rejected - reuse existing infrastructure
- Stream directly to user: Rejected - long-running operations need background processing

---

### 3. Permission Model

**Decision**: Add new `ImportExportTickets` permission to `SystemPermissions` flags enum.

**Rationale**:
- Existing permission model uses `[Flags]` enum with bitwise operations
- Role-based with permissions checked via `SystemPermissions.HasFlag()`
- Admin check via `User.IsAdmin` boolean property

**Implementation**:
```csharp
// In SystemPermissions enum (next power of 2 after ManageSystemViews = 256)
ImportExportTickets = 512,

// In BuiltInSystemPermission
public const string IMPORT_EXPORT_TICKETS_PERMISSION = "import_export_tickets";
public static BuiltInSystemPermission ImportExportTickets =>
    new("Import / Export Tickets", IMPORT_EXPORT_TICKETS_PERMISSION, SystemPermissions.ImportExportTickets);
```

**Download Authorization Logic**:
```csharp
bool canDownload = user.IsAdmin && 
    user.Roles.Any(r => r.SystemPermissions.HasFlag(SystemPermissions.ImportExportTickets));
```

**Alternatives Considered**:
- Separate permission table: Rejected - existing flags enum is consistent with codebase patterns
- Reuse AccessReports: Rejected - export access is distinct from report viewing

---

### 4. Snapshot Consistency Strategy

**Decision**: Use CreationTime cutoff with keyset pagination.

**Rationale**:
- Database transaction-level snapshot isolation holds connection too long for large exports
- Keyset pagination (`WHERE Id > @lastId ORDER BY Id`) avoids offset performance degradation
- CreationTime filter (`WHERE CreationTime <= @requestedAt`) ensures point-in-time consistency

**Implementation**:
```csharp
var cutoffTime = exportJob.RequestedAt;
long lastId = 0;
const int batchSize = 1000;

while (true)
{
    var batch = await _db.Tickets
        .Where(t => t.Id > lastId && t.CreationTime <= cutoffTime && !t.IsDeleted)
        .OrderBy(t => t.Id)
        .Take(batchSize)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
    
    if (batch.Count == 0) break;
    
    // Process batch, write to CSV stream
    lastId = batch.Last().Id;
}
```

**Soft Delete Handling**: 
- For tickets deleted AFTER `RequestedAt`, include them (use `IgnoreQueryFilters()` with manual check)
- For tickets deleted BEFORE `RequestedAt`, exclude them

**Alternatives Considered**:
- Database snapshot isolation transaction: Rejected - connection timeouts for long exports
- Offset pagination: Rejected - O(n²) performance on large datasets
- Change data capture: Rejected - over-engineering for this use case

---

### 5. CSV Generation

**Decision**: Use streaming approach with `StreamWriter` and `CsvHelper` library (or manual escaping).

**Rationale**:
- Must not load entire dataset into memory (FR-020)
- Proper CSV escaping required for quotes, commas, newlines (FR-018)
- UTF-8 encoding with BOM for Excel compatibility

**Implementation**:
```csharp
using var memoryStream = new MemoryStream();
using var streamWriter = new StreamWriter(memoryStream, new UTF8Encoding(true)); // BOM for Excel
using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

// Write header
foreach (var column in columns)
{
    csvWriter.WriteField(column.Label);
}
csvWriter.NextRecord();

// Write rows in batches
while (var batch = await GetNextBatch(...))
{
    foreach (var ticket in batch)
    {
        foreach (var column in columns)
        {
            csvWriter.WriteField(GetColumnValue(ticket, column));
        }
        csvWriter.NextRecord();
    }
    await streamWriter.FlushAsync();
}
```

**Alternatives Considered**:
- StringBuilder: Rejected - memory accumulation for large exports
- Excel format (.xlsx): Rejected - spec explicitly requires CSV (FR-015)

---

### 6. Progress Tracking

**Decision**: Stage-based progress with optional row-count percentage.

**Rationale**:
- Spec requires stage-based minimum (FR-027)
- Exact percentage requires knowing total row count upfront (extra query)

**Stages**:
1. `Queued` - Job created, waiting for worker
2. `Counting` - Getting total row count (optional)
3. `Exporting` - Processing batches (can show X of Y if count known)
4. `Uploading` - Uploading to file storage
5. `Completed` / `Failed`

**Implementation**:
- Update `BackgroundTask.StatusInfo` with current stage
- Update `BackgroundTask.PercentComplete` based on stage or rows processed

---

### 7. Cleanup Strategy

**Decision**: Scheduled background service running hourly.

**Rationale**:
- ExportJob has `ExpiresAt` field (RequestedAt + 72 hours)
- Must delete file from storage AND mark MediaItem as deleted
- Idempotent: safe to run multiple times

**Implementation**:
```csharp
public class ExportCleanupJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredExports(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

---

### 8. UI Status Page

**Decision**: Server-rendered Razor Page with polling (no new API endpoints).

**Rationale**:
- Spec requires no new public API endpoints (FR-029)
- Existing Staff area uses Razor Pages pattern
- Simple polling via meta refresh or JavaScript `setInterval`

**Implementation**:
- Route: `/staff/exports/{exportJobId}`
- Poll every 2-3 seconds while status is Queued/Running
- On Completed: JavaScript triggers download via existing MediaItem download endpoint
- On Failed: Show error message + retry button

---

## Dependencies

| Dependency | Purpose | Status |
|------------|---------|--------|
| CsvHelper (NuGet) | CSV generation with proper escaping | **NEW - Add to project** |
| Existing BackgroundTask infrastructure | Job queuing and execution | ✅ Available |
| Existing MediaItem + FileStorageProvider | File storage and download | ✅ Available |
| Existing SystemPermissions enum | Permission model | ✅ Available (extend) |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Large export causes memory pressure | Medium | High | Streaming approach with batch processing |
| Concurrent exports overload system | Low | Medium | No hard limit, but job queue naturally throttles |
| Cleanup fails to delete files | Low | Medium | Idempotent cleanup, retry on next run |
| Export takes too long | Medium | Low | Progress UI shows status; no hard timeout |

