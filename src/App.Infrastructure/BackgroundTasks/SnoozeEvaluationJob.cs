using App.Application.Common.Interfaces;
using App.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job that periodically checks for snoozed tickets that should be unsnoozed.
/// Runs every 5 minutes and processes tickets whose SnoozedUntil time has passed.
/// </summary>
public class SnoozeEvaluationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SnoozeEvaluationJob> _logger;
    private readonly TimeSpan _evaluationInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 100;

    public SnoozeEvaluationJob(IServiceProvider serviceProvider, ILogger<SnoozeEvaluationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Snooze Evaluation Job started. Interval: {Interval}, BatchSize: {BatchSize}",
            _evaluationInterval,
            BatchSize
        );

        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UnsnoozeExpiredTicketsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during snooze evaluation");
            }

            await Task.Delay(_evaluationInterval, stoppingToken);
        }
    }

    private async Task UnsnoozeExpiredTicketsAsync(CancellationToken cancellationToken)
    {
        int totalUnsnoozed = 0;
        var now = DateTime.UtcNow;

        // Create a fresh scope for processing
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // Get organization settings for PauseSlaOnSnooze flag
        var orgSettings = await db.OrganizationSettings.FirstOrDefaultAsync(cancellationToken);
        var pauseSlaOnSnooze = orgSettings?.PauseSlaOnSnooze ?? false;

        // Find all tickets where snooze time has passed
        // Uses the partial index on SnoozedUntil for efficiency
        var expiredTickets = await db
            .Tickets.Where(t => t.SnoozedUntil != null && t.SnoozedUntil <= now)
            .OrderBy(t => t.SnoozedUntil)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (expiredTickets.Count == 0)
        {
            return;
        }

        _logger.LogDebug(
            "Found {Count} tickets to auto-unsnooze",
            expiredTickets.Count
        );

        foreach (var ticket in expiredTickets)
        {
            var snoozeDuration =
                ticket.SnoozedAt != null
                    ? now - ticket.SnoozedAt.Value
                    : TimeSpan.Zero;

            // Extend SLA due time by snooze duration if PauseSlaOnSnooze is enabled
            if (pauseSlaOnSnooze && ticket.SlaDueAt != null && snoozeDuration > TimeSpan.Zero)
            {
                ticket.SlaDueAt = ticket.SlaDueAt.Value.Add(snoozeDuration);
                _logger.LogDebug(
                    "Extended SLA for ticket {TicketId} by {Duration} (new due: {SlaDueAt})",
                    ticket.Id,
                    snoozeDuration,
                    ticket.SlaDueAt
                );
            }

            // Clear snooze fields
            ticket.SnoozedUntil = null;
            ticket.SnoozedAt = null;
            ticket.SnoozedById = null;
            ticket.SnoozedReason = null;
            ticket.UnsnoozedAt = now;

            // Raise event for notification handling and SLA extension
            ticket.AddDomainEvent(
                new TicketUnsnoozedEvent(
                    ticket,
                    unsnoozedById: null, // System auto-unsnooze
                    wasAutoUnsnooze: true,
                    snoozeDuration: snoozeDuration
                )
            );

            totalUnsnoozed++;

            _logger.LogInformation(
                "Auto-unsnoozed ticket {TicketId} (was snoozed for {Duration})",
                ticket.Id,
                snoozeDuration
            );
        }

        if (totalUnsnoozed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Snooze evaluation complete. Auto-unsnoozed {Count} ticket(s)",
                totalUnsnoozed
            );
        }
    }
}
