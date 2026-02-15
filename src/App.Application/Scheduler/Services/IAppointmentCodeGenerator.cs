namespace App.Application.Scheduler.Services;

/// <summary>
/// Generates unique numeric IDs for appointments.
/// Appointments use human-readable codes formatted as APT-{Id:D4}.
/// </summary>
public interface IAppointmentCodeGenerator
{
    /// <summary>
    /// Gets the next available appointment ID.
    /// </summary>
    Task<long> GetNextAppointmentIdAsync(CancellationToken cancellationToken = default);
}
