# Feature Specification: Ticket View CSV Export

**Feature Branch**: `002-ticket-enhancements`  
**Created**: 2025-12-13  
**Status**: Draft  
**Extends**: [001-dme-staff-ticketing](../001-dme-staff-ticketing/spec.md)  
**Input**: Enhancement requirements for the DME Staff Ticketing System

---

## Overview

This specification defines a secure, permissioned CSV export capability for the existing DME Staff Ticketing System. Users with appropriate permissions can export ticket views to CSV format using background job processing, with progress tracking and secure file delivery via the existing MediaItem infrastructure.

**Key Objectives**:
- Enable secure, permissioned export of ticket data to CSV format
- Ensure data consistency via snapshot isolation (point-in-time accuracy)
- Provide progress visibility during long-running exports
- Integrate with existing MediaItem and File Storage Provider infrastructure
- Enforce strict access controls (Admin + permission required for download)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export Ticket View to CSV (Priority: P1) 🎯 MVP

A supervisor with "Can Import / Export Tickets" permission needs to export the current ticket view to CSV for external reporting. They click "Export to CSV", the system queues a background job, and the supervisor waits on a status screen showing progress. When complete, the file automatically downloads through the existing MediaItem download mechanism.

**Why this priority**: Data export is critical for compliance reporting, external stakeholder communication, and data analysis. This is the most requested enhancement.

**Independent Test**: Can be tested by exporting a view with known tickets, verifying the CSV contains expected data with correct column ordering, and confirming the file downloads securely.

**Acceptance Scenarios**:

1. **Given** a staff user with "Can Import / Export Tickets" permission viewing a ticket view, **When** they click "Export to CSV", **Then** an export job is created capturing the current view configuration (filters, sort, columns, search term) and they are redirected to the Export Status screen.

2. **Given** an export job is queued, **When** the background task processes it, **Then** the job reads tickets using snapshot isolation to ensure data consistency as of the request time, regardless of concurrent modifications.

3. **Given** an export job is running, **When** the user views the Export Status screen, **Then** they see the current status (Queued/Running/Completed/Failed) and progress indicator.

4. **Given** an export job completes successfully, **When** the user is on the Export Status screen, **Then** the CSV file automatically begins downloading via the existing MediaItem download mechanism.

5. **Given** a completed export, **When** a non-Admin user attempts to download the file (even if they initiated the export), **Then** access is denied.

6. **Given** a completed export, **When** an Admin user without "Can Import / Export Tickets" permission attempts to download, **Then** access is denied (both permissions required).

7. **Given** an export job fails, **When** the user views the Export Status screen, **Then** they see a safe-to-display error message and a "Retry" button that creates a new export job.

8. **Given** an export older than 72 hours, **When** the cleanup background task runs, **Then** the exported file is deleted from storage and the MediaItem is marked as expired/deleted.

---

### Edge Cases

- **Export of empty view**: Export succeeds with header row only, no data rows.
- **Export during system high load**: Job queues normally; may take longer but doesn't timeout.
- **Very large export (100k+ rows)**: Streaming approach handles without memory issues.
- **User loses permission after initiating export**: Download denied at download time (permissions checked at download, not request).
- **Concurrent exports by same user**: Allowed; each is an independent job.
- **Export view that no longer exists**: Job fails with clear error; retry not applicable.
- **Network interruption during download**: Standard browser retry applies; file remains available until expiry.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Permissions

- **FR-001**: System MUST add a new permission: "Can Import / Export Tickets".
- **FR-002**: Export UI actions (Export to CSV button) MUST require "Can Import / Export Tickets" permission.
- **FR-003**: Export file download MUST require user to be Admin AND have "Can Import / Export Tickets" permission.
- **FR-004**: Non-admin users MUST NOT be able to download exported files even if they initiated the export.

#### Export Initiation

- **FR-005**: System MUST provide "Export to CSV" action on ticket views for users with "Can Import / Export Tickets" permission.
- **FR-006**: System MUST capture view snapshot at request time including: view ID (if applicable), filters, search term, sort order, selected columns, column order, and scope (team/assigned-to/etc.).
- **FR-007**: System MUST capture requester user ID and requester's roles/permission state at request time for audit purposes.
- **FR-008**: Download permissions MUST be enforced at download time, not request time.

#### Background Processing

- **FR-009**: Export MUST run via the existing BackgroundTasks infrastructure as a queued job.
- **FR-010**: System MUST create an ExportJob record and enqueue a BackgroundTask to execute it.

#### Data Consistency (Snapshot Correctness)

