# Implementation Plan: Ticket View CSV Export

**Branch**: `002-ticket-enhancements` | **Date**: 2025-12-13 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/002-ticket-enhancements/spec.md`

---

## Summary

This feature adds secure, permissioned CSV export capability for ticket views. Users with the new "Import / Export Tickets" permission can initiate exports; downloads require Admin + permission. Exports run as background jobs using existing BackgroundTasks infrastructure, with progress tracking via a status page. Completed exports are stored via the existing MediaItem/FileStorageProvider infrastructure and automatically cleaned up after 72 hours.

**Technical Approach**: 
- Keyset pagination with timestamp cutoff for snapshot consistency
- Streaming CSV generation to handle large datasets without memory pressure
- Integration with existing infrastructure (BackgroundTasks, MediaItem, FileStorageProvider)
- Server-rendered Razor Pages for status UI (no new API endpoints)

---

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: ASP.NET Core, Entity Framework Core, Mediator, FluentValidation, CsvHelper (new)  
**Storage**: PostgreSQL, existing FileStorageProvider (Local/S3/Azure)  
**Testing**: xUnit (existing test projects)  
**Target Platform**: Linux server (Docker)  
**Project Type**: Web application (Clean Architecture)  
**Performance Goals**: 10,000 tickets exported in <60 seconds  
**Constraints**: Constant memory usage regardless of export size (streaming)  
**Scale/Scope**: Single new entity (ExportJob), ~15 new files

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Clean Architecture & Dependency Rule | ✅ PASS | Domain → Application → Infrastructure → Web flow maintained |
| CQRS & Mediator-Driven Use Cases | ✅ PASS | CreateExportJob, GetExportJobById use Mediator pattern |
| Razor Pages First, Minimal JavaScript | ✅ PASS | Status page uses server rendering with polling; auto-download via minimal JS |
| Explicit Data Access | ✅ PASS | All DB access via IAppDbContext; async with CancellationToken |
| Security & Observability | ✅ PASS | Permission checks in handlers; audit logging; structured logs |
| GUID vs ShortGuid Pattern | ✅ PASS | ExportJob uses GUID internally, ShortGuid in DTOs |
| BuiltIn Value Objects Pattern | ✅ PASS | ExportJobStatus follows existing ValueObject pattern |

**No constitution violations detected.**

---

## Project Structure

### Documentation (this feature)

```text
specs/002-ticket-enhancements/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Technical research
├── data-model.md        # Entity definitions
├── quickstart.md        # Implementation guide
├── checklists/
│   └── requirements.md  # Quality checklist
└── tasks.md             # Implementation tasks (via /speckit.tasks)
```

### Source Code (new/modified files)

```text
src/App.Domain/
├── Entities/
│   ├── ExportJob.cs                    # NEW: Export job entity
│   └── Role.cs                         # MODIFY: Add ImportExportTickets permission
└── ValueObjects/
    └── ExportJobStatus.cs              # NEW: Status value object

src/App.Application/
├── Common/Interfaces/
│   └── IRaythaDbContext.cs             # MODIFY: Add ExportJobs DbSet
└── Exports/                            # NEW: Feature folder
    ├── ExportJobDto.cs
    ├── Models/
    │   └── ExportSnapshotPayload.cs
    ├── Commands/
    │   ├── CreateExportJob.cs
    │   └── RetryExportJob.cs
    └── Queries/
        ├── GetExportJobById.cs
        └── GetExportJobsForUser.cs

src/App.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs                 # MODIFY: Add ExportJobs DbSet
│   ├── Configurations/
│   │   └── ExportJobConfiguration.cs   # NEW: EF configuration
│   └── Migrations/
│       └── *_AddExportJobs.cs          # NEW: Migration
├── BackgroundTasks/
│   ├── TicketExportJob.cs              # NEW: Export execution job
│   └── ExportCleanupJob.cs             # NEW: Cleanup background service
└── ConfigureServices.cs                # MODIFY: Register new services

src/App.Web/
└── Areas/Staff/
    └── Pages/
        ├── Tickets/
        │   └── Index.cshtml            # MODIFY: Add export button
        ├── Exports/                    # NEW: Export status pages
        │   ├── Status.cshtml
        │   └── Status.cshtml.cs
        └── Shared/
            └── RouteNames.cs           # MODIFY: Add export routes
```

**Structure Decision**: Follows existing Clean Architecture with feature-based organization. New `Exports` feature folder in Application layer. Background jobs in Infrastructure layer following existing patterns.

---

## Phase Summary

| Phase | Focus | Key Outputs |
|-------|-------|-------------|
| 0 | Research | [research.md](./research.md) - Technical decisions |
| 1 | Design | [data-model.md](./data-model.md), [quickstart.md](./quickstart.md) |
| 2 | Tasks | [tasks.md](./tasks.md) - Implementation checklist (via /speckit.tasks) |

---

## Integration Points

### BackgroundTasks Infrastructure

**Pattern**: Implement `IExecuteBackgroundTask` interface
```csharp
public class TicketExportJob : IExecuteBackgroundTask
{
    public async Task Execute(Guid jobId, JsonElement args, CancellationToken ct) { ... }
}
```

**Registration**: Add to DI in `ConfigureServices.cs`
```csharp
services.AddScoped<TicketExportJob>();
```

### MediaItem / FileStorageProvider

**Upload Pattern**:
```csharp
var objectKey = $"exports/{exportJobId}/{timestamp}-tickets.csv";
await _fileStorageProvider.SaveAndGetDownloadUrlAsync(
    data, objectKey, "export.csv", "text/csv", expiresAt);
```

**Download**: Use existing MediaItem download controller with added permission check

### Permission System

**New Flag**: `SystemPermissions.ImportExportTickets = 512`

**Check Pattern**:
```csharp
bool canExport = user.Roles.Any(r => 
    r.SystemPermissions.HasFlag(SystemPermissions.ImportExportTickets));
bool canDownload = canExport && user.IsAdmin;
```

---

## Estimated Effort

| Component | Effort | Notes |
|-----------|--------|-------|
| Domain (entities, value objects) | 0.5 day | Straightforward, follows patterns |
| Persistence (config, migration) | 0.5 day | Standard EF Core setup |
| Application (commands, queries) | 1 day | CreateExportJob is the main logic |
| Background Jobs | 1.5 days | Export job with streaming, cleanup job |
| Staff UI (status page, export button) | 1 day | Simple polling page |
| Permission integration | 0.5 day | Add flag, update checks |
| Testing & verification | 1 day | Manual testing, edge cases |

**Total**: ~6 days

---

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | - | - |

---

## Next Steps

Run `/speckit.tasks` to generate the implementation task list from this plan.
