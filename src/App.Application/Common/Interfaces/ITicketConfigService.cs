using App.Application.TicketConfig;

namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for retrieving ticket priority and status configurations.
/// Provides cached lookups for performance and consistent access.
/// </summary>
public interface ITicketConfigService
{
    // Priority methods
    Task<TicketPriorityConfigDto?> GetPriorityByDeveloperNameAsync(string developerName, CancellationToken cancellationToken = default);
    Task<TicketPriorityConfigDto> GetDefaultPriorityAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketPriorityConfigDto>> GetAllPrioritiesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketPriorityConfigDto>> GetActivePrioritiesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsPriorityValidAsync(string developerName, CancellationToken cancellationToken = default);

    // Status methods
    Task<TicketStatusConfigDto?> GetStatusByDeveloperNameAsync(string developerName, CancellationToken cancellationToken = default);
    Task<TicketStatusConfigDto> GetDefaultStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketStatusConfigDto>> GetAllStatusesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketStatusConfigDto>> GetActiveStatusesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsStatusValidAsync(string developerName, CancellationToken cancellationToken = default);
    Task<bool> IsStatusOpenTypeAsync(string developerName, CancellationToken cancellationToken = default);
    Task<bool> IsStatusClosedTypeAsync(string developerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears any cached configuration data. Call after modifying priorities or statuses.
    /// </summary>
    void InvalidateCache();
}

