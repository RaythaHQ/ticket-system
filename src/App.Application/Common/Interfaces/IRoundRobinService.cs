using CSharpVitamins;

namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for round-robin ticket assignment within teams.
/// </summary>
public interface IRoundRobinService
{
    /// <summary>
    /// Gets the next assignee for a team using round-robin distribution.
    /// Returns null if the team has no eligible assignable members.
    /// </summary>
    Task<Guid?> GetNextAssigneeAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that an assignment was made to a member, updating last_assigned_at.
    /// </summary>
    Task RecordAssignmentAsync(Guid membershipId, CancellationToken cancellationToken = default);
}

