using System.Globalization;
using System.Text;
using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Exports.Commands;
using App.Application.Exports.Models;
using App.Application.TicketViews;
using App.Application.TicketViews.Services;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background task that processes ticket exports using snapshot-consistent queries.
/// </summary>
public class TicketExportBackgroundTask : TicketExportJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketExportBackgroundTask> _logger;

    public TicketExportBackgroundTask(
        IServiceProvider serviceProvider,
        ILogger<TicketExportBackgroundTask> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task Execute(
        Guid jobId,
        JsonElement args,
        CancellationToken cancellationToken
    )
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageProvider>();

        // Parse export job ID from args
        if (!args.TryGetProperty("ExportJobId", out var exportJobIdElement))
        {
            _logger.LogError("TicketExportBackgroundTask: Missing ExportJobId in args");
            return;
        }

        var exportJobId = exportJobIdElement.GetGuid();
        var exportJob = await db.ExportJobs.FirstOrDefaultAsync(
            e => e.Id == exportJobId,
            cancellationToken
        );

        if (exportJob == null)
        {
            _logger.LogError(
                "TicketExportBackgroundTask: ExportJob {ExportJobId} not found",
                exportJobId
            );
            return;
        }

        try
        {
            await ProcessExportAsync(db, fileStorage, exportJob, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TicketExportBackgroundTask: Export failed for job {ExportJobId}",
                exportJobId
            );

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
        CancellationToken cancellationToken
    )
    {
        // Update status to Running
        exportJob.Status = ExportJobStatus.Running;
        exportJob.ProgressStage = "Preparing query";
        exportJob.ProgressPercent = 0;
        await db.SaveChangesAsync(cancellationToken);

        // Parse snapshot payload
        var snapshotPayload = JsonSerializer.Deserialize<ExportSnapshotPayload>(
            exportJob.SnapshotPayloadJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (snapshotPayload == null)
        {
            throw new InvalidOperationException("Failed to deserialize snapshot payload");
        }

        // Capture the request time as the cutoff for snapshot consistency
        var cutoffTime = exportJob.RequestedAt;

        // Build the query with snapshot consistency (keyset pagination with timestamp cutoff)
        var query = BuildSnapshotQuery(db, snapshotPayload, cutoffTime);

        // Update progress
        exportJob.ProgressStage = "Counting records";
        exportJob.ProgressPercent = 10;
        await db.SaveChangesAsync(cancellationToken);

        var totalCount = await query.CountAsync(cancellationToken);

        // Generate CSV
        exportJob.ProgressStage = "Generating CSV";
        exportJob.ProgressPercent = 20;
        await db.SaveChangesAsync(cancellationToken);

        var csvBytes = await GenerateCsvAsync(
            db,
            snapshotPayload,
            cutoffTime,
            totalCount,
            async (progress) =>
            {
                exportJob.ProgressPercent = 20 + (int)(progress * 60); // 20-80%
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        // Upload to file storage
        exportJob.ProgressStage = "Uploading file";
        exportJob.ProgressPercent = 85;
        await db.SaveChangesAsync(cancellationToken);

        var fileName = $"ticket-export-{exportJob.RequestedAt:yyyyMMdd-HHmmss}.csv";
        var objectKey = $"exports/{exportJob.Id}/{fileName}";
        var contentType = "text/csv";

        await fileStorage.SaveAndGetDownloadUrlAsync(
            csvBytes,
            objectKey,
            fileName,
            contentType,
            exportJob.ExpiresAt,
            inline: false
        );

        // Create MediaItem
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

        // Complete the export job
        exportJob.Status = ExportJobStatus.Completed;
        exportJob.ProgressStage = "Completed";
        exportJob.ProgressPercent = 100;
        exportJob.RowCount = totalCount;
        exportJob.MediaItemId = mediaItem.Id;
        exportJob.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "TicketExportBackgroundTask: Export {ExportJobId} completed successfully. Rows: {RowCount}",
            exportJob.Id,
            totalCount
        );
    }

    private IQueryable<Ticket> BuildSnapshotQuery(
        IAppDbContext db,
        ExportSnapshotPayload payload,
        DateTime cutoffTime
    )
    {
        // Base query with snapshot consistency: only include tickets created before cutoff
        var query = db
            .Tickets.Include(t => t.Contact)
            .Include(t => t.Assignee)
            .Include(t => t.OwningTeam)
            .Include(t => t.CreatedByStaff)
            .AsNoTracking()
            .Where(t => t.CreationTime <= cutoffTime);

        // Apply scope filtering
        if (payload.Scope != null)
        {
            if (payload.Scope.TeamId.HasValue)
            {
                query = query.Where(t => t.OwningTeamId == payload.Scope.TeamId.Value);
            }
            if (payload.Scope.AssignedToUserId.HasValue)
            {
                query = query.Where(t => t.AssigneeId == payload.Scope.AssignedToUserId.Value);
            }
        }

        // Apply filters using ViewFilterBuilder pattern
        if (payload.Filters.Any())
        {
            var filterBuilder = new ViewFilterBuilder(db);
            var conditions = new ViewConditions
            {
                Filters = payload
                    .Filters.Select(f => new ViewFilterCondition
                    {
                        Field = f.Field,
                        Operator = f.Operator,
                        Value = f.Value,
                    })
                    .ToList(),
            };
            query = filterBuilder.ApplyFilters(query, conditions);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(payload.SearchTerm) && payload.Columns.Any())
        {
            var filterBuilder = new ViewFilterBuilder(db);
            query = filterBuilder.ApplyColumnSearch(query, payload.SearchTerm, payload.Columns);
        }

        // Apply sorting (use keyset pagination approach for consistency)
        var sortField = payload.SortField?.ToLower() ?? "id";
        var sortDesc = payload.SortDirection?.ToLower() == "desc";

        query = sortField switch
        {
            "creationtime" => sortDesc
                ? query.OrderByDescending(t => t.CreationTime).ThenBy(t => t.Id)
                : query.OrderBy(t => t.CreationTime).ThenBy(t => t.Id),
            "title" => sortDesc
                ? query.OrderByDescending(t => t.Title).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Title).ThenBy(t => t.Id),
            "status" => sortDesc
                ? query.OrderByDescending(t => t.Status).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Status).ThenBy(t => t.Id),
            "priority" => sortDesc
                ? query.OrderByDescending(t => t.Priority).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Priority).ThenBy(t => t.Id),
            "lastmodificationtime" => sortDesc
                ? query.OrderByDescending(t => t.LastModificationTime).ThenBy(t => t.Id)
                : query.OrderBy(t => t.LastModificationTime).ThenBy(t => t.Id),
            _ => sortDesc ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id),
        };

        return query;
    }

    private async Task<byte[]> GenerateCsvAsync(
        IAppDbContext db,
        ExportSnapshotPayload payload,
        DateTime cutoffTime,
        int totalCount,
        Func<double, Task> progressCallback,
        CancellationToken cancellationToken
    )
    {
        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, new UTF8Encoding(true));
        await using var csv = new CsvWriter(
            writer,
            new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }
        );

        // Write header row based on columns
        var columns = payload.Columns.Any() ? payload.Columns : GetDefaultColumns();
        foreach (var column in columns)
        {
            csv.WriteField(GetColumnLabel(column));
        }
        await csv.NextRecordAsync();

        // Stream rows using batched keyset pagination
        const int batchSize = 1000;
        var query = BuildSnapshotQuery(db, payload, cutoffTime);
        var processedCount = 0;
        long? lastId = null;

        while (true)
        {
            var batchQuery = lastId.HasValue ? query.Where(t => t.Id > lastId.Value) : query;

            var batch = await batchQuery.Take(batchSize).ToListAsync(cancellationToken);

            if (!batch.Any())
                break;

            foreach (var ticket in batch)
            {
                WriteTicketRow(csv, ticket, columns);
                await csv.NextRecordAsync();
                lastId = ticket.Id;
            }

            processedCount += batch.Count;
            if (totalCount > 0)
            {
                await progressCallback((double)processedCount / totalCount);
            }
        }

        await writer.FlushAsync(cancellationToken);
        return memoryStream.ToArray();
    }

    private void WriteTicketRow(CsvWriter csv, Ticket ticket, List<string> columns)
    {
        foreach (var column in columns)
        {
            var value = GetColumnValue(ticket, column);
            csv.WriteField(value);
        }
    }

    private string GetColumnValue(Ticket ticket, string column)
    {
        return column.ToLower() switch
        {
            "id" => ticket.Id.ToString(),
            "title" => ticket.Title,
            "description" => ticket.Description ?? "",
            "status" => ticket.Status,
            "priority" => ticket.Priority,
            "category" => ticket.Category ?? "",
            "slastatus" => ticket.SlaStatus ?? "",
            "sladueat" => ticket.SlaDueAt?.ToString("O") ?? "",
            "contactname" => ticket.Contact?.FullName ?? "",
            "contactemail" => ticket.Contact?.Email ?? "",
            "assigneename" => ticket.Assignee != null
                ? $"{ticket.Assignee.FirstName} {ticket.Assignee.LastName}".Trim()
                : "",
            "teamname" => ticket.OwningTeam?.Name ?? "",
            "createdbystaff" => ticket.CreatedByStaff != null
                ? $"{ticket.CreatedByStaff.FirstName} {ticket.CreatedByStaff.LastName}".Trim()
                : "",
            "creationtime" => ticket.CreationTime.ToString("O"),
            "lastmodificationtime" => ticket.LastModificationTime?.ToString("O") ?? "",
            "resolvedat" => ticket.ResolvedAt?.ToString("O") ?? "",
            "closedat" => ticket.ClosedAt?.ToString("O") ?? "",
            "tags" => ticket.Tags != null ? string.Join(", ", ticket.Tags) : "",
            _ => "",
        };
    }

    private string GetColumnLabel(string column)
    {
        return column.ToLower() switch
        {
            "id" => "ID",
            "title" => "Title",
            "description" => "Description",
            "status" => "Status",
            "priority" => "Priority",
            "category" => "Category",
            "slastatus" => "SLA Status",
            "sladueat" => "SLA Due At",
            "contactname" => "Contact Name",
            "contactemail" => "Contact Email",
            "assigneename" => "Assignee",
            "teamname" => "Team",
            "createdbystaff" => "Created By",
            "creationtime" => "Created At",
            "lastmodificationtime" => "Last Modified",
            "resolvedat" => "Resolved At",
            "closedat" => "Closed At",
            "tags" => "Tags",
            _ => column,
        };
    }

    private List<string> GetDefaultColumns()
    {
        return new List<string>
        {
            "id",
            "title",
            "status",
            "priority",
            "category",
            "contactname",
            "assigneename",
            "teamname",
            "creationtime",
        };
    }

    private string GetSafeErrorMessage(Exception ex)
    {
        // Return a safe error message without exposing internals
        return ex switch
        {
            InvalidOperationException => "Export configuration error. Please try again.",
            TimeoutException => "Export timed out. Please try with a smaller dataset.",
            _ => "An error occurred during export. Please try again or contact support.",
        };
    }
}
