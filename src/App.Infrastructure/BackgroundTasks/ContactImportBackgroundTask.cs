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
/// Background task that processes contact imports from CSV files.
/// </summary>
public class ContactImportBackgroundTask : ContactImportJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContactImportBackgroundTask> _logger;

    // Constants
    private const int MaxFileSizeBytes = 30 * 1024 * 1024; // 30 MB
    private const int BatchSize = 100;
    private const string NullIndicator = "[NULL]";

    // Column header names
    public static class ColumnNames
    {
        public const string Id = "Id";
        public const string FirstName = "FirstName";
        public const string LastName = "LastName";
        public const string Email = "Email";
        public const string PhoneNumbers = "PhoneNumbers";
        public const string Address = "Address";
        public const string OrganizationAccount = "OrganizationAccount";
    }

    public ContactImportBackgroundTask(
        IServiceProvider serviceProvider,
        ILogger<ContactImportBackgroundTask> logger
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
            _logger.LogError("ContactImportBackgroundTask: Missing ImportJobId in args");
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
                "ContactImportBackgroundTask: ImportJob {ImportJobId} not found",
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
                "ContactImportBackgroundTask: Import failed for job {ImportJobId}",
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
                "ContactImportBackgroundTask: Failed to save error status for job {ImportJobId}",
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
                    "ContactImportBackgroundTask: Import {ImportJobId} completed. Processed: {Processed}, Inserted: {Inserted}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
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
                "ContactImportBackgroundTask: Failed to mark job {ImportJobId} as completed",
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
                "ContactImportBackgroundTask: Failed to update progress for job {ImportJobId}",
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
                "ContactImportBackgroundTask: Failed to update stage for job {ImportJobId}",
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
                "ContactImportBackgroundTask: Failed to save failure status for job {ImportJobId}",
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

        // FirstName is only required for modes that can insert new records
        var requireFirstName = importJob.Mode.DeveloperName != ImportMode.UPDATE_EXISTING_ONLY;
        var (rows, parseErrors) = ParseCsv(fileBytes, requireFirstName);

        if (parseErrors.Any())
        {
            var errorMessage = $"CSV parsing failed: {string.Join("; ", parseErrors.Take(5))}";
            await SaveFailureWithMessageAsync(importJob.Id, errorMessage, cancellationToken);
            return;
        }

        // Update total rows count
        await UpdateStageAsync(
            importJob.Id,
            "Processing rows",
            20,
            rows.Count,
            cancellationToken
        );

        var errorRows = new List<ImportErrorRow>();
        var processedCount = 0;
        var insertedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        // Build lookup for existing contacts by ID
        var allIds = rows.Where(r =>
                r.TryGetValue(ColumnNames.Id, out var id) && long.TryParse(id, out _)
            )
            .Select(r => long.Parse(r[ColumnNames.Id]))
            .Distinct()
            .ToList();

        // Load existing contacts WITHOUT tracking - we just need to know which IDs exist
        // When we need to update, we'll use FindAsync to get a tracked version
        var existingContacts = await db
            .Contacts.AsNoTracking()
            .Where(c => allIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        foreach (var row in rows)
        {
            try
            {
                var result = await ProcessRowAsync(
                    db,
                    idGenerator,
                    importJob,
                    row,
                    existingContacts,
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

    private (List<Dictionary<string, string>> rows, List<string> errors) ParseCsv(
        byte[] fileBytes,
        bool requireFirstName = true
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

            // FirstName is required for insert modes, but not for update-only mode
            if (requireFirstName && (headers == null || !headers.Contains(ColumnNames.FirstName)))
            {
                errors.Add($"Missing required column: {ColumnNames.FirstName}");
                return (rows, errors);
            }

            // Id column is always required for update-only mode
            if (!requireFirstName && (headers == null || !headers.Contains(ColumnNames.Id)))
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
        Dictionary<long, Contact> existingContacts,
        CancellationToken cancellationToken
    )
    {
        // Get ID if provided
        long? contactId = null;
        if (row.TryGetValue(ColumnNames.Id, out var idStr) && !string.IsNullOrWhiteSpace(idStr))
        {
            if (long.TryParse(idStr, out var parsedId))
            {
                contactId = parsedId;
            }
            else
            {
                return ImportRowResult.Error($"Invalid ID format: {idStr}");
            }
        }

        // Check if contact exists
        Contact? existingContact = null;
        if (
            contactId.HasValue && existingContacts.TryGetValue(contactId.Value, out existingContact)
        )
        {
            // Contact exists
        }

        // Determine action based on import mode
        var mode = importJob.Mode;

        if (mode == ImportMode.InsertIfNotExists)
        {
            if (existingContact != null)
            {
                return ImportRowResult.Skipped();
            }
            // Validate required fields for insert
            if (
                !row.TryGetValue(ColumnNames.FirstName, out var firstName)
                || string.IsNullOrWhiteSpace(firstName)
            )
            {
                return ImportRowResult.Error($"Missing required field: {ColumnNames.FirstName}");
            }
            return await InsertContactAsync(
                db,
                idGenerator,
                row,
                contactId,
                importJob.IsDryRun,
                cancellationToken
            );
        }
        else if (mode == ImportMode.UpdateExistingOnly)
        {
            if (existingContact == null)
            {
                return ImportRowResult.Skipped();
            }
            return await UpdateContactAsync(
                db,
                existingContact,
                row,
                importJob.IsDryRun,
                cancellationToken
            );
        }
        else // Upsert
        {
            if (existingContact != null)
            {
                return await UpdateContactAsync(
                    db,
                    existingContact,
                    row,
                    importJob.IsDryRun,
                    cancellationToken
                );
            }
            else
            {
                // Validate required fields for insert
                if (
                    !row.TryGetValue(ColumnNames.FirstName, out var firstName)
                    || string.IsNullOrWhiteSpace(firstName)
                )
                {
                    return ImportRowResult.Error(
                        $"Missing required field: {ColumnNames.FirstName}"
                    );
                }
                return await InsertContactAsync(
                    db,
                    idGenerator,
                    row,
                    contactId,
                    importJob.IsDryRun,
                    cancellationToken
                );
            }
        }
    }

    /// <summary>
    /// EF Core occasionally holds Detached entries in the ChangeTracker after various operations.
    /// This helper force-detaches them so SaveChanges doesn't see unexpected Detached entries.
    /// </summary>
    private async Task<ImportRowResult> InsertContactAsync(
        IAppDbContext db,
        INumericIdGenerator idGenerator,
        Dictionary<string, string> row,
        long? specifiedId,
        bool isDryRun,
        CancellationToken cancellationToken
    )
    {
        // Use specified ID if provided, otherwise generate a new one
        long contactId;
        if (specifiedId.HasValue)
        {
            contactId = specifiedId.Value;
        }
        else
        {
            contactId = await idGenerator.GetNextContactIdAsync(cancellationToken);
        }

        var contact = new Contact
        {
            Id = contactId,
            FirstName = GetValue(row, ColumnNames.FirstName) ?? string.Empty,
            LastName = GetNullableValue(row, ColumnNames.LastName),
            Email = GetNullableValue(row, ColumnNames.Email),
            Address = GetNullableValue(row, ColumnNames.Address),
            OrganizationAccount = GetNullableValue(row, ColumnNames.OrganizationAccount),
        };

        // Handle phone numbers (semicolon-separated)
        var phoneNumbers = GetNullableValue(row, ColumnNames.PhoneNumbers);
        if (!string.IsNullOrWhiteSpace(phoneNumbers))
        {
            contact.PhoneNumbers = phoneNumbers
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
        }

        if (!isDryRun)
        {
            // Use a fresh context to insert the contact immediately
            // This avoids tracking conflicts with the main context
            using var scope = _serviceProvider.CreateScope();
            var freshDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            freshDb.Contacts.Add(contact);
            await freshDb.SaveChangesAsync(cancellationToken);
        }

        return ImportRowResult.Inserted();
    }

    private async Task<ImportRowResult> UpdateContactAsync(
        IAppDbContext db,
        Contact contact,
        Dictionary<string, string> row,
        bool isDryRun,
        CancellationToken cancellationToken
    )
    {
        if (!isDryRun)
        {
            // Use a fresh context for the update to avoid tracking conflicts
            using var scope = _serviceProvider.CreateScope();
            var freshDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var trackedContact = await freshDb.Contacts.FindAsync(
                new object[] { contact.Id },
                cancellationToken
            );
            if (trackedContact == null)
            {
                return ImportRowResult.Error("Contact not found for update");
            }

            // Update fields if provided (empty = no change, [NULL] = set to null)
            if (
                row.TryGetValue(ColumnNames.FirstName, out var firstName)
                && !string.IsNullOrWhiteSpace(firstName)
            )
            {
                if (firstName != NullIndicator)
                {
                    trackedContact.FirstName = firstName;
                }
            }

            if (
                row.TryGetValue(ColumnNames.LastName, out var lastName)
                && !string.IsNullOrEmpty(lastName)
            )
            {
                trackedContact.LastName = lastName == NullIndicator ? null : lastName;
            }

            if (row.TryGetValue(ColumnNames.Email, out var email) && !string.IsNullOrEmpty(email))
            {
                trackedContact.Email = email == NullIndicator ? null : email;
            }

            if (
                row.TryGetValue(ColumnNames.Address, out var address)
                && !string.IsNullOrEmpty(address)
            )
            {
                trackedContact.Address = address == NullIndicator ? null : address;
            }

            if (
                row.TryGetValue(ColumnNames.OrganizationAccount, out var orgAccount)
                && !string.IsNullOrEmpty(orgAccount)
            )
            {
                trackedContact.OrganizationAccount =
                    orgAccount == NullIndicator ? null : orgAccount;
            }

            if (
                row.TryGetValue(ColumnNames.PhoneNumbers, out var phoneNumbers)
                && !string.IsNullOrEmpty(phoneNumbers)
            )
            {
                if (phoneNumbers == NullIndicator)
                {
                    trackedContact.PhoneNumbers = new List<string>();
                }
                else
                {
                    trackedContact.PhoneNumbers = phoneNumbers
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .ToList();
                }
            }

            await freshDb.SaveChangesAsync(cancellationToken);
        }

        return ImportRowResult.Updated();
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

        // Get all column headers from first error row plus error column
        var headers = errorRows.First().Row.Keys.ToList();
        headers.Add("ImportError");

        // Write header
        foreach (var header in headers)
        {
            csv.WriteField(header);
        }
        await csv.NextRecordAsync();

        // Write error rows
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

        // Upload error file
        var fileName = $"contact-import-errors-{importJob.RequestedAt:yyyyMMdd-HHmmss}.csv";
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
}
