# Tasks: Ticket View CSV Export

**Feature Branch**: `002-ticket-enhancements`  
**Input**: Design documents from `/specs/002-ticket-enhancements/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, quickstart.md ✓

**Tests**: Tests are NOT explicitly requested in the specification. Test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

---

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 for this feature)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and dependency additions

- [x] T001 Add CsvHelper NuGet package to src/App.Infrastructure/App.Infrastructure.csproj
- [x] T002 [P] Verify CsvHelper package restores correctly with `dotnet restore`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, value objects, and persistence that MUST be complete before user story implementation

**⚠️ CRITICAL**: User story work cannot begin until this phase is complete

### Domain Layer

- [x] T003 Add ImportExportTickets permission flag (512) to SystemPermissions enum in src/App.Domain/Entities/Role.cs
- [x] T004 Add ImportExportTickets to BuiltInSystemPermission class in src/App.Domain/Entities/Role.cs
- [x] T005 Update BuiltInSystemPermission.Permissions property to yield ImportExportTickets in src/App.Domain/Entities/Role.cs
- [x] T006 Update BuiltInSystemPermission.AllPermissionsAsEnum to include ImportExportTickets in src/App.Domain/Entities/Role.cs
- [x] T007 Update BuiltInSystemPermission.From(SystemPermissions) to handle ImportExportTickets flag in src/App.Domain/Entities/Role.cs
- [x] T008 [P] Create ExportJobStatus value object in src/App.Domain/ValueObjects/ExportJobStatus.cs
- [x] T009 Create ExportJob entity in src/App.Domain/Entities/ExportJob.cs

### Persistence Layer

- [x] T010 Add ExportJobs DbSet to IAppDbContext interface in src/App.Application/Common/Interfaces/IRaythaDbContext.cs
- [x] T011 Implement ExportJobs DbSet in AppDbContext in src/App.Infrastructure/Persistence/AppDbContext.cs
- [x] T012 Create ExportJobConfiguration in src/App.Infrastructure/Persistence/Configurations/ExportJobConfiguration.cs
- [x] T013 Create EF migration: `dotnet ef migrations add AddExportJobs -p src/App.Infrastructure -s src/App.Web`
- [x] T014 Apply migration: `dotnet ef database update -p src/App.Infrastructure -s src/App.Web`

**Checkpoint**: Foundation ready - User Story 1 implementation can now begin

---

## Phase 3: User Story 1 - Export Ticket View to CSV (Priority: P1) 🎯 MVP

**Goal**: Staff with "Can Import / Export Tickets" permission can export ticket views to CSV via background job, with progress tracking and secure admin-only download

**Independent Test**: 
1. Login as admin with ImportExportTickets permission
2. Navigate to Staff > Tickets view
3. Click "Export to CSV"
4. Verify redirect to status page showing progress
5. Wait for completion, verify CSV downloads
6. Verify non-admin users cannot download

### Application Layer - DTOs & Models

- [x] T015 [P] [US1] Create ExportSnapshotPayload model in src/App.Application/Exports/Models/ExportSnapshotPayload.cs
- [x] T016 [P] [US1] Create ExportFilter record in src/App.Application/Exports/Models/ExportSnapshotPayload.cs
- [x] T017 [P] [US1] Create ExportScope record in src/App.Application/Exports/Models/ExportSnapshotPayload.cs
- [x] T018 [P] [US1] Create ExportJobDto in src/App.Application/Exports/ExportJobDto.cs

### Application Layer - Commands

- [x] T019 [US1] Create CreateExportJob command with validator and handler in src/App.Application/Exports/Commands/CreateExportJob.cs
- [x] T020 [P] [US1] Create RetryExportJob command in src/App.Application/Exports/Commands/RetryExportJob.cs

### Application Layer - Queries

- [x] T021 [P] [US1] Create GetExportJobById query in src/App.Application/Exports/Queries/GetExportJobById.cs
- [x] T022 [P] [US1] Create GetExportJobsForUser query in src/App.Application/Exports/Queries/GetExportJobsForUser.cs

### Background Jobs

- [x] T023 [US1] Create TicketExportBackgroundTask in src/App.Infrastructure/BackgroundTasks/TicketExportBackgroundTask.cs
- [x] T024 [US1] Implement keyset pagination with timestamp cutoff in TicketExportBackgroundTask for snapshot consistency
- [x] T025 [US1] Implement streaming CSV generation with CsvHelper in TicketExportBackgroundTask
- [x] T026 [US1] Implement file upload to FileStorageProvider and MediaItem creation in TicketExportBackgroundTask
- [x] T027 [US1] Implement progress stage updates (Counting, Exporting, Uploading) in TicketExportBackgroundTask
- [x] T028 [P] [US1] Create ExportCleanupJob as BackgroundService in src/App.Infrastructure/BackgroundTasks/ExportCleanupJob.cs
- [x] T029 [US1] Implement hourly cleanup of expired exports (ExpiresAt < now) in ExportCleanupJob

### Service Registration

- [x] T030 [US1] Register TicketExportJob in DI container in src/App.Infrastructure/ConfigureServices.cs
- [x] T031 [P] [US1] Register ExportCleanupJob as hosted service in src/App.Infrastructure/ConfigureServices.cs

### Staff UI - Export Button

- [x] T032 [US1] Add "Export to CSV" button to ticket list page in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml
- [x] T033 [US1] Add permission check for export button visibility (ImportExportTickets) in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs
- [x] T034 [US1] Add POST handler for export initiation in src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs

### Staff UI - Export Status Page

- [x] T035 [US1] Create Exports folder in src/App.Web/Areas/Staff/Pages/Exports/
- [x] T036 [US1] Create Export Status page in src/App.Web/Areas/Staff/Pages/Exports/Status.cshtml
- [x] T037 [US1] Create Export Status PageModel in src/App.Web/Areas/Staff/Pages/Exports/Status.cshtml.cs
- [x] T038 [US1] Implement status polling (JavaScript setInterval) in Status.cshtml
- [x] T039 [US1] Implement auto-download trigger on completion in Status.cshtml
- [x] T040 [US1] Implement retry button for failed exports in Status.cshtml
- [x] T041 [US1] Implement error message display for failed exports in Status.cshtml

### Download Authorization

- [ ] T042 [US1] Add download authorization check (IsAdmin AND ImportExportTickets) to MediaItem download flow
- [ ] T043 [US1] Create or update MediaItem download endpoint to check export-specific permissions in src/App.Web/Areas/Admin/Endpoints/MediaItemsEndpoints.cs

### Route Configuration

- [x] T044 [P] [US1] Add export routes to RouteNames.cs in src/App.Web/Areas/Staff/Pages/Shared/RouteNames.cs

### Audit Logging

- [ ] T045 [US1] Add audit log entry creation for export requests in CreateExportJob handler
- [ ] T046 [US1] Add audit log entry for export completion/failure in TicketExportBackgroundTask

**Checkpoint**: User Story 1 complete - CSV export fully functional with permission enforcement

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Improvements and verification

- [ ] T047 [P] Add export job status to Staff area navigation (optional)
- [x] T048 [P] Verify export button hidden for users without ImportExportTickets permission (implemented in Index.cshtml.cs)
- [x] T049 [P] Verify download denied for non-admin users (implemented in Status.cshtml.cs CanDownload check)
- [ ] T050 [P] Verify CSV opens correctly in Excel, Google Sheets
- [ ] T051 [P] Verify cleanup job removes expired exports
- [x] T052 Code cleanup and remove any TODO comments
- [ ] T053 Run quickstart.md verification steps

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS User Story 1
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **Polish (Phase 4)**: Depends on User Story 1 completion

### Within Phase 2 (Foundational)

```
T003-T007 (permission changes) can run in sequence (same file)
T008 (ExportJobStatus) can run in parallel with T003-T007
T009 (ExportJob entity) depends on T008
T010-T012 (persistence) depend on T009
T013-T014 (migration) depend on T012
```

### Within Phase 3 (User Story 1)

```
Parallel Group 1: T015-T018 (DTOs/models) - different files
Parallel Group 2: T019-T022 (commands/queries) - after DTOs
Parallel Group 3: T023-T029 (background jobs) - after commands
Parallel Group 4: T030-T031 (registration) - after jobs
Parallel Group 5: T032-T034 (export button) - after registration
Parallel Group 6: T035-T041 (status page) - after export button
Sequential: T042-T043 (download auth) - after status page
Parallel: T044-T046 (routes/audit) - after status page
```

### Parallel Opportunities per Phase

**Phase 2 (Foundational)**:
```
Parallel: T008 with T003-T007
Sequential: Everything else (dependencies on same files or previous tasks)
```

**Phase 3 (User Story 1)**:
```
Parallel Group 1: T015, T016, T017, T018 (all different files)
Parallel Group 2: T019 with T020, T021, T022 (different files)
Parallel Group 3: T028 with T023-T027 (different files)
Parallel Group 4: T030 with T031 (different registration areas)
Parallel Group 5: T044 with T032-T034 (different files)
Parallel Group 6: T045 with T046 (different phases of audit)
```

---

## Parallel Example: User Story 1 DTOs

```bash
# Launch all DTO/model tasks together:
Task: "Create ExportSnapshotPayload model in src/App.Application/Exports/Models/ExportSnapshotPayload.cs"
Task: "Create ExportJobDto in src/App.Application/Exports/ExportJobDto.cs"

