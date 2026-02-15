using App.Application.Common.Interfaces;
using App.Application.Scheduler.Services;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

/// <summary>
/// Validates that a contact's zipcode falls within the coverage zones of the assigned staff member.
/// Falls back to org-wide default coverage zones if the staff member has none configured.
/// </summary>
public class CoverageZoneValidator : ICoverageZoneValidator
{
    private readonly IAppDbContext _db;

    public CoverageZoneValidator(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool IsValid, string? WarningMessage)> ValidateAsync(
        long contactId,
        Guid staffMemberId,
        CancellationToken cancellationToken = default
    )
    {
        var contact = await _db
            .Contacts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);

        if (contact == null)
            return (false, "Contact not found.");

        // If the contact has no zipcode, skip validation (no zone to check against)
        if (string.IsNullOrWhiteSpace(contact.Zipcode))
            return (true, null);

        var staffMember = await _db
            .SchedulerStaffMembers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == staffMemberId, cancellationToken);

        if (staffMember == null)
            return (false, "Staff member not found.");

        // Determine applicable coverage zones: staff-specific > org-default
        var coverageZones = staffMember.CoverageZones;

        if (coverageZones.Count == 0)
        {
            // Fall back to org-wide default coverage zones
            var orgConfig = await _db
                .SchedulerConfigurations.AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (orgConfig != null)
            {
                coverageZones = orgConfig.DefaultCoverageZones;
            }
        }

        // If no coverage zones are configured at all, allow everything
        if (coverageZones.Count == 0)
            return (true, null);

        // Check if the contact's zipcode is in the coverage zones
        var contactZipcode = contact.Zipcode.Trim();
        if (coverageZones.Any(z => z.Trim().Equals(contactZipcode, StringComparison.OrdinalIgnoreCase)))
        {
            return (true, null);
        }

        return (
            false,
            $"Contact's zipcode ({contactZipcode}) is outside the coverage zone for this staff member. "
                + "An override reason is required to proceed."
        );
    }
}
