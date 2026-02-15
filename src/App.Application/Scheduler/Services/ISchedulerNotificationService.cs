namespace App.Application.Scheduler.Services;

public interface ISchedulerNotificationService
{
    Task SendConfirmationAsync(long appointmentId, CancellationToken cancellationToken = default);
    Task SendReminderAsync(long appointmentId, CancellationToken cancellationToken = default);
    Task SendPostMeetingAsync(long appointmentId, CancellationToken cancellationToken = default);
}