# After DTOs complete, launch commands/queries together:
Task: "Create CreateExportJob command in src/App.Application/Exports/Commands/CreateExportJob.cs"
Task: "Create RetryExportJob command in src/App.Application/Exports/Commands/RetryExportJob.cs"
Task: "Create GetExportJobById query in src/App.Application/Exports/Queries/GetExportJobById.cs"
Task: "Create GetExportJobsForUser query in src/App.Application/Exports/Queries/GetExportJobsForUser.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (add CsvHelper package)
2. Complete Phase 2: Foundational (permission, entities, persistence)
3. Complete Phase 3: User Story 1 (full export functionality)
4. **STOP and VALIDATE**: Test export flow end-to-end
5. Complete Phase 4: Polish and verification

### Incremental Delivery

| Delivery | Phases | Value Delivered |
|----------|--------|-----------------|
| MVP | 1 + 2 + 3 | Full CSV export with permissions |
| Release | + 4 | Polished, verified, production-ready |

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 53 |
| **Setup Tasks** | 2 |
| **Foundational Tasks** | 12 |
| **US1 Tasks** | 32 |
| **Polish Tasks** | 7 |
| **Parallel Tasks [P]** | 22 (42%) |

**Suggested MVP Scope**: Complete Phases 1-3 (46 tasks) for full export functionality

---

## Notes

- [P] tasks can run in parallel (different files, no dependencies)
- [US1] labels map tasks to User Story 1 (the only story in this feature)
- ExportJob uses GUID ID (standard for this codebase)
- Permission check pattern: `user.IsAdmin && SystemPermissions.HasFlag(ImportExportTickets)`
- CSV streaming is critical for memory efficiency
- Keyset pagination required (no offset) per spec
- Commit after each task or logical group

