namespace App.Application.Scheduler.Services;

/// <summary>
/// Validates that a contact's zipcode falls within a staff member's (or org default) coverage zone.
/// Returns a soft warning allowing override with reason when the contact is outside the zone.
/// </summary>
public interface ICoverageZoneValidator
{
    /// <summary>
    /// Validates coverage zone compatibility between a contact and a staff member.
    /// </summary>
    /// <param name="contactId">The contact's ID.</param>
    /// <param name="staffMemberId">The scheduler staff member's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// IsValid: true if the contact is within the coverage zone (or no zones are configured).
    /// WarningMessage: non-null if the contact is outside the zone.
    /// </returns>
    Task<(bool IsValid, string? WarningMessage)> ValidateAsync(
        long contactId,
        Guid staffMemberId,
        CancellationToken cancellationToken = default
    );
}
