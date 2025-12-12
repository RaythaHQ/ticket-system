using System.Text.Json;
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
/// </summary>
public class SlaEvaluationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaEvaluationJob> _logger;
    private readonly TimeSpan _evaluationInterval = TimeSpan.FromMinutes(5);

    public SlaEvaluationJob(IServiceProvider serviceProvider, ILogger<SlaEvaluationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Evaluation Job started. Interval: {Interval}", _evaluationInterval);

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
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();

        // Get all open tickets with SLA assignments
        var ticketsWithSla = await db.Tickets
            .Where(t => t.SlaRuleId != null)
            .Where(t => t.Status != TicketStatus.CLOSED && t.Status != TicketStatus.RESOLVED)
            .Where(t => t.SlaStatus != SlaStatus.BREACHED) // Skip already breached
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Evaluating SLA for {Count} tickets", ticketsWithSla.Count);

        int updatedCount = 0;
        foreach (var ticket in ticketsWithSla)
        {
            var statusChanged = await slaService.UpdateSlaStatusAsync(ticket, cancellationToken);
            if (statusChanged)
            {
                updatedCount++;
                _logger.LogInformation(
                    "Ticket {TicketId} SLA status changed to {Status}",
                    ticket.Id,
                    ticket.SlaStatus
                );
            }
        }

        if (updatedCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated SLA status for {Count} tickets", updatedCount);
        }
    }
}