- **FR-011**: Export dataset MUST reflect database state at the moment the export was requested ("as-of request" semantics).
- **FR-012**: Concurrent creates, updates, or deletes during job execution MUST NOT alter the exported dataset.
- **FR-013**: System MUST use snapshot isolation, repeatable read, or stable "as-of" cutoff strategy with keyset pagination to guarantee no drift/duplication/missing rows.
- **FR-014**: System MUST NOT use offset pagination for large exports.

#### CSV Generation

- **FR-015**: Output format MUST be CSV (not Excel).
- **FR-016**: CSV output MUST include header row with column labels corresponding to view columns.
- **FR-017**: CSV output MUST respect view column order and inclusion exactly.
- **FR-018**: CSV output MUST use proper escaping (quotes, commas, newlines).
- **FR-019**: CSV output MUST use UTF-8 encoding.
- **FR-020**: Export MUST stream rows to avoid loading entire dataset into memory.

#### Storage & Security via MediaItem

- **FR-021**: On completion, background job MUST create a MediaItem record with type/category indicating "Ticket Export".
- **FR-022**: On completion, background job MUST upload CSV file using existing File Storage Provider abstraction.
- **FR-023**: Export job MUST associate MediaItem to the job for retrieval.
- **FR-024**: Download MUST be performed through the existing secure MediaItem download mechanism.

#### Progress / Wait UI

- **FR-025**: After clicking Export, system MUST route user to an Export Status screen (or modal/page).
- **FR-026**: Export Status screen MUST show: Queued/Running/Completed/Failed status.
- **FR-027**: Export Status screen MUST show progress (stage-based minimum, optionally percentage).
- **FR-028**: When export completes, Status screen MUST automatically trigger file download via existing MediaItem download mechanism.
- **FR-029**: System MUST NOT create new public API endpoints; use existing server-rendered patterns for status display and MediaItem download.

#### Failure Handling

- **FR-030**: Failed exports MUST display safe-to-display error message on Status screen.
- **FR-031**: Status screen MUST provide "Retry" button for failed exports (permissioned) that creates a new export job.

#### Retention & Cleanup

- **FR-032**: System MUST implement retention policy: exports expire after defined window (72 hours).
- **FR-033**: Cleanup background task MUST delete expired files from storage provider.
- **FR-034**: Cleanup MUST mark/remove associated MediaItem record on expiry.
- **FR-035**: Cleanup MUST be idempotent on retries.

#### Auditing & Observability

- **FR-036**: System MUST create audit log entry for export requests including: who requested, timestamp, view snapshot summary, row count, success/failure, duration.
- **FR-037**: System MUST track failures with safe-to-display error message on status UI.

---

### Key Entities

#### ExportJob

Represents a ticket export operation initiated by a user.

**Key Attributes**:
- `Id` (long) - Primary identifier
- `RequesterUserId` (Guid) - Staff who requested the export
- `RequestedAt` (DateTime) - When export was requested
- `Status` (string) - Queued, Running, Completed, Failed
- `Progress` (string/JSON) - Stage and optional percentage
- `SnapshotPayload` (JSON) - View ID, filters, search, sort, columns, scope
- `RowCount` (int, nullable) - Number of rows exported (set on completion)
- `ErrorMessage` (string, nullable) - Safe-to-display error if failed
- `CompletedAt` (DateTime, nullable) - When job finished
- `MediaItemId` (Guid, nullable) - Reference to generated CSV file (null until complete)
- `ExpiresAt` (DateTime) - When export should be deleted (RequestedAt + 72 hours)

#### BackgroundTask Payload Schema

```json
{
  "type": "TicketExport",
  "exportJobId": 123,
  "snapshotPayload": {
    "viewId": "abc-123",
    "filters": [
      { "field": "status", "operator": "eq", "value": "open" }
    ],
    "searchTerm": "wheelchair",
    "sortField": "createdAt",
    "sortDirection": "desc",
    "columns": ["id", "title", "status", "priority", "contactName", "createdAt"],
    "scope": {
      "teamId": "team-456",
      "assignedToUserId": null
    }
  },
  "requestedAt": "2025-12-13T10:30:00Z",
  "requesterUserId": "user-789"
}
```

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Export of 10,000 tickets completes within 60 seconds.
- **SC-002**: Export data matches database state at request time with 100% accuracy (no drift from concurrent changes).
- **SC-003**: CSV export file can be opened without errors in Excel, Google Sheets, and standard CSV parsers.
- **SC-004**: Export cleanup removes 100% of expired exports within 1 hour of expiration.
- **SC-005**: Export audit log captures 100% of export operations with required metadata.
- **SC-006**: Non-admin users are blocked from download with 100% enforcement (server-side).
- **SC-007**: Memory usage during export stays constant regardless of export size (streaming verification).

