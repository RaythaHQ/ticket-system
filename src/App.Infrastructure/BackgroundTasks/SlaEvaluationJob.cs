using App.Application.Common.Interfaces;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job that periodically evaluates SLA status for all open tickets.
/// Runs on a timer and updates tickets that are approaching breach or have breached.
/// Processes tickets in batches to avoid loading too many entities into memory at once.
/// </summary>
public class SlaEvaluationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaEvaluationJob> _logger;
    private readonly TimeSpan _evaluationInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 100;

    public SlaEvaluationJob(IServiceProvider serviceProvider, ILogger<SlaEvaluationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SLA Evaluation Job started. Interval: {Interval}, BatchSize: {BatchSize}",
            _evaluationInterval,
            BatchSize
        );

        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAllTicketsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SLA evaluation");
            }

            await Task.Delay(_evaluationInterval, stoppingToken);
        }
    }

    private async Task EvaluateAllTicketsAsync(CancellationToken cancellationToken)
    {
        int totalUpdated = 0;
        int batchNumber = 0;
        bool hasMoreTickets = true;

        while (hasMoreTickets)
        {
            // Create a fresh scope for each batch to prevent DbContext bloat
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();

            // Get only ticket IDs first (lighter query)
            var ticketIds = await db
                .Tickets.AsNoTracking()
                .Where(t => t.SlaRuleId != null)
                .Where(t => t.Status != TicketStatus.CLOSED && t.Status != TicketStatus.RESOLVED)
                .Where(t => t.SlaStatus != SlaStatus.BREACHED)
                .OrderBy(t => t.Id)
                .Skip(batchNumber * BatchSize)
                .Take(BatchSize)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            if (ticketIds.Count == 0)
            {
                hasMoreTickets = false;
                break;
            }

            if (ticketIds.Count < BatchSize)
            {
                hasMoreTickets = false;
            }

            _logger.LogDebug(
                "Processing SLA batch {BatchNumber} with {Count} tickets",
                batchNumber + 1,
                ticketIds.Count
            );

            // Now load and process tickets with tracking
            var tickets = await db
                .Tickets.Where(t => ticketIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            int batchUpdated = 0;
            foreach (var ticket in tickets)
            {
                var statusChanged = await slaService.UpdateSlaStatusAsync(
                    ticket,
                    cancellationToken
                );
                if (statusChanged)
                {
                    batchUpdated++;
                    _logger.LogInformation(
                        "Ticket {TicketId} SLA status changed to {Status}",
                        ticket.Id,
                        ticket.SlaStatus
                    );
                }
            }

            if (batchUpdated > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                totalUpdated += batchUpdated;
            }

            batchNumber++;
        }

        if (totalUpdated > 0)
        {
            _logger.LogInformation(
                "SLA evaluation complete. Updated {Count} tickets across {Batches} batch(es)",
                totalUpdated,
                batchNumber
            );
        }
    }
}
