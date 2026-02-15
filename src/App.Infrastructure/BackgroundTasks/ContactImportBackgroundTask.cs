using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Imports.Commands;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static App.Infrastructure.BackgroundTasks.ImportJobHelper;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background task that processes contact imports from CSV files.
/// </summary>
public class ContactImportBackgroundTask : ContactImportJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContactImportBackgroundTask> _logger;
    private readonly ImportJobHelper _helper;

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
        _helper = new ImportJobHelper(serviceProvider, logger, nameof(ContactImportBackgroundTask));
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
            await _helper.SaveFailureStatusAsync(importJobId, ex, cancellationToken);
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

        if (fileBytes.Length > ImportJobHelper.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size exceeds maximum of {ImportJobHelper.MaxFileSizeBytes / 1024 / 1024} MB"
            );
        }

        // Parse CSV
        await _helper.UpdateStageAsync(
            importJob.Id,
            "Parsing CSV",
            10,
            cancellationToken: cancellationToken
        );

        // FirstName is only required for modes that can insert new records
        var requireFirstName = importJob.Mode.DeveloperName != ImportMode.UPDATE_EXISTING_ONLY;
        var (rows, parseErrors) = ImportJobHelper.ParseCsv(fileBytes, ColumnNames.FirstName, requireFirstName);

        if (parseErrors.Any())
        {
            var errorMessage = $"CSV parsing failed: {string.Join("; ", parseErrors.Take(5))}";
            await _helper.SaveFailureWithMessageAsync(importJob.Id, errorMessage, cancellationToken);
            return;
        }

        // Update total rows count
        await _helper.UpdateStageAsync(
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
            if (processedCount % ImportJobHelper.BatchSize == 0)
            {
                await _helper.UpdateProgressAsync(
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
            await _helper.UpdateProgressAsync(importJob.Id, processedCount, rows.Count, cancellationToken);

            errorMediaItemId = await _helper.GenerateErrorFileAsync(
                fileStorage,
                importJob,
                errorRows,
                "contact",
                cancellationToken
            );
        }

        await _helper.CompleteImportJobAsync(
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
            FirstName = ImportJobHelper.GetValue(row, ColumnNames.FirstName) ?? string.Empty,
            LastName = ImportJobHelper.GetNullableValue(row, ColumnNames.LastName),
            Email = ImportJobHelper.GetNullableValue(row, ColumnNames.Email),
            Address = ImportJobHelper.GetNullableValue(row, ColumnNames.Address),
            OrganizationAccount = ImportJobHelper.GetNullableValue(row, ColumnNames.OrganizationAccount),
        };

        // Handle phone numbers (semicolon-separated)
        var phoneNumbers = ImportJobHelper.GetNullableValue(row, ColumnNames.PhoneNumbers);
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
                if (firstName != ImportJobHelper.NullIndicator)
                {
                    trackedContact.FirstName = firstName;
                }
            }

            if (
                row.TryGetValue(ColumnNames.LastName, out var lastName)
                && !string.IsNullOrEmpty(lastName)
            )
            {
                trackedContact.LastName = lastName == ImportJobHelper.NullIndicator ? null : lastName;
            }

            if (row.TryGetValue(ColumnNames.Email, out var email) && !string.IsNullOrEmpty(email))
            {
                trackedContact.Email = email == ImportJobHelper.NullIndicator ? null : email;
            }

            if (
                row.TryGetValue(ColumnNames.Address, out var address)
                && !string.IsNullOrEmpty(address)
            )
            {
                trackedContact.Address = address == ImportJobHelper.NullIndicator ? null : address;
            }

            if (
                row.TryGetValue(ColumnNames.OrganizationAccount, out var orgAccount)
                && !string.IsNullOrEmpty(orgAccount)
            )
            {
                trackedContact.OrganizationAccount =
                    orgAccount == ImportJobHelper.NullIndicator ? null : orgAccount;
            }

            if (
                row.TryGetValue(ColumnNames.PhoneNumbers, out var phoneNumbers)
                && !string.IsNullOrEmpty(phoneNumbers)
            )
            {
                if (phoneNumbers == ImportJobHelper.NullIndicator)
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
}
