using App.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Teams.Services;

/// <summary>
/// Implementation of round-robin ticket assignment within teams.
/// </summary>
public class RoundRobinService : IRoundRobinService
{
    private readonly IAppDbContext _db;

    public RoundRobinService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid?> GetNextAssigneeAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await _db.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        // Team doesn't exist or round-robin is not enabled
        if (team == null || !team.RoundRobinEnabled)
            return null;

        // Get all eligible members (assignable = true) ordered by last_assigned_at (nulls first = never assigned)
        var eligibleMembers = await _db.TeamMemberships
            .Include(m => m.StaffAdmin)
            .Where(m => m.TeamId == teamId && m.IsAssignable)
            .Where(m => m.StaffAdmin != null && m.StaffAdmin.IsActive) // Only active staff
            .OrderBy(m => m.LastAssignedAt ?? DateTime.MinValue) // Null = never assigned, gets priority
            .ToListAsync(cancellationToken);

        if (!eligibleMembers.Any())
            return null;

        // Return the first member (least recently assigned or never assigned)
        return eligibleMembers.First().StaffAdminId;
    }

    public async Task RecordAssignmentAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        var membership = await _db.TeamMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken);

        if (membership != null)
        {
            membership.LastAssignedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

