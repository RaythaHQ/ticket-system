using App.Application.Common.Interfaces;
using App.Application.Scheduler.Services;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job that periodically checks for upcoming appointments that need reminder
/// notifications sent. Runs every 5 minutes and processes appointments where:
/// - Status is active (scheduled or confirmed)
/// - ScheduledStartTime minus now is within ReminderLeadTimeMinutes (from config)
/// - ReminderSentAt is null (not yet sent)
/// </summary>
public class AppointmentReminderJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentReminderJob> _logger;
    private readonly TimeSpan _evaluationInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 100;

    public AppointmentReminderJob(
        IServiceProvider serviceProvider,
        ILogger<AppointmentReminderJob> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Appointment Reminder Job started. Interval: {Interval}, BatchSize: {BatchSize}",
            _evaluationInterval,
            BatchSize
        );

        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendPendingRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during appointment reminder evaluation");
            }

            await Task.Delay(_evaluationInterval, stoppingToken);
        }
    }

    private async Task SendPendingRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var notificationService =
            scope.ServiceProvider.GetRequiredService<ISchedulerNotificationService>();

        // Load the scheduler configuration to get ReminderLeadTimeMinutes
        var config = await db
            .SchedulerConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
        {
            _logger.LogDebug("No scheduler configuration found; skipping reminder evaluation");
            return;
        }

        var reminderLeadTime = TimeSpan.FromMinutes(config.ReminderLeadTimeMinutes);
        var now = DateTime.UtcNow;
        var reminderThreshold = now.Add(reminderLeadTime);

        // Find appointments where:
        // - Status is active (scheduled or confirmed)
        // - Scheduled start is within the reminder window (now + lead time)
        // - Reminder hasn't been sent yet
        var activeStatuses = new[]
        {
            AppointmentStatus.SCHEDULED,
            AppointmentStatus.CONFIRMED,
        };

        var appointments = await db
            .Appointments.Where(a =>
                activeStatuses.Contains(a.Status)
                && a.ScheduledStartTime <= reminderThreshold
                && a.ScheduledStartTime > now
                && a.ReminderSentAt == null
            )
            .OrderBy(a => a.ScheduledStartTime)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0)
        {
            return;
        }

        _logger.LogDebug(
            "Found {Count} appointments needing reminder notifications",
            appointments.Count
        );

        int sentCount = 0;

        foreach (var appointment in appointments)
        {
            try
            {
                await notificationService.SendReminderAsync(
                    appointment.Id,
                    cancellationToken
                );

                // Mark reminder as sent
                appointment.ReminderSentAt = DateTime.UtcNow;
                sentCount++;

                _logger.LogInformation(
                    "Sent reminder for appointment {AppointmentId} (scheduled at {ScheduledStartTime})",
                    appointment.Id,
                    appointment.ScheduledStartTime
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send reminder for appointment {AppointmentId}",
                    appointment.Id
                );
            }
        }

        if (sentCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Appointment reminder evaluation complete. Sent {Count} reminder(s)",
                sentCount
            );
        }
    }
}
