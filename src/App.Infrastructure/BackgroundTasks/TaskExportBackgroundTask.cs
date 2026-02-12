using System.Globalization;
using System.Text;
using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Exports.Commands;
using App.Application.Exports.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background task that processes task exports using snapshot-consistent queries.
/// </summary>
public class TaskExportBackgroundTask : TaskExportJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskExportBackgroundTask> _logger;

    public TaskExportBackgroundTask(
        IServiceProvider serviceProvider,
        ILogger<TaskExportBackgroundTask> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task Execute(
        Guid jobId,
        JsonElement args,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageProvider>();

        if (!args.TryGetProperty("ExportJobId", out var exportJobIdElement))
        {
            _logger.LogError("TaskExportBackgroundTask: Missing ExportJobId in args");
            return;
        }

        var exportJobId = exportJobIdElement.GetGuid();
        var exportJob = await db.ExportJobs.FirstOrDefaultAsync(
            e => e.Id == exportJobId, cancellationToken);

        if (exportJob == null)
        {
            _logger.LogError("TaskExportBackgroundTask: ExportJob {ExportJobId} not found", exportJobId);
            return;
        }

        try
        {
            await ProcessExportAsync(db, fileStorage, exportJob, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TaskExportBackgroundTask: Export failed for job {ExportJobId}", exportJobId);

            exportJob.Status = ExportJobStatus.Failed;
            exportJob.ErrorMessage = GetSafeErrorMessage(ex);
            exportJob.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessExportAsync(
        IAppDbContext db,
        IFileStorageProvider fileStorage,
        ExportJob exportJob,
        CancellationToken cancellationToken)
    {
        exportJob.Status = ExportJobStatus.Running;
        exportJob.ProgressStage = "Preparing query";
        exportJob.ProgressPercent = 0;
        await db.SaveChangesAsync(cancellationToken);

        var payload = JsonSerializer.Deserialize<TaskExportSnapshotPayload>(
            exportJob.SnapshotPayloadJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize task export snapshot payload");

        var cutoffTime = exportJob.RequestedAt;

        var query = BuildQuery(db, payload, cutoffTime);

        exportJob.ProgressStage = "Counting records";
        exportJob.ProgressPercent = 10;
        await db.SaveChangesAsync(cancellationToken);

        var totalCount = await query.CountAsync(cancellationToken);

        exportJob.ProgressStage = "Generating CSV";
        exportJob.ProgressPercent = 20;
        await db.SaveChangesAsync(cancellationToken);

        var csvBytes = await GenerateCsvAsync(db, payload, cutoffTime, totalCount, async (progress) =>
        {
            exportJob.ProgressPercent = 20 + (int)(progress * 60);
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        exportJob.ProgressStage = "Uploading file";
        exportJob.ProgressPercent = 85;
        await db.SaveChangesAsync(cancellationToken);

        var fileName = $"task-export-{exportJob.RequestedAt:yyyyMMdd-HHmmss}.csv";
        var objectKey = $"exports/{exportJob.Id}/{fileName}";
        var contentType = "text/csv";

        await fileStorage.SaveAndGetDownloadUrlAsync(
            csvBytes, objectKey, fileName, contentType, exportJob.ExpiresAt, inline: false);

        exportJob.ProgressStage = "Creating media record";
        exportJob.ProgressPercent = 95;
        await db.SaveChangesAsync(cancellationToken);

        var mediaItem = new MediaItem
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            FileStorageProvider = fileStorage.GetName(),
            ObjectKey = objectKey,
            Length = csvBytes.Length,
        };

        db.MediaItems.Add(mediaItem);
        await db.SaveChangesAsync(cancellationToken);

        exportJob.Status = ExportJobStatus.Completed;
        exportJob.ProgressStage = "Completed";
        exportJob.ProgressPercent = 100;
        exportJob.RowCount = totalCount;
        exportJob.MediaItemId = mediaItem.Id;
        exportJob.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "TaskExportBackgroundTask: Export {ExportJobId} completed. Rows: {RowCount}",
            exportJob.Id, totalCount);
    }

    private IQueryable<TicketTask> BuildQuery(
        IAppDbContext db,
        TaskExportSnapshotPayload payload,
        DateTime cutoffTime)
    {
        var query = db.TicketTasks
            .Include(t => t.Ticket)
            .Include(t => t.Assignee)
            .Include(t => t.OwningTeam)
            .Include(t => t.DependsOnTask)
            .Include(t => t.CreatedByStaff)
            .Include(t => t.ClosedByStaff)
            .AsNoTracking()
            .Where(t => t.CreationTime <= cutoffTime);

        // Apply view filter
        var view = payload.BuiltInView?.ToLower() ?? "my-tasks";
        switch (view)
        {
            case "my-tasks":
                if (payload.RequestingUserId.HasValue)
                    query = query.Where(t => t.AssigneeId == payload.RequestingUserId.Value);
                break;

            case "team-tasks":
                if (payload.RequestingUserId.HasValue)
                {
                    var teamIds = db.TeamMemberships
                        .AsNoTracking()
                        .Where(m => m.StaffAdminId == payload.RequestingUserId.Value)
                        .Select(m => m.TeamId)
                        .ToList();
                    query = query.Where(t => t.OwningTeamId != null && teamIds.Contains(t.OwningTeamId.Value));
                }
                break;

            case "unassigned":
                query = query.Where(t => t.AssigneeId == null && t.OwningTeamId == null);
                break;

            case "created-by-me":
                if (payload.RequestingUserId.HasValue)
                    query = query.Where(t => t.CreatedByStaffId == payload.RequestingUserId.Value);
                break;

            case "overdue":
                var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                query = query.Where(t => t.Status == TicketTaskStatus.OPEN
                    && t.DueAt != null && t.DueAt < nowUtc);
                break;

            case "all":
                // No view filter
                break;
        }

        // Apply status filter
        if (payload.StatusFilter != "all" && view != "overdue")
        {
            query = query.Where(t => t.Status != TicketTaskStatus.CLOSED);
        }

        // Hide blocked tasks from non-All views (unless searching)
        if (view != "all" && string.IsNullOrWhiteSpace(payload.SearchTerm))
        {
            query = query.Where(t =>
                t.DependsOnTaskId == null
                || t.DependsOnTask!.Status == TicketTaskStatus.CLOSED);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(payload.SearchTerm))
        {
            var searchLower = payload.SearchTerm.ToLower().Trim();
            long? searchTicketId = null;
            if (long.TryParse(payload.SearchTerm.TrimStart('#'), out var parsedId))
            {
                searchTicketId = parsedId;
            }

            query = query.Where(t =>
                t.Title.ToLower().Contains(searchLower)
                || t.Ticket.Title.ToLower().Contains(searchLower)
                || (searchTicketId.HasValue && t.TicketId == searchTicketId.Value));
        }

        // Apply sorting
        var sortField = payload.SortField?.ToLower() ?? "default";
        var sortDesc = payload.SortDirection?.ToLower() == "desc";

        query = sortField switch
        {
            "newest" => query.OrderByDescending(t => t.CreationTime).ThenBy(t => t.Id),
            "oldest" => query.OrderBy(t => t.CreationTime).ThenBy(t => t.Id),
            "dueat" => sortDesc
                ? query.OrderByDescending(t => t.DueAt).ThenBy(t => t.Id)
                : query.OrderBy(t => t.DueAt).ThenBy(t => t.Id),
            "title" => sortDesc
                ? query.OrderByDescending(t => t.Title).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Title).ThenBy(t => t.Id),
            "ticket" => sortDesc
                ? query.OrderByDescending(t => t.TicketId).ThenBy(t => t.Id)
                : query.OrderBy(t => t.TicketId).ThenBy(t => t.Id),
            "assignee" => query.OrderBy(t => t.Assignee != null ? t.Assignee.FirstName : "")
                .ThenByDescending(t => t.CreationTime),
            "status" => query.OrderBy(t => t.Status).ThenByDescending(t => t.CreationTime),
            _ => view switch
            {
                "overdue" => query.OrderBy(t => t.DueAt).ThenBy(t => t.Id),
                "all" => query.OrderByDescending(t => t.CreationTime).ThenBy(t => t.Id),
                _ => query.OrderBy(t => t.DueAt ?? DateTime.MaxValue)
                    .ThenByDescending(t => t.CreationTime),
            }
        };

        return query;
    }

    private async Task<byte[]> GenerateCsvAsync(
        IAppDbContext db,
        TaskExportSnapshotPayload payload,
        DateTime cutoffTime,
        int totalCount,
        Func<double, Task> progressCallback,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, new UTF8Encoding(true));
        await using var csv = new CsvWriter(
            writer,
            new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        // Write header
        foreach (var (_, label) in Columns)
        {
            csv.WriteField(label);
        }
        await csv.NextRecordAsync();

        // Stream rows in batches
        const int batchSize = 1000;
        var query = BuildQuery(db, payload, cutoffTime);
        var processedCount = 0;

        // Use offset pagination since TicketTask uses Guid keys (not sequential)
        var allTasks = await query.ToListAsync(cancellationToken);

        foreach (var task in allTasks)
        {
            WriteTaskRow(csv, task);
            await csv.NextRecordAsync();

            processedCount++;
            if (totalCount > 0 && processedCount % batchSize == 0)
            {
                await progressCallback((double)processedCount / totalCount);
            }
        }

        await writer.FlushAsync(cancellationToken);
        return memoryStream.ToArray();
    }

    private void WriteTaskRow(CsvWriter csv, TicketTask task)
    {
        var nowUtc = DateTime.UtcNow;

        foreach (var (key, _) in Columns)
        {
            var value = key switch
            {
                "task_title" => task.Title,
                "task_status" => task.Status == TicketTaskStatus.OPEN ? "Open" : "Closed",
                "is_blocked" => (task.DependsOnTaskId != null
                    && task.DependsOnTask?.Status != TicketTaskStatus.CLOSED) ? "Yes" : "No",
                "is_overdue" => (task.Status == TicketTaskStatus.OPEN
                    && task.DueAt.HasValue && task.DueAt.Value < nowUtc) ? "Yes" : "No",
                "assignee" => task.Assignee != null
                    ? $"{task.Assignee.FirstName} {task.Assignee.LastName}".Trim()
                    : "",
                "team" => task.OwningTeam?.Name ?? "",
                "due_at" => task.DueAt?.ToString("O") ?? "",
                "depends_on" => task.DependsOnTask?.Title ?? "",
                "created_by" => task.CreatedByStaff != null
                    ? $"{task.CreatedByStaff.FirstName} {task.CreatedByStaff.LastName}".Trim()
                    : "",
                "created_at" => task.CreationTime.ToString("O"),
                "closed_at" => task.ClosedAt?.ToString("O") ?? "",
                "closed_by" => task.ClosedByStaff != null
                    ? $"{task.ClosedByStaff.FirstName} {task.ClosedByStaff.LastName}".Trim()
                    : "",
                "ticket_id" => $"#{task.TicketId}",
                "ticket_title" => task.Ticket?.Title ?? "",
                "ticket_priority" => task.Ticket?.Priority ?? "",
                "ticket_status" => task.Ticket?.Status ?? "",
                _ => "",
            };
            csv.WriteField(value);
        }
    }

    /// <summary>
    /// Column definitions for the task CSV export.
    /// </summary>
    private static readonly List<(string Key, string Label)> Columns = new()
    {
        ("ticket_id", "Ticket ID"),
        ("ticket_title", "Ticket Title"),
        ("ticket_priority", "Ticket Priority"),
        ("ticket_status", "Ticket Status"),
        ("task_title", "Task Title"),
        ("task_status", "Task Status"),
        ("is_blocked", "Blocked"),
        ("is_overdue", "Overdue"),
        ("assignee", "Assignee"),
        ("team", "Team"),
        ("due_at", "Due At"),
        ("depends_on", "Depends On"),
        ("created_by", "Created By"),
        ("created_at", "Created At"),
        ("closed_at", "Closed At"),
        ("closed_by", "Closed By"),
    };

    private string GetSafeErrorMessage(Exception ex)
    {
        return ex switch
        {
            InvalidOperationException => "Export configuration error. Please try again.",
            TimeoutException => "Export timed out. Please try with a smaller dataset.",
            _ => "An error occurred during export. Please try again or contact support.",
        };
    }
}
