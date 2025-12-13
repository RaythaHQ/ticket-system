namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for generating unique numeric IDs for entities like Contacts and Tickets.
/// </summary>
public interface INumericIdGenerator
{
    /// <summary>
    /// Gets the next available ID for a Contact.
    /// Returns at minimum 1000000 (7 digits) for new systems.
    /// </summary>
    Task<long> GetNextContactIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next available ID for a Ticket.
    /// Starts at 1 and increments normally.
    /// </summary>
    Task<long> GetNextTicketIdAsync(CancellationToken cancellationToken = default);
}

