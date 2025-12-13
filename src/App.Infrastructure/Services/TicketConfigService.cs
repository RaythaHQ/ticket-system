using App.Application.Common.Interfaces;
using App.Application.TicketConfig;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace App.Infrastructure.Services;

/// <summary>
/// Implementation of ITicketConfigService with in-memory caching.
/// </summary>
public class TicketConfigService : ITicketConfigService
{
    private readonly IAppDbContext _db;
    private readonly IMemoryCache _cache;

    private const string PrioritiesCacheKey = "TicketPriorityConfigs";
    private const string StatusesCacheKey = "TicketStatusConfigs";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public TicketConfigService(IAppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    #region Priority Methods

    public async Task<TicketPriorityConfigDto?> GetPriorityByDeveloperNameAsync(string developerName, CancellationToken cancellationToken = default)
    {
        var priorities = await GetAllPrioritiesInternalAsync(cancellationToken);
        return priorities.FirstOrDefault(p => p.DeveloperName.Equals(developerName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TicketPriorityConfigDto> GetDefaultPriorityAsync(CancellationToken cancellationToken = default)
    {
        var priorities = await GetAllPrioritiesInternalAsync(cancellationToken);
        var defaultPriority = priorities.FirstOrDefault(p => p.IsDefault && p.IsActive);
        
        if (defaultPriority == null)
        {
            // Fallback to first active priority
            defaultPriority = priorities.Where(p => p.IsActive).OrderBy(p => p.SortOrder).FirstOrDefault();
        }

        if (defaultPriority == null)
        {
            throw new InvalidOperationException("No active ticket priorities configured. Please configure at least one priority.");
        }

        return defaultPriority;
    }

    public async Task<IReadOnlyList<TicketPriorityConfigDto>> GetAllPrioritiesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var priorities = await GetAllPrioritiesInternalAsync(cancellationToken);
        
        if (!includeInactive)
        {
            return priorities.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
        }

        return priorities.OrderBy(p => p.SortOrder).ToList();
    }

    public async Task<IReadOnlyList<TicketPriorityConfigDto>> GetActivePrioritiesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllPrioritiesAsync(includeInactive: false, cancellationToken);
    }

    public async Task<bool> IsPriorityValidAsync(string developerName, CancellationToken cancellationToken = default)
    {
        var priority = await GetPriorityByDeveloperNameAsync(developerName, cancellationToken);
        return priority != null && priority.IsActive;
    }

    private async Task<IReadOnlyList<TicketPriorityConfigDto>> GetAllPrioritiesInternalAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(PrioritiesCacheKey, out IReadOnlyList<TicketPriorityConfigDto>? cached) && cached != null)
        {
            return cached;
        }

        var priorities = await _db.TicketPriorityConfigs
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var dtos = priorities.Select(TicketPriorityConfigDto.MapFrom).ToList();

        _cache.Set(PrioritiesCacheKey, dtos, CacheDuration);

        return dtos;
    }

    #endregion

    #region Status Methods

    public async Task<TicketStatusConfigDto?> GetStatusByDeveloperNameAsync(string developerName, CancellationToken cancellationToken = default)
    {
        var statuses = await GetAllStatusesInternalAsync(cancellationToken);
        return statuses.FirstOrDefault(s => s.DeveloperName.Equals(developerName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TicketStatusConfigDto> GetDefaultStatusAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await GetAllStatusesInternalAsync(cancellationToken);
        
        // Default is the first active status (lowest SortOrder), which must be Open type
        var defaultStatus = statuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .FirstOrDefault();

        if (defaultStatus == null)
        {
            throw new InvalidOperationException("No active ticket statuses configured. Please configure at least one status.");
        }

        return defaultStatus;
    }

    public async Task<IReadOnlyList<TicketStatusConfigDto>> GetAllStatusesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var statuses = await GetAllStatusesInternalAsync(cancellationToken);
        
        if (!includeInactive)
        {
            return statuses.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToList();
        }

        return statuses.OrderBy(s => s.SortOrder).ToList();
    }

    public async Task<IReadOnlyList<TicketStatusConfigDto>> GetActiveStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllStatusesAsync(includeInactive: false, cancellationToken);
    }

    public async Task<bool> IsStatusValidAsync(string developerName, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusByDeveloperNameAsync(developerName, cancellationToken);
        return status != null && status.IsActive;
    }

    public async Task<bool> IsStatusOpenTypeAsync(string developerName, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusByDeveloperNameAsync(developerName, cancellationToken);
        return status?.StatusType == TicketStatusType.OPEN;
    }

    public async Task<bool> IsStatusClosedTypeAsync(string developerName, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusByDeveloperNameAsync(developerName, cancellationToken);
        return status?.StatusType == TicketStatusType.CLOSED;
    }

    private async Task<IReadOnlyList<TicketStatusConfigDto>> GetAllStatusesInternalAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(StatusesCacheKey, out IReadOnlyList<TicketStatusConfigDto>? cached) && cached != null)
        {
            return cached;
        }

        var statuses = await _db.TicketStatusConfigs
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        var dtos = statuses.Select(TicketStatusConfigDto.MapFrom).ToList();

        _cache.Set(StatusesCacheKey, dtos, CacheDuration);

        return dtos;
    }

    #endregion

    public void InvalidateCache()
    {
        _cache.Remove(PrioritiesCacheKey);
        _cache.Remove(StatusesCacheKey);
    }
}

