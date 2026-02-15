using App.Domain.Entities;

namespace App.Domain.Events;

public class AppointmentCreatedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public Appointment Appointment { get; private set; }

    public AppointmentCreatedEvent(Appointment appointment)
    {
        Appointment = appointment;
    }
}
