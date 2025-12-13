# Quickstart: Ticket View CSV Export

**Feature Branch**: `002-ticket-enhancements`  
**Created**: 2025-12-13

---

## Prerequisites

- Base ticketing system (001-dme-staff-ticketing) substantially complete
- Ticket, TicketView, BackgroundTask, MediaItem entities working
- Staff area UI scaffold in place
- User with admin role and appropriate permissions

---

## Implementation Order

### Phase 1: Domain & Permission (Foundation)

1. **Add permission flag** to `SystemPermissions` enum
   - File: `src/App.Domain/Entities/Role.cs`
   - Add `ImportExportTickets = 512`
   - Update `BuiltInSystemPermission` class

2. **Create ExportJobStatus** value object
   - File: `src/App.Domain/ValueObjects/ExportJobStatus.cs`
   - Follow existing pattern (TicketStatus, BackgroundTaskStatus)

3. **Create ExportJob** entity
   - File: `src/App.Domain/Entities/ExportJob.cs`

### Phase 2: Persistence

4. **Add DbSet** to `IAppDbContext`
   - File: `src/App.Application/Common/Interfaces/IRaythaDbContext.cs`

5. **Implement DbSet** in `AppDbContext`
   - File: `src/App.Infrastructure/Persistence/AppDbContext.cs`

6. **Create entity configuration**
   - File: `src/App.Infrastructure/Persistence/Configurations/ExportJobConfiguration.cs`

7. **Create migration**
   ```bash
   dotnet ef migrations add AddExportJobs -p src/App.Infrastructure -s src/App.Web
   dotnet ef database update -p src/App.Infrastructure -s src/App.Web
   ```

### Phase 3: Application Layer

8. **Create DTOs**
   - `src/App.Application/Exports/ExportJobDto.cs`
   - `src/App.Application/Exports/Models/ExportSnapshotPayload.cs`

9. **Create Commands**
   - `src/App.Application/Exports/Commands/CreateExportJob.cs`
   - `src/App.Application/Exports/Commands/RetryExportJob.cs`

10. **Create Queries**
    - `src/App.Application/Exports/Queries/GetExportJobById.cs`
    - `src/App.Application/Exports/Queries/GetExportJobsForUser.cs`

11. **Create Background Job**
    - `src/App.Infrastructure/BackgroundTasks/TicketExportJob.cs`
    - Implements `IExecuteBackgroundTask`

12. **Create Cleanup Job**
    - `src/App.Infrastructure/BackgroundTasks/ExportCleanupJob.cs`
    - Extends `BackgroundService`

### Phase 4: Staff UI

13. **Add Export button** to ticket list page
    - File: `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml`
    - Conditionally show based on permission

14. **Create Export Status page**
    - Files: `src/App.Web/Areas/Staff/Pages/Exports/Status.cshtml[.cs]`
    - Polls for status, auto-downloads on completion

15. **Register routes**
    - Update `RouteNames.cs` for Staff area

### Phase 5: Integration & Cleanup

16. **Register services** in DI
    - File: `src/App.Infrastructure/ConfigureServices.cs`
    - Register `TicketExportJob`
    - Register `ExportCleanupJob` as hosted service

17. **Add CsvHelper** NuGet package
    ```bash
    dotnet add src/App.Infrastructure package CsvHelper
    ```

---

## Verification Steps

### 1. Permission Check
```sql
-- Verify permission flag was added
SELECT * FROM "Roles" WHERE ("SystemPermissions" & 512) = 512;
```

### 2. Create Test Export
1. Login as admin with ImportExportTickets permission
2. Navigate to Staff > Tickets
3. Click "Export to CSV"
4. Verify redirect to status page
5. Wait for completion
6. Verify CSV downloads

### 3. Permission Enforcement
1. Login as non-admin user WITH ImportExportTickets permission
2. Initiate export (should succeed)
3. Try to download (should fail - admin required)

### 4. Cleanup Verification
1. Create export
2. Manually set `ExpiresAt` to past timestamp
3. Wait for cleanup job to run (or trigger manually)
4. Verify file deleted and MediaItem marked as cleaned up

---

## Key Files Summary

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/ExportJob.cs` | Export job entity |
| Domain | `ValueObjects/ExportJobStatus.cs` | Status value object |
| Domain | `Entities/Role.cs` | Add permission flag |
| Application | `Exports/ExportJobDto.cs` | DTO for export job |
| Application | `Exports/Commands/CreateExportJob.cs` | Create export command |
| Application | `Exports/Queries/GetExportJobById.cs` | Get export status |
| Infrastructure | `BackgroundTasks/TicketExportJob.cs` | Export execution |
| Infrastructure | `BackgroundTasks/ExportCleanupJob.cs` | Cleanup expired exports |
| Infrastructure | `Configurations/ExportJobConfiguration.cs` | EF configuration |
| Web | `Areas/Staff/Pages/Exports/Status.cshtml` | Status UI |
| Web | `Areas/Staff/Pages/Tickets/Index.cshtml` | Add export button |

---

## Common Issues

### "Permission denied" on download
- Verify user is admin (`User.IsAdmin = true`)
- Verify user has ImportExportTickets permission via role

### Export job stuck in "Running"
- Check BackgroundTask table for errors
- Verify `QueuedHostedService` is running
- Check application logs for exceptions

### CSV file encoding issues
- Ensure UTF-8 with BOM for Excel compatibility
- Use `new UTF8Encoding(true)` when creating StreamWriter

### Memory issues on large exports
- Verify batch processing is working
- Check that CSV is streamed, not buffered entirely
- Consider reducing batch size from 1000

