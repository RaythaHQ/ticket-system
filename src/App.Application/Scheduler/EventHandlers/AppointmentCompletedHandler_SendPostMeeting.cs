using App.Application.Scheduler.Services;
using App.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace App.Application.Scheduler.EventHandlers;

/// <summary>
/// Sends a post-meeting follow-up email to both the contact and the assigned staff member
/// when an appointment is marked as completed.
/// </summary>
public class AppointmentCompletedHandler_SendPostMeeting
    : INotificationHandler<AppointmentCompletedEvent>
{
    private readonly ISchedulerNotificationService _notificationService;
    private readonly ILogger<AppointmentCompletedHandler_SendPostMeeting> _logger;

    public AppointmentCompletedHandler_SendPostMeeting(
        ISchedulerNotificationService notificationService,
        ILogger<AppointmentCompletedHandler_SendPostMeeting> logger
    )
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async ValueTask Handle(
        AppointmentCompletedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Sending post-meeting notification for appointment {AppointmentId}",
                notification.Appointment.Id
            );

            await _notificationService.SendPostMeetingAsync(
                notification.Appointment.Id,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send post-meeting notification for appointment {AppointmentId}",
                notification.Appointment.Id
            );
        }
    }
}
