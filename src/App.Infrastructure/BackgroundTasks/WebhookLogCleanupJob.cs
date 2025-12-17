using App.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job that periodically cleans up old webhook logs.
/// Runs once per day to:
/// - Delete logs older than 30 days
/// - Ensure total log count doesn't exceed 10,000 entries
/// </summary>
public class WebhookLogCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookLogCleanupJob> _logger;

    private const int MaxLogEntries = 10_000;
    private const int RetentionDays = 30;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);

    public WebhookLogCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<WebhookLogCleanupJob> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before first run to let the app start up
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during webhook log cleanup");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

        // Delete logs older than retention period
        var oldLogsDeleted = await db.DbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"WebhookLogs\" WHERE \"CreatedAt\" < {0}",
            new object[] { cutoffDate },
            cancellationToken
        );

        if (oldLogsDeleted > 0)
        {
            _logger.LogInformation(
                "Webhook log cleanup: Deleted {Count} logs older than {Days} days",
                oldLogsDeleted,
                RetentionDays
            );
        }

        // Check total count and delete oldest if over limit
        var totalCount = await db.WebhookLogs.CountAsync(cancellationToken);
        if (totalCount > MaxLogEntries)
        {
            var excessCount = totalCount - MaxLogEntries;

            // Get the cutoff ID (oldest entries beyond the limit)
            var cutoffId = await db
                .WebhookLogs.AsNoTracking()
                .OrderBy(l => l.CreatedAt)
                .Skip(excessCount - 1)
                .Select(l => l.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (cutoffId != Guid.Empty)
            {
                // Get the cutoff timestamp
                var cutoffTimestamp = await db
                    .WebhookLogs.AsNoTracking()
                    .Where(l => l.Id == cutoffId)
                    .Select(l => l.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var excessDeleted = await db.DbContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM \"WebhookLogs\" WHERE \"CreatedAt\" <= {0} AND \"Id\" != {1}",
                    new object[] { cutoffTimestamp, cutoffId },
                    cancellationToken
                );

                _logger.LogInformation(
                    "Webhook log cleanup: Deleted {Count} excess logs to maintain {Max} entry limit",
                    excessDeleted,
                    MaxLogEntries
                );
            }
        }
    }
}
