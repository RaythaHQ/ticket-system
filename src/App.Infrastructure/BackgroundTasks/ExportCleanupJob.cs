using App.Application.Common.Interfaces;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background service that periodically cleans up expired export jobs and their associated files.
/// Runs hourly to remove exports older than their expiration time (72 hours by default).
/// </summary>
public class ExportCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExportCleanupJob> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public ExportCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<ExportCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredExportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportCleanupJob: Error during cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredExportsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageProvider>();

        var now = DateTime.UtcNow;

        // Find expired export jobs that haven't been cleaned up
        var expiredJobs = await db.ExportJobs
            .Include(e => e.MediaItem)
            .Where(e => e.ExpiresAt < now && !e.IsCleanedUp)
            .ToListAsync(cancellationToken);

        if (!expiredJobs.Any())
        {
            return;
        }

        _logger.LogInformation(
            "ExportCleanupJob: Found {Count} expired exports to clean up",
            expiredJobs.Count);

        foreach (var job in expiredJobs)
        {
            try
            {
                // Delete file from storage if it exists
                if (job.MediaItem != null)
                {
                    await fileStorage.DeleteAsync(job.MediaItem.ObjectKey);
                    _logger.LogInformation(
                        "ExportCleanupJob: Deleted file {ObjectKey} for export {ExportJobId}",
                        job.MediaItem.ObjectKey, job.Id);
                }

                // Mark as cleaned up
                job.IsCleanedUp = true;
                job.MediaItemId = null;

                // Optionally delete the MediaItem record
                if (job.MediaItem != null)
                {
                    db.MediaItems.Remove(job.MediaItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "ExportCleanupJob: Failed to clean up export {ExportJobId}",
                    job.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ExportCleanupJob: Cleanup complete. Processed {Count} exports",
            expiredJobs.Count);
    }
}

