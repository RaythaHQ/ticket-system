using App.Domain.Entities;

namespace App.Domain.Events;

public class AppointmentStatusChangedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Appointment Appointment { get; private set; }
    public string OldStatus { get; private set; }
    public string NewStatus { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    public AppointmentStatusChangedEvent(
        Appointment appointment,
        string oldStatus,
        string newStatus,
        Guid? changedByUserId = null
    )
    {
        Appointment = appointment;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
    }
}
