using App.Application.Common.Interfaces;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Services;

/// <summary>
/// Provides badge counts for the staff sidebar navigation.
/// Queries the database for open tickets and tasks assigned to the current user.
/// </summary>
public class SidebarBadgeService : ISidebarBadgeService
{
    private readonly IAppDbContext _db;

    public SidebarBadgeService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetMyTicketCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Get all status developer names that are of type "open"
        var openStatuses = await _db.TicketStatusConfigs
            .AsNoTracking()
            .Where(s => s.StatusType == TicketStatusType.OPEN)
            .Select(s => s.DeveloperName)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return await _db.Tickets
            .AsNoTracking()
            .CountAsync(t => t.AssigneeId == userId
                && openStatuses.Contains(t.Status)
                && (t.SnoozedUntil == null || t.SnoozedUntil <= now),
                cancellationToken);
    }

    public async Task<int> GetMyTaskCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.TicketTasks
            .AsNoTracking()
            .CountAsync(t => t.AssigneeId == userId
                && t.Status != TicketTaskStatus.CLOSED
                && (t.DependsOnTaskId == null || t.DependsOnTask!.Status == TicketTaskStatus.CLOSED),
                cancellationToken);
    }
}
