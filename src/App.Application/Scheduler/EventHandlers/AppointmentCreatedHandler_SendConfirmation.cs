using App.Application.Scheduler.Services;
using App.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace App.Application.Scheduler.EventHandlers;

/// <summary>
/// Sends a confirmation email to both the contact and the assigned staff member
/// when a new appointment is created.
/// </summary>
public class AppointmentCreatedHandler_SendConfirmation
    : INotificationHandler<AppointmentCreatedEvent>
{
    private readonly ISchedulerNotificationService _notificationService;
    private readonly ILogger<AppointmentCreatedHandler_SendConfirmation> _logger;

    public AppointmentCreatedHandler_SendConfirmation(
        ISchedulerNotificationService notificationService,
        ILogger<AppointmentCreatedHandler_SendConfirmation> logger
    )
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async ValueTask Handle(
        AppointmentCreatedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Sending confirmation notification for appointment {AppointmentId}",
                notification.Appointment.Id
            );

            await _notificationService.SendConfirmationAsync(
                notification.Appointment.Id,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send confirmation notification for appointment {AppointmentId}",
                notification.Appointment.Id
            );
        }
    }
}
