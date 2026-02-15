using App.Domain.Entities;

namespace App.Domain.Events;

public class AppointmentCompletedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Appointment Appointment { get; private set; }

    public AppointmentCompletedEvent(Appointment appointment)
    {
        Appointment = appointment;
    }
}