---

## Permissions Model

### New Permission Flag

| Permission Flag | Description |
|-----------------|-------------|
| **Can Import / Export Tickets** | Initiate export jobs; download requires Admin + this permission |

### Export Download Access Matrix

| User Type | Has "Can Import/Export" Permission | Can Initiate Export? | Can Download Export? |
|-----------|-------------------------------------|----------------------|----------------------|
| Admin | Yes | ✓ | ✓ |
| Admin | No | ✗ | ✗ |
| Staff | Yes | ✓ | ✗ (Admin required) |
| Staff | No | ✗ | ✗ |

---

## Technical Notes: Snapshot Consistency Approach

### Recommended: Keyset Pagination with Timestamp Cutoff

The export feature requires point-in-time consistency to ensure the exported data reflects exactly what was visible when the user clicked "Export".

**Implementation Strategy**:

1. **Snapshot Cutoff**: At job creation, capture `RequestedAt` timestamp as the snapshot boundary.

2. **Keyset Pagination**: Use `WHERE Id > @lastId ORDER BY Id LIMIT @batchSize` pattern instead of `OFFSET/LIMIT` to:
   - Avoid performance degradation on large datasets
   - Ensure stable iteration even with concurrent modifications

3. **Filter by CreatedAt**: Only include tickets where `CreationTime <= RequestedAt` to exclude tickets created after export request.

4. **Ignore Soft Deletes After Request**: For tickets soft-deleted after `RequestedAt`, still include them (they existed at request time).

5. **Streaming**: Process in batches (e.g., 1000 rows), write to CSV stream, release memory between batches.

**Why not database-level snapshot isolation transaction?**: Exports may take minutes, and holding an open transaction for that duration risks connection timeouts and database resource pressure. The cutoff + keyset approach is more robust for long-running operations.

---

## UI Flows

### Export Flow

1. **Initiate Export**
   - User clicks "Export to CSV" on ticket view
   - System validates user has "Can Import / Export Tickets" permission
   - System creates ExportJob record with snapshot payload
   - System enqueues BackgroundTask
   - User is redirected to Export Status page

2. **Status/Progress Screen**
   - Page polls for job status (or uses existing SignalR if available)
   - Shows: Job ID, Status, Progress stage, Time elapsed
   - If Completed: Auto-triggers download via MediaItem download endpoint
   - If Failed: Shows error message, "Retry" button

3. **Download**
   - Download URL points to existing MediaItem download controller
   - Controller checks: User is Admin AND has "Can Import / Export Tickets"
   - If authorized: Streams file from storage provider
   - If unauthorized: Returns 403

### Cleanup Flow

1. BackgroundTask (scheduled, e.g., hourly) queries ExportJobs where `ExpiresAt < now` and not yet cleaned up
2. For each expired job:
   - Delete file from storage provider
   - Update MediaItem status to Deleted/Expired
   - Mark ExportJob as cleaned up
3. Idempotent: safe to run multiple times

---

## Assumptions

1. **MediaItem Infrastructure**: The existing MediaItem and File Storage Provider abstractions can store and serve CSV files without modification.

2. **Admin Concept**: The application has an existing concept of "Admin" users that can be checked in authorization logic.

3. **Background Tasks**: The existing BackgroundTasks infrastructure supports long-running jobs and can be extended with new job types.

4. **Progress Reporting**: Stage-based progress (Querying → Generating → Uploading) is sufficient; percentage progress is optional enhancement.

5. **Retention Policy**: 72 hours is appropriate; organization-configurable retention is a future enhancement.

6. **No Size Limits**: No hard limit on export size; streaming approach handles large exports.

---

## Open Questions

No critical questions remain that would block specification completion. All core behaviors have been defined.

---

## Appendix: Relationship to Base Specification

This specification extends `001-dme-staff-ticketing` with an additive enhancement. It does not modify or remove any existing functionality. The base specification's entities (Ticket, TicketView, etc.) remain unchanged; this spec adds the ExportJob entity and export capabilities that integrate with them.

**Dependency**: This spec requires the base ticketing system (001) to be substantially complete before implementation begins. Specifically:
- Ticket entity and queries
- TicketView and view filtering
- BackgroundTasks infrastructure
- MediaItem and File Storage Provider
- User with permission model
- Staff area UI scaffold
