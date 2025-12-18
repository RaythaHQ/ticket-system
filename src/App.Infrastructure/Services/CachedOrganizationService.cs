using App.Application.AuthenticationSchemes;
using App.Application.Common.Interfaces;
using App.Application.OrganizationSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace App.Infrastructure.Services;

/// <summary>
/// Provides cached access to organization settings and authentication schemes.
/// These values are cached because they rarely change and are accessed on every page request.
/// </summary>
public interface ICachedOrganizationService
{
    /// <summary>
    /// Gets the organization settings, cached for performance.
    /// </summary>
    Task<OrganizationSettingsDto?> GetOrganizationSettingsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the authentication schemes, cached for performance.
    /// </summary>
    Task<IEnumerable<AuthenticationSchemeDto>> GetAuthenticationSchemesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Invalidates the organization settings cache. Call this when settings are updated.
    /// </summary>
    void InvalidateOrganizationSettingsCache();

    /// <summary>
    /// Invalidates the authentication schemes cache. Call this when schemes are updated.
    /// </summary>
    void InvalidateAuthenticationSchemesCache();
}

public class CachedOrganizationService : ICachedOrganizationService
{
    private readonly IAppDbContext _db;
    private readonly IMemoryCache _cache;

    private const string OrganizationSettingsCacheKey = "OrganizationSettings";
    private const string AuthenticationSchemesCacheKey = "AuthenticationSchemes";

    // Cache for 5 minutes - balance between performance and freshness
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedOrganizationService(IAppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<OrganizationSettingsDto?> GetOrganizationSettingsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_cache.TryGetValue(OrganizationSettingsCacheKey, out OrganizationSettingsDto? cached))
        {
            return cached;
        }

        var settings = await _db
            .OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var dto = settings != null ? OrganizationSettingsDto.GetProjection(settings) : null;

        _cache.Set(OrganizationSettingsCacheKey, dto, CacheDuration);

        return dto;
    }

    public async Task<IEnumerable<AuthenticationSchemeDto>> GetAuthenticationSchemesAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (
            _cache.TryGetValue(
                AuthenticationSchemesCacheKey,
                out IEnumerable<AuthenticationSchemeDto>? cached
            )
        )
        {
            return cached ?? Enumerable.Empty<AuthenticationSchemeDto>();
        }

        var schemes = await _db
            .AuthenticationSchemes.AsNoTracking()
            .Select(AuthenticationSchemeDto.GetProjection())
            .ToListAsync(cancellationToken);

        _cache.Set(AuthenticationSchemesCacheKey, schemes, CacheDuration);

        return schemes;
    }

    public void InvalidateOrganizationSettingsCache()
    {
        _cache.Remove(OrganizationSettingsCacheKey);
    }

    public void InvalidateAuthenticationSchemesCache()
    {
        _cache.Remove(AuthenticationSchemesCacheKey);
    }
}
