using App.Domain.Entities;

namespace App.Domain.Events;

public class AppointmentRescheduledEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Appointment Appointment { get; private set; }
    public DateTime OldStartTime { get; private set; }
    public DateTime NewStartTime { get; private set; }
    public int OldDurationMinutes { get; private set; }
    public int NewDurationMinutes { get; private set; }

    public AppointmentRescheduledEvent(
        Appointment appointment,
        DateTime oldStartTime,
        DateTime newStartTime,
        int oldDurationMinutes,
        int newDurationMinutes
    )
    {
        Appointment = appointment;
        OldStartTime = oldStartTime;
        NewStartTime = newStartTime;
        OldDurationMinutes = oldDurationMinutes;
        NewDurationMinutes = newDurationMinutes;
    }
}
