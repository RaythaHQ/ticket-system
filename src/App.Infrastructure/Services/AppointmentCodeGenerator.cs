using App.Application.Common.Interfaces;
using App.Application.Scheduler.Services;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

/// <summary>
/// Generates unique numeric IDs for appointments.
/// Follows the same INumericIdGenerator pattern used by Tickets and Contacts.
/// </summary>
public class AppointmentCodeGenerator : IAppointmentCodeGenerator
{
    private readonly IAppDbContext _db;

    public AppointmentCodeGenerator(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<long> GetNextAppointmentIdAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Get the current maximum ID, including soft-deleted records
        var maxId =
            await _db
                .Appointments.IgnoreQueryFilters()
                .MaxAsync(a => (long?)a.Id, cancellationToken) ?? 0;

        // Appointments start at 1, no minimum requirement
        return maxId + 1;
    }
}
