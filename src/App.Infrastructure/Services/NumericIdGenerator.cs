using App.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

/// <summary>
/// Generates unique numeric IDs for Contacts and Tickets.
/// Contact IDs start at a minimum of 7 digits (1000000) when auto-generated.
/// Ticket IDs start at 1 and increment normally.
/// </summary>
public class NumericIdGenerator : INumericIdGenerator
{
    private readonly IAppDbContext _db;
    private const long MinimumContactId = 1_000_000; // 7 digits minimum for contacts

    public NumericIdGenerator(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<long> GetNextContactIdAsync(CancellationToken cancellationToken = default)
    {
        // Get the current maximum ID, including soft-deleted records
        var maxId =
            await _db.Contacts.IgnoreQueryFilters().MaxAsync(c => (long?)c.Id, cancellationToken)
            ?? 0;

        // Return the greater of (maxId + 1) or the minimum starting ID (7 digits)
        return Math.Max(maxId + 1, MinimumContactId);
    }

    public async Task<long> GetNextTicketIdAsync(CancellationToken cancellationToken = default)
    {
        // Get the current maximum ID, including soft-deleted records
        var maxId =
            await _db.Tickets.IgnoreQueryFilters().MaxAsync(t => (long?)t.Id, cancellationToken)
            ?? 0;

        // Tickets start at 1, no minimum requirement
        return maxId + 1;
    }
}
