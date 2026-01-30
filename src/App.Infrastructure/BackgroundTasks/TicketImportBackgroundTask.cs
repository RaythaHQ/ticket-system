using System.Globalization;
using System.Text;
using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Imports.Commands;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background task that processes ticket imports from CSV files.
/// </summary>
public class TicketImportBackgroundTask : TicketImportJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketImportBackgroundTask> _logger;

    // Constants
    private const int MaxFileSizeBytes = 30 * 1024 * 1024; // 30 MB
    private const int BatchSize = 100;
    private const string NullIndicator = "[NULL]";

    // Column header names
    public static class ColumnNames
    {
        public const string Id = "Id";
        public const string Title = "Title";
        public const string Description = "Description";
        public const string Status = "Status";
        public const string Priority = "Priority";
        public const string Category = "Category";
        public const string OwningTeam = "OwningTeam";
        public const string Assignee = "Assignee";
        public const string Contact = "Contact";
        public const string Tags = "Tags";
        public const string CreatedAt = "CreatedAt";
        public const string ResolvedAt = "ResolvedAt";
        public const string ClosedAt = "ClosedAt";
    }

    public TicketImportBackgroundTask(
        IServiceProvider serviceProvider,
        ILogger<TicketImportBackgroundTask> logger
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
        var idGenerator = scope.ServiceProvider.GetRequiredService<INumericIdGenerator>();

        // Parse import job ID from args
        if (!args.TryGetProperty("ImportJobId", out var importJobIdElement))
        {
            _logger.LogError("TicketImportBackgroundTask: Missing ImportJobId in args");
            return;
        }

        var importJobId = importJobIdElement.GetGuid();
        var importJob = await db.ImportJobs.FirstOrDefaultAsync(
            e => e.Id == importJobId,
            cancellationToken
        );

        if (importJob == null)
        {
            _logger.LogError(
                "TicketImportBackgroundTask: ImportJob {ImportJobId} not found",
                importJobId
            );
            return;
        }

        try
        {
            await ProcessImportAsync(db, fileStorage, idGenerator, importJob, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TicketImportBackgroundTask: Import failed for job {ImportJobId}",
                importJobId
            );

            // Use a fresh DbContext to save the error status since the original context
            // may be in a bad state after the exception
            await SaveFailureStatusAsync(importJobId, ex, cancellationToken);
        }
    }

    private async Task SaveFailureStatusAsync(
        Guid importJobId,
        Exception ex,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var importJob = await db.ImportJobs.FirstOrDefaultAsync(
                e => e.Id == importJobId,
                cancellationToken
            );

            if (importJob != null)
            {
                importJob.Status = ImportJobStatus.Failed;
                importJob.ErrorMessage = GetSafeErrorMessage(ex);
                importJob.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception saveEx)
        {
            _logger.LogError(
                saveEx,
                "TicketImportBackgroundTask: Failed to save error status for job {ImportJobId}",
                importJobId
            );
        }
    }

    private async Task CompleteImportJobAsync(
        Guid importJobId,
        bool isDryRun,
        int processed,
        int inserted,
        int updated,
        int skipped,
        int errors,
        Guid? errorMediaItemId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var importJob = await db.ImportJobs.FirstOrDefaultAsync(
                e => e.Id == importJobId,
                cancellationToken
            );

            if (importJob != null)
            {
                importJob.Status = ImportJobStatus.Completed;
                importJob.ProgressStage = isDryRun ? "Dry run completed" : "Completed";
                importJob.ProgressPercent = 100;
                importJob.RowsProcessed = processed;
                importJob.RowsInserted = inserted;
                importJob.RowsUpdated = updated;
                importJob.RowsSkipped = skipped;
                importJob.RowsWithErrors = errors;
                importJob.ErrorMediaItemId = errorMediaItemId;
                importJob.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "TicketImportBackgroundTask: Import {ImportJobId} completed. Processed: {Processed}, Inserted: {Inserted}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
                    importJob.Id,
                    processed,
                    inserted,
                    updated,
                    skipped,
                    errors
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TicketImportBackgroundTask: Failed to mark job {ImportJobId} as completed",
                importJobId
            );
        }
    }

    private async Task UpdateProgressAsync(
        Guid importJobId,
        int processedCount,
        int totalRows,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var importJob = await db.ImportJobs.FirstOrDefaultAsync(
                e => e.Id == importJobId,
                cancellationToken
            );

            if (importJob != null)
            {
                importJob.RowsProcessed = processedCount;
                importJob.ProgressPercent = 20 + (int)((double)processedCount / totalRows * 60);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal - just log and continue
            _logger.LogWarning(
                ex,
                "TicketImportBackgroundTask: Failed to update progress for job {ImportJobId}",
                importJobId
            );
        }
    }

    /// <summary>
    /// Updates the import job stage using a fresh context (safe to call after entity is detached).
    /// </summary>
    private async Task UpdateStageAsync(
        Guid importJobId,
        string stage,
        int percent,
        int? totalRows = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var importJob = await db.ImportJobs.FirstOrDefaultAsync(
                e => e.Id == importJobId,
                cancellationToken
            );

            if (importJob != null)
            {
                importJob.ProgressStage = stage;
                importJob.ProgressPercent = percent;
                if (totalRows.HasValue)
                {
                    importJob.TotalRows = totalRows.Value;
                }
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal - just log and continue
            _logger.LogWarning(
                ex,
                "TicketImportBackgroundTask: Failed to update stage for job {ImportJobId}",
                importJobId
            );
        }
    }

    /// <summary>
    /// Saves a failure status using a fresh context (safe to call after entity is detached).
    /// </summary>
    private async Task SaveFailureWithMessageAsync(
        Guid importJobId,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var importJob = await db.ImportJobs.FirstOrDefaultAsync(
                e => e.Id == importJobId,
                cancellationToken
            );

            if (importJob != null)
            {
                importJob.Status = ImportJobStatus.Failed;
                importJob.ErrorMessage = errorMessage;
                importJob.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TicketImportBackgroundTask: Failed to save failure status for job {ImportJobId}",
                importJobId
            );
        }
    }

    private async Task ProcessImportAsync(
        IAppDbContext db,
        IFileStorageProvider fileStorage,
        INumericIdGenerator idGenerator,
        ImportJob importJob,
        CancellationToken cancellationToken
    )
    {
        // Update status to Running
        importJob.Status = ImportJobStatus.Running;
        importJob.ProgressStage = "Loading file";
        importJob.ProgressPercent = 0;
        await db.SaveChangesAsync(cancellationToken);
        // Detach the ImportJob from this context; we'll update progress/completion via separate contexts
        db.DbContext.Entry(importJob).State = EntityState.Detached;
        db.DbContext.ChangeTracker.Clear();

        // Get source file
        var sourceMediaItem = await db
            .MediaItems.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == importJob.SourceMediaItemId, cancellationToken);

        if (sourceMediaItem == null)
        {
            throw new InvalidOperationException("Source file not found");
        }

        // Download file content
        var fileBytes = await fileStorage.GetFileAsync(sourceMediaItem.ObjectKey);

        if (fileBytes.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size exceeds maximum of {MaxFileSizeBytes / 1024 / 1024} MB"
            );
        }

        // Parse CSV
        await UpdateStageAsync(
            importJob.Id,
            "Parsing CSV",
            10,
            cancellationToken: cancellationToken
        );

        // Title is only required for modes that can insert new records
        var requireTitle = importJob.Mode.DeveloperName != ImportMode.UPDATE_EXISTING_ONLY;
        var (rows, parseErrors) = ParseCsv(fileBytes, requireTitle);

        if (parseErrors.Any())
        {
            var errorMessage = $"CSV parsing failed: {string.Join("; ", parseErrors.Take(5))}";
            await SaveFailureWithMessageAsync(importJob.Id, errorMessage, cancellationToken);
            return;
        }

        // Update total rows count
        await UpdateStageAsync(
            importJob.Id,
            "Building reference lookups",
            15,
            rows.Count,
            cancellationToken
        );

        var lookups = await BuildLookupsAsync(db, cancellationToken);

        // Process rows
        await UpdateStageAsync(
            importJob.Id,
            "Processing rows",
            20,
            cancellationToken: cancellationToken
        );

        var errorRows = new List<ImportErrorRow>();
        var processedCount = 0;
        var insertedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        // Build lookup for existing tickets by ID
        var allIds = rows.Where(r =>
                r.TryGetValue(ColumnNames.Id, out var id) && long.TryParse(id, out _)
            )
            .Select(r => long.Parse(r[ColumnNames.Id]))
            .Distinct()
            .ToList();

        // Load existing tickets WITHOUT tracking - we just need to know which IDs exist
        // When we need to update, we'll use FindAsync to get a tracked version
        var existingTickets = await db
            .Tickets.AsNoTracking()
            .Where(t => allIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        foreach (var row in rows)
        {
            try
            {
                var result = await ProcessRowAsync(
                    db,
                    idGenerator,
                    importJob,
                    row,
                    existingTickets,
                    lookups,
                    cancellationToken
                );

                switch (result.Action)
                {
                    case ImportAction.Inserted:
                        insertedCount++;
                        break;
                    case ImportAction.Updated:
                        updatedCount++;
                        break;
                    case ImportAction.Skipped:
                        skippedCount++;
                        break;
                    case ImportAction.Error:
                        errorRows.Add(
                            new ImportErrorRow { Row = row, ErrorMessage = result.ErrorMessage }
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                errorRows.Add(
                    new ImportErrorRow
                    {
                        Row = row,
                        ErrorMessage = $"Unexpected error: {ex.Message}",
                    }
                );
            }

            processedCount++;

            // Update progress every batch (entities are saved immediately in their own contexts)
            if (processedCount % BatchSize == 0)
            {
                await UpdateProgressAsync(
                    importJob.Id,
                    processedCount,
                    rows.Count,
                    cancellationToken
                );
            }
        }

        // Generate error file if needed
        Guid? errorMediaItemId = null;
        if (errorRows.Any())
        {
            // Update progress using fresh context
            await UpdateProgressAsync(importJob.Id, processedCount, rows.Count, cancellationToken);

            errorMediaItemId = await GenerateErrorFileAsync(
                fileStorage,
                importJob,
                errorRows,
                cancellationToken
            );
        }

        await CompleteImportJobAsync(
            importJob.Id,
            importJob.IsDryRun,
            processedCount,
            insertedCount,
            updatedCount,
            skippedCount,
            errorRows.Count,
            errorMediaItemId,
            cancellationToken
        );
    }

    private async Task<ReferenceLookups> BuildLookupsAsync(
        IAppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var teams = await db.Teams.AsNoTracking().ToListAsync(cancellationToken);
        var users = await db.Users.AsNoTracking().ToListAsync(cancellationToken);
        var contacts = await db.Contacts.AsNoTracking().ToListAsync(cancellationToken);
        var statuses = await db.TicketStatusConfigs.AsNoTracking().ToListAsync(cancellationToken);
        var priorities = await db
            .TicketPriorityConfigs.AsNoTracking()
            .ToListAsync(cancellationToken);

        return new ReferenceLookups
        {
            TeamsById = teams.ToDictionary(t => t.Id),
            TeamsByName = teams.ToDictionary(
                t => t.Name.ToLowerInvariant(),
                StringComparer.OrdinalIgnoreCase
            ),
            UsersById = users.ToDictionary(u => u.Id),
            UsersByEmail = users
                .Where(u => !string.IsNullOrEmpty(u.EmailAddress))
                .GroupBy(u => u.EmailAddress!.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase),
            ContactsById = contacts.ToDictionary(c => c.Id),
            ContactsByEmail = contacts
                .Where(c => !string.IsNullOrEmpty(c.Email))
                .GroupBy(c => c.Email!.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase),
            ValidStatuses = statuses
                .Select(s => s.DeveloperName.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            ValidPriorities = priorities
                .Select(p => p.DeveloperName.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
        };
    }

    private (List<Dictionary<string, string>> rows, List<string> errors) ParseCsv(
        byte[] fileBytes,
        bool requireTitle = true
    )
    {
        var rows = new List<Dictionary<string, string>>();
        var errors = new List<string>();

        try
        {
            using var stream = new MemoryStream(fileBytes);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var csv = new CsvHelper.CsvReader(
                reader,
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                }
            );

            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord;

            // Title is required for insert modes, but not for update-only mode
            if (requireTitle && (headers == null || !headers.Contains(ColumnNames.Title)))
            {
                errors.Add($"Missing required column: {ColumnNames.Title}");
                return (rows, errors);
            }

            // Id column is always required for update-only mode
            if (!requireTitle && (headers == null || !headers.Contains(ColumnNames.Id)))
            {
                errors.Add($"Missing required column: {ColumnNames.Id} (required for update mode)");
                return (rows, errors);
            }

            while (csv.Read())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in headers)
                {
                    row[header] = csv.GetField(header) ?? string.Empty;
                }
                rows.Add(row);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"CSV parsing error: {ex.Message}");
        }

        return (rows, errors);
    }

    private async Task<ImportRowResult> ProcessRowAsync(
        IAppDbContext db,
        INumericIdGenerator idGenerator,
        ImportJob importJob,
        Dictionary<string, string> row,
        Dictionary<long, Ticket> existingTickets,
        ReferenceLookups lookups,
        CancellationToken cancellationToken
    )
    {
        // Get ID if provided
        long? ticketId = null;
        if (row.TryGetValue(ColumnNames.Id, out var idStr) && !string.IsNullOrWhiteSpace(idStr))
        {
            if (long.TryParse(idStr, out var parsedId))
            {
                ticketId = parsedId;
            }
            else
            {
                return ImportRowResult.Error($"Invalid ID format: {idStr}");
            }
        }

        // Check if ticket exists
        Ticket? existingTicket = null;
        if (ticketId.HasValue && existingTickets.TryGetValue(ticketId.Value, out existingTicket))
        {
            // Ticket exists
        }

        // Determine action based on import mode
        var mode = importJob.Mode;

        if (mode == ImportMode.InsertIfNotExists)
        {
            if (existingTicket != null)
            {
                return ImportRowResult.Skipped();
            }
            if (
                !row.TryGetValue(ColumnNames.Title, out var title)
                || string.IsNullOrWhiteSpace(title)
            )
            {
                return ImportRowResult.Error($"Missing required field: {ColumnNames.Title}");
            }
            return await InsertTicketAsync(
                db,
                idGenerator,
                row,
                ticketId,
                lookups,
                importJob.IsDryRun,
                importJob.SlaMode,
                cancellationToken
            );
        }
        else if (mode == ImportMode.UpdateExistingOnly)
        {
            if (existingTicket == null)
            {
                return ImportRowResult.Skipped();
            }
            return await UpdateTicketAsync(
                db,
                existingTicket,
                row,
                lookups,
                importJob.IsDryRun,
                cancellationToken
            );
        }
        else // Upsert
        {
            if (existingTicket != null)
            {
                return await UpdateTicketAsync(
                    db,
                    existingTicket,
                    row,
                    lookups,
                    importJob.IsDryRun,
                    cancellationToken
                );
            }
            else
            {
                if (
                    !row.TryGetValue(ColumnNames.Title, out var title)
                    || string.IsNullOrWhiteSpace(title)
                )
                {
                    return ImportRowResult.Error($"Missing required field: {ColumnNames.Title}");
                }
                return await InsertTicketAsync(
                    db,
                    idGenerator,
                    row,
                    ticketId,
                    lookups,
                    importJob.IsDryRun,
                    importJob.SlaMode,
                    cancellationToken
                );
            }
        }
    }

    private async Task<ImportRowResult> InsertTicketAsync(
        IAppDbContext db,
        INumericIdGenerator idGenerator,
        Dictionary<string, string> row,
        long? specifiedId,
        ReferenceLookups lookups,
        bool isDryRun,
        ImportSlaMode? slaMode,
        CancellationToken cancellationToken
    )
    {
        // Use specified ID if provided, otherwise generate a new one
        long ticketId;
        if (specifiedId.HasValue)
        {
            ticketId = specifiedId.Value;
        }
        else
        {
            ticketId = await idGenerator.GetNextTicketIdAsync(cancellationToken);
        }

        var ticket = new Ticket
        {
            Id = ticketId,
            Title = GetValue(row, ColumnNames.Title) ?? string.Empty,
            Description = GetNullableValue(row, ColumnNames.Description),
            Status = TicketStatus.OPEN,
            Priority = TicketPriority.NORMAL,
            Category = GetNullableValue(row, ColumnNames.Category),
        };

        // Resolve status
        if (
            row.TryGetValue(ColumnNames.Status, out var statusStr)
            && !string.IsNullOrWhiteSpace(statusStr)
            && statusStr != NullIndicator
        )
        {
            if (lookups.ValidStatuses.Contains(statusStr))
            {
                ticket.Status = statusStr.ToLowerInvariant();
            }
            else
            {
                return ImportRowResult.Error($"Invalid status: {statusStr}");
            }
        }

        // Resolve priority
        if (
            row.TryGetValue(ColumnNames.Priority, out var priorityStr)
            && !string.IsNullOrWhiteSpace(priorityStr)
            && priorityStr != NullIndicator
        )
        {
            if (lookups.ValidPriorities.Contains(priorityStr))
            {
                ticket.Priority = priorityStr.ToLowerInvariant();
            }
            else
            {
                return ImportRowResult.Error($"Invalid priority: {priorityStr}");
            }
        }

        // Resolve team
        var teamResult = ResolveTeam(row, lookups);
        if (teamResult.IsError)
        {
            return ImportRowResult.Error(teamResult.ErrorMessage!);
        }
        ticket.OwningTeamId = teamResult.Value;

        // Resolve assignee
        var assigneeResult = ResolveUser(row, ColumnNames.Assignee, lookups);
        if (assigneeResult.IsError)
        {
            return ImportRowResult.Error(assigneeResult.ErrorMessage!);
        }
        ticket.AssigneeId = assigneeResult.Value;

        // Resolve contact
        var contactResult = ResolveContact(row, lookups);
        if (contactResult.IsError)
        {
            return ImportRowResult.Error(contactResult.ErrorMessage!);
        }
        ticket.ContactId = contactResult.Value;

        // Handle tags (semicolon-separated)
        var tagsStr = GetNullableValue(row, ColumnNames.Tags);
        if (!string.IsNullOrWhiteSpace(tagsStr))
        {
            ticket.Tags = tagsStr
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
        }

        // Handle dates
        // CreatedAt - use provided value or default to now (handled by EF)
        if (
            row.TryGetValue(ColumnNames.CreatedAt, out var createdAtStr)
            && !string.IsNullOrWhiteSpace(createdAtStr)
            && createdAtStr != NullIndicator
        )
        {
            if (
                DateTime.TryParse(
                    createdAtStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var createdAt
                )
            )
            {
                ticket.CreationTime = createdAt;
            }
            else
            {
                return ImportRowResult.Error(
                    $"Invalid date format for CreatedAt: {createdAtStr}. Use ISO 8601 format (e.g., 2024-01-15T10:30:00Z)"
                );
            }
        }

        if (
            row.TryGetValue(ColumnNames.ResolvedAt, out var resolvedAtStr)
            && !string.IsNullOrWhiteSpace(resolvedAtStr)
            && resolvedAtStr != NullIndicator
        )
        {
            if (
                DateTime.TryParse(
                    resolvedAtStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var resolvedAt
                )
            )
            {
                ticket.ResolvedAt = resolvedAt;
            }
            else
            {
                return ImportRowResult.Error(
                    $"Invalid date format for ResolvedAt: {resolvedAtStr}. Use ISO 8601 format (e.g., 2024-01-15T10:30:00Z)"
                );
            }
        }

        if (
            row.TryGetValue(ColumnNames.ClosedAt, out var closedAtStr)
            && !string.IsNullOrWhiteSpace(closedAtStr)
            && closedAtStr != NullIndicator
        )
        {
            if (
                DateTime.TryParse(
                    closedAtStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var closedAt
                )
            )
            {
                ticket.ClosedAt = closedAt;
            }
            else
            {
                return ImportRowResult.Error(
                    $"Invalid date format for ClosedAt: {closedAtStr}. Use ISO 8601 format (e.g., 2024-01-15T10:30:00Z)"
                );
            }
        }

        if (!isDryRun)
        {
            // Use a fresh context to insert the ticket immediately
            // This avoids tracking conflicts with the main context
            using var scope = _serviceProvider.CreateScope();
            var freshDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            freshDb.Tickets.Add(ticket);
            await freshDb.SaveChangesAsync(cancellationToken);

            // Apply SLA rules if requested
            if (slaMode != null && slaMode != ImportSlaMode.DoNotApply)
            {
                await ApplySlaRulesAsync(scope, ticket, slaMode, cancellationToken);
            }
        }

        return ImportRowResult.Inserted();
    }

    /// <summary>
    /// Applies SLA rules to an imported ticket based on the selected SLA mode.
    /// </summary>
    private async Task ApplySlaRulesAsync(
        IServiceScope scope,
        Ticket ticket,
        ImportSlaMode slaMode,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var freshDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();

            // Re-fetch the ticket to get a tracked entity
            var trackedTicket = await freshDb.Tickets.FindAsync(
                new object[] { ticket.Id },
                cancellationToken
            );

            if (trackedTicket == null)
                return;

            // For "from_current_time" mode, temporarily override CreationTime for SLA calculation
            var originalCreationTime = trackedTicket.CreationTime;
            if (slaMode == ImportSlaMode.FromCurrentTime)
            {
                trackedTicket.CreationTime = DateTime.UtcNow;
            }

            // Evaluate and assign SLA rules
            await slaService.EvaluateAndAssignSlaAsync(trackedTicket, cancellationToken);

            // Restore original CreationTime if we changed it
            if (slaMode == ImportSlaMode.FromCurrentTime)
            {
                trackedTicket.CreationTime = originalCreationTime;
            }

            await freshDb.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but don't fail the import if SLA assignment fails
            _logger.LogWarning(
                ex,
                "TicketImportBackgroundTask: Failed to apply SLA rules for ticket {TicketId}",
                ticket.Id
            );
        }
    }

    private async Task<ImportRowResult> UpdateTicketAsync(
        IAppDbContext db,
        Ticket ticket,
        Dictionary<string, string> row,
        ReferenceLookups lookups,
        bool isDryRun,
        CancellationToken cancellationToken
    )
    {
        // For actual updates, use a fresh context
        IAppDbContext? freshDb = null;
        IServiceScope? scope = null;

        if (!isDryRun)
        {
            scope = _serviceProvider.CreateScope();
            freshDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var trackedTicket = await freshDb.Tickets.FindAsync(
                new object[] { ticket.Id },
                cancellationToken
            );
            if (trackedTicket == null)
            {
                scope.Dispose();
                return ImportRowResult.Error("Ticket not found for update");
            }
            ticket = trackedTicket;
        }

        // Update fields if provided
        if (row.TryGetValue(ColumnNames.Title, out var title) && !string.IsNullOrWhiteSpace(title))
        {
            if (title != NullIndicator)
            {
                ticket.Title = title;
            }
        }

        if (
            row.TryGetValue(ColumnNames.Description, out var description)
            && !string.IsNullOrEmpty(description)
        )
        {
            ticket.Description = description == NullIndicator ? null : description;
        }

        if (
            row.TryGetValue(ColumnNames.Category, out var category)
            && !string.IsNullOrEmpty(category)
        )
        {
            ticket.Category = category == NullIndicator ? null : category;
        }

        // Resolve status
        if (
            row.TryGetValue(ColumnNames.Status, out var statusStr)
            && !string.IsNullOrEmpty(statusStr)
        )
        {
            if (statusStr == NullIndicator)
            {
                // Status is required, can't be null - skip
            }
            else if (lookups.ValidStatuses.Contains(statusStr))
            {
                ticket.Status = statusStr.ToLowerInvariant();
            }
            else
            {
                return ImportRowResult.Error($"Invalid status: {statusStr}");
            }
        }

        // Resolve priority
        if (
            row.TryGetValue(ColumnNames.Priority, out var priorityStr)
            && !string.IsNullOrEmpty(priorityStr)
        )
        {
            if (priorityStr == NullIndicator)
            {
                // Priority is required, can't be null - skip
            }
            else if (lookups.ValidPriorities.Contains(priorityStr))
            {
                ticket.Priority = priorityStr.ToLowerInvariant();
            }
            else
            {
                return ImportRowResult.Error($"Invalid priority: {priorityStr}");
            }
        }

        // Resolve team
        if (
            row.TryGetValue(ColumnNames.OwningTeam, out var teamStr)
            && !string.IsNullOrEmpty(teamStr)
        )
        {
            if (teamStr == NullIndicator)
            {
                ticket.OwningTeamId = null;
            }
            else
            {
                var teamResult = ResolveTeam(row, lookups);
                if (teamResult.IsError)
                {
                    return ImportRowResult.Error(teamResult.ErrorMessage!);
                }
                ticket.OwningTeamId = teamResult.Value;
            }
        }

        // Resolve assignee
        if (
            row.TryGetValue(ColumnNames.Assignee, out var assigneeStr)
            && !string.IsNullOrEmpty(assigneeStr)
        )
        {
            if (assigneeStr == NullIndicator)
            {
                ticket.AssigneeId = null;
            }
            else
            {
                var assigneeResult = ResolveUser(row, ColumnNames.Assignee, lookups);
                if (assigneeResult.IsError)
                {
                    return ImportRowResult.Error(assigneeResult.ErrorMessage!);
                }
                ticket.AssigneeId = assigneeResult.Value;
            }
        }

        // Resolve contact
        if (
            row.TryGetValue(ColumnNames.Contact, out var contactStr)
            && !string.IsNullOrEmpty(contactStr)
        )
        {
            if (contactStr == NullIndicator)
            {
                ticket.ContactId = null;
            }
            else
            {
                var contactResult = ResolveContact(row, lookups);
                if (contactResult.IsError)
                {
                    return ImportRowResult.Error(contactResult.ErrorMessage!);
                }
                ticket.ContactId = contactResult.Value;
            }
        }

        // Handle tags (replace all)
        if (row.TryGetValue(ColumnNames.Tags, out var tagsStr) && !string.IsNullOrEmpty(tagsStr))
        {
            if (tagsStr == NullIndicator)
            {
                ticket.Tags = new List<string>();
            }
            else
            {
                ticket.Tags = tagsStr
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();
            }
        }

        // Handle dates
        // CreatedAt - can be updated for data migration purposes
        if (
            row.TryGetValue(ColumnNames.CreatedAt, out var createdAtStr)
            && !string.IsNullOrEmpty(createdAtStr)
            && createdAtStr != NullIndicator
        )
        {
            if (
                DateTime.TryParse(
                    createdAtStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var createdAt
                )
            )
            {
                ticket.CreationTime = createdAt;
            }
            else
            {
                return ImportRowResult.Error(
                    $"Invalid date format for CreatedAt: {createdAtStr}. Use ISO 8601 format."
                );
            }
        }

        if (
            row.TryGetValue(ColumnNames.ResolvedAt, out var resolvedAtStr)
            && !string.IsNullOrEmpty(resolvedAtStr)
        )
        {
            if (resolvedAtStr == NullIndicator)
            {
                ticket.ResolvedAt = null;
            }
            else if (
                DateTime.TryParse(
                    resolvedAtStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var resolvedAt
                )
            )
            {
                ticket.ResolvedAt = resolvedAt;
            }
            else
            {
                return ImportRowResult.Error(
                    $"Invalid date format for ResolvedAt: {resolvedAtStr}. Use ISO 8601 format."
                );
            }
        }

        if (
            row.TryGetValue(ColumnNames.ClosedAt, out var closedAtStr)
            && !string.IsNullOrEmpty(closedAtStr)
        )
        {
            if (closedAtStr == NullIndicator)
            {
                ticket.ClosedAt = null;
            }
            else if (
                DateTime.TryParse(
                    closedAtStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var closedAt
                )
            )
            {
                ticket.ClosedAt = closedAt;
            }
            else
            {
                return ImportRowResult.Error(
                    $"Invalid date format for ClosedAt: {closedAtStr}. Use ISO 8601 format."
                );
            }
        }

        // Save changes and dispose fresh context
        if (!isDryRun && freshDb != null)
        {
            await freshDb.SaveChangesAsync(cancellationToken);
            scope?.Dispose();
        }

        return ImportRowResult.Updated();
    }

    private ResolveResult<Guid?> ResolveTeam(
        Dictionary<string, string> row,
        ReferenceLookups lookups
    )
    {
        if (
            !row.TryGetValue(ColumnNames.OwningTeam, out var teamStr)
            || string.IsNullOrWhiteSpace(teamStr)
            || teamStr == NullIndicator
        )
        {
            return ResolveResult<Guid?>.Success(null);
        }

        // Try parse as GUID
        if (Guid.TryParse(teamStr, out var teamId))
        {
            if (lookups.TeamsById.ContainsKey(teamId))
            {
                return ResolveResult<Guid?>.Success(teamId);
            }
            return ResolveResult<Guid?>.Fail($"Team with ID '{teamId}' not found");
        }

        // Try lookup by name
        if (lookups.TeamsByName.TryGetValue(teamStr, out var team))
        {
            return ResolveResult<Guid?>.Success(team.Id);
        }

        return ResolveResult<Guid?>.Fail($"Team '{teamStr}' not found");
    }

    private ResolveResult<Guid?> ResolveUser(
        Dictionary<string, string> row,
        string columnName,
        ReferenceLookups lookups
    )
    {
        if (
            !row.TryGetValue(columnName, out var userStr)
            || string.IsNullOrWhiteSpace(userStr)
            || userStr == NullIndicator
        )
        {
            return ResolveResult<Guid?>.Success(null);
        }

        // Try parse as GUID
        if (Guid.TryParse(userStr, out var userId))
        {
            if (lookups.UsersById.ContainsKey(userId))
            {
                return ResolveResult<Guid?>.Success(userId);
            }
            return ResolveResult<Guid?>.Fail($"User with ID '{userId}' not found");
        }

        // Try lookup by email
        if (lookups.UsersByEmail.TryGetValue(userStr, out var user))
        {
            return ResolveResult<Guid?>.Success(user.Id);
        }

        return ResolveResult<Guid?>.Fail($"User '{userStr}' not found");
    }

    private ResolveResult<long?> ResolveContact(
        Dictionary<string, string> row,
        ReferenceLookups lookups
    )
    {
        if (
            !row.TryGetValue(ColumnNames.Contact, out var contactStr)
            || string.IsNullOrWhiteSpace(contactStr)
            || contactStr == NullIndicator
        )
        {
            return ResolveResult<long?>.Success(null);
        }

        // Try parse as long (contact ID)
        if (long.TryParse(contactStr, out var contactId))
        {
            if (lookups.ContactsById.ContainsKey(contactId))
            {
                return ResolveResult<long?>.Success(contactId);
            }
            return ResolveResult<long?>.Fail($"Contact with ID '{contactId}' not found");
        }

        // Try lookup by email
        if (lookups.ContactsByEmail.TryGetValue(contactStr, out var contact))
        {
            return ResolveResult<long?>.Success(contact.Id);
        }

        return ResolveResult<long?>.Fail($"Contact '{contactStr}' not found");
    }

    private string? GetValue(Dictionary<string, string> row, string key)
    {
        if (
            row.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(value)
            && value != NullIndicator
        )
        {
            return value;
        }
        return null;
    }

    private string? GetNullableValue(Dictionary<string, string> row, string key)
    {
        if (row.TryGetValue(key, out var value))
        {
            if (string.IsNullOrEmpty(value) || value == NullIndicator)
            {
                return null;
            }
            return value;
        }
        return null;
    }

    private async Task<Guid> GenerateErrorFileAsync(
        IFileStorageProvider fileStorage,
        ImportJob importJob,
        List<ImportErrorRow> errorRows,
        CancellationToken cancellationToken
    )
    {
        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, new UTF8Encoding(true));
        await using var csv = new CsvWriter(
            writer,
            new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }
        );

        var headers = errorRows.First().Row.Keys.ToList();
        headers.Add("ImportError");

        foreach (var header in headers)
        {
            csv.WriteField(header);
        }
        await csv.NextRecordAsync();

        foreach (var errorRow in errorRows)
        {
            foreach (var header in headers)
            {
                if (header == "ImportError")
                {
                    csv.WriteField(errorRow.ErrorMessage);
                }
                else
                {
                    csv.WriteField(errorRow.Row.GetValueOrDefault(header, ""));
                }
            }
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
        var csvBytes = memoryStream.ToArray();

        var fileName = $"ticket-import-errors-{importJob.RequestedAt:yyyyMMdd-HHmmss}.csv";
        var objectKey = $"imports/{importJob.Id}/errors/{fileName}";
        var contentType = "text/csv";

        await fileStorage.SaveAndGetDownloadUrlAsync(
            csvBytes,
            objectKey,
            fileName,
            contentType,
            importJob.ExpiresAt,
            inline: false
        );

        // Create MediaItem using fresh context
        var mediaItem = new MediaItem
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            FileStorageProvider = fileStorage.GetName(),
            ObjectKey = objectKey,
            Length = csvBytes.Length,
        };

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        db.MediaItems.Add(mediaItem);
        await db.SaveChangesAsync(cancellationToken);

        return mediaItem.Id;
    }

    private string GetSafeErrorMessage(Exception ex)
    {
        // Get the innermost exception message for database/EF errors
        var innerMessage = ex.InnerException?.Message ?? ex.Message;

        return ex switch
        {
            InvalidOperationException ioe => ioe.Message,
            TimeoutException => "Import timed out. Please try with a smaller file.",
            Microsoft.EntityFrameworkCore.DbUpdateException dbEx =>
                $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}",
            _ => $"Import failed: {innerMessage}",
        };
    }

    private enum ImportAction
    {
        Inserted,
        Updated,
        Skipped,
        Error,
    }

    private record ImportRowResult
    {
        public ImportAction Action { get; init; }
        public string? ErrorMessage { get; init; }

        public static ImportRowResult Inserted() => new() { Action = ImportAction.Inserted };

        public static ImportRowResult Updated() => new() { Action = ImportAction.Updated };

        public static ImportRowResult Skipped() => new() { Action = ImportAction.Skipped };

        public static ImportRowResult Error(string message) =>
            new() { Action = ImportAction.Error, ErrorMessage = message };
    }

    private record ImportErrorRow
    {
        public Dictionary<string, string> Row { get; init; } = new();
        public string ErrorMessage { get; init; } = string.Empty;
    }

    private record ReferenceLookups
    {
        public Dictionary<Guid, Team> TeamsById { get; init; } = new();
        public Dictionary<string, Team> TeamsByName { get; init; } = new();
        public Dictionary<Guid, User> UsersById { get; init; } = new();
        public Dictionary<string, User> UsersByEmail { get; init; } = new();
        public Dictionary<long, Contact> ContactsById { get; init; } = new();
        public Dictionary<string, Contact> ContactsByEmail { get; init; } = new();
        public HashSet<string> ValidStatuses { get; init; } = new();
        public HashSet<string> ValidPriorities { get; init; } = new();
    }

    private record ResolveResult<T>
    {
        public T? Value { get; init; }
        public bool IsError { get; init; }
        public string? ErrorMessage { get; init; }

        public static ResolveResult<T> Success(T? value) =>
            new() { Value = value, IsError = false };

        public static ResolveResult<T> Fail(string message) =>
            new() { IsError = true, ErrorMessage = message };
    }
}
