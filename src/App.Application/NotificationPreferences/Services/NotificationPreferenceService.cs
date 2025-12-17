using App.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.Application.NotificationPreferences.Services;

/// <summary>
/// Service for checking user notification preferences.
/// </summary>
public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly IAppDbContext _db;

    public NotificationPreferenceService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsEmailEnabledAsync(
        Guid userId,
        string eventType,
        CancellationToken cancellationToken = default
    )
    {
        var preference = await _db
            .NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.StaffAdminId == userId && p.EventType == eventType,
                cancellationToken
            );

        // Default to enabled if no preference exists
        return preference?.EmailEnabled ?? true;
    }

    public async Task<bool> IsInAppEnabledAsync(
        Guid userId,
        string eventType,
        CancellationToken cancellationToken = default
    )
    {
        var preference = await _db
            .NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.StaffAdminId == userId && p.EventType == eventType,
                cancellationToken
            );

        // Default to enabled if no preference exists
        return preference?.InAppEnabled ?? true;
    }

    public async Task<List<Guid>> FilterUsersWithEmailEnabledAsync(
        IEnumerable<Guid> userIds,
        string eventType,
        CancellationToken cancellationToken = default
    )
    {
        var userIdList = userIds.ToList();
        if (!userIdList.Any())
            return new List<Guid>();

        // Get preferences for these users for this event type
        var preferences = await _db
            .NotificationPreferences.AsNoTracking()
            .Where(p => userIdList.Contains(p.StaffAdminId) && p.EventType == eventType)
            .ToListAsync(cancellationToken);

        var result = new List<Guid>();
        foreach (var userId in userIdList)
        {
            var pref = preferences.FirstOrDefault(p => p.StaffAdminId == userId);
            // Default to enabled if no preference exists
            if (pref?.EmailEnabled ?? true)
            {
                result.Add(userId);
            }
        }

        return result;
    }

    public async Task<List<Guid>> FilterUsersWithInAppEnabledAsync(
        IEnumerable<Guid> userIds,
        string eventType,
        CancellationToken cancellationToken = default
    )
    {
        var userIdList = userIds.ToList();
        if (!userIdList.Any())
            return new List<Guid>();

        // Get preferences for these users for this event type
        var preferences = await _db
            .NotificationPreferences.AsNoTracking()
            .Where(p => userIdList.Contains(p.StaffAdminId) && p.EventType == eventType)
            .ToListAsync(cancellationToken);

        var result = new List<Guid>();
        foreach (var userId in userIdList)
        {
            var pref = preferences.FirstOrDefault(p => p.StaffAdminId == userId);
            // Default to enabled if no preference exists
            if (pref?.InAppEnabled ?? true)
            {
                result.Add(userId);
            }
        }

        return result;
    }
}
