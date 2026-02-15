using System.Globalization;
using System.Text;
using App.Application.Common.Interfaces;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Shared helper for import background tasks. Provides common operations for
/// progress tracking, error handling, CSV parsing, and error file generation.
/// </summary>
public class ImportJobHelper
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly string _taskName;

    public const int MaxFileSizeBytes = 30 * 1024 * 1024; // 30 MB
    public const int BatchSize = 100;
    public const string NullIndicator = "[NULL]";

    // Progress bar range: processing rows occupies 20%-80% of the bar
    private const int ProgressStartPercent = 20;
    private const int ProgressRangePercent = 60;

    public ImportJobHelper(IServiceProvider serviceProvider, ILogger logger, string taskName)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _taskName = taskName;
    }

    public async Task SaveFailureStatusAsync(
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
                "{TaskName}: Failed to save error status for job {ImportJobId}",
                _taskName,
                importJobId
            );
        }
    }

    public async Task CompleteImportJobAsync(
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
                    "{TaskName}: Import {ImportJobId} completed. Processed: {Processed}, Inserted: {Inserted}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
                    _taskName,
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
                "{TaskName}: Failed to mark job {ImportJobId} as completed",
                _taskName,
                importJobId
            );
        }
    }

    public async Task UpdateProgressAsync(
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
                importJob.ProgressPercent =
                    ProgressStartPercent
                    + (int)((double)processedCount / totalRows * ProgressRangePercent);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal - just log and continue
            _logger.LogWarning(
                ex,
                "{TaskName}: Failed to update progress for job {ImportJobId}",
                _taskName,
                importJobId
            );
        }
    }

    /// <summary>
    /// Updates the import job stage using a fresh context (safe to call after entity is detached).
    /// </summary>
    public async Task UpdateStageAsync(
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
                "{TaskName}: Failed to update stage for job {ImportJobId}",
                _taskName,
                importJobId
            );
        }
    }

    /// <summary>
    /// Saves a failure status using a fresh context (safe to call after entity is detached).
    /// </summary>
    public async Task SaveFailureWithMessageAsync(
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
                "{TaskName}: Failed to save failure status for job {ImportJobId}",
                _taskName,
                importJobId
            );
        }
    }

    public static string? GetValue(Dictionary<string, string> row, string key)
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

    public static string? GetNullableValue(Dictionary<string, string> row, string key)
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

    public static string GetSafeErrorMessage(Exception ex)
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

    /// <summary>
    /// Parses a CSV byte array into a list of row dictionaries.
    /// </summary>
    /// <param name="fileBytes">The raw CSV file bytes.</param>
    /// <param name="requiredColumnName">The column name required for insert modes (e.g., "Title" or "FirstName").</param>
    /// <param name="requireColumn">Whether the required column must be present.</param>
    public static (List<Dictionary<string, string>> rows, List<string> errors) ParseCsv(
        byte[] fileBytes,
        string requiredColumnName,
        bool requireColumn = true
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

            // Required column for insert modes
            if (requireColumn && (headers == null || !headers.Contains(requiredColumnName)))
            {
                errors.Add($"Missing required column: {requiredColumnName}");
                return (rows, errors);
            }

            // Id column is always required for update-only mode
            if (!requireColumn && (headers == null || !headers.Contains("Id")))
            {
                errors.Add("Missing required column: Id (required for update mode)");
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

    /// <summary>
    /// Generates an error CSV file containing rows that failed import, with an added ImportError column.
    /// </summary>
    public async Task<Guid> GenerateErrorFileAsync(
        IFileStorageProvider fileStorage,
        ImportJob importJob,
        List<ImportErrorRow> errorRows,
        string fileNamePrefix,
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

        var fileName = $"{fileNamePrefix}-import-errors-{importJob.RequestedAt:yyyyMMdd-HHmmss}.csv";
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

    /// <summary>
    /// Shared result types for import row processing.
    /// </summary>
    public enum ImportAction
    {
        Inserted,
        Updated,
        Skipped,
        Error,
    }

    public record ImportRowResult
    {
        public ImportAction Action { get; init; }
        public string? ErrorMessage { get; init; }

        public static ImportRowResult Inserted() => new() { Action = ImportAction.Inserted };

        public static ImportRowResult Updated() => new() { Action = ImportAction.Updated };

        public static ImportRowResult Skipped() => new() { Action = ImportAction.Skipped };

        public static ImportRowResult Error(string message) =>
            new() { Action = ImportAction.Error, ErrorMessage = message };
    }

    public record ImportErrorRow
    {
        public Dictionary<string, string> Row { get; init; } = new();
        public string ErrorMessage { get; init; } = string.Empty;
    }
}
