using App.Domain.Entities;

namespace App.Application.Common.Interfaces;

/// <summary>
/// Service for SLA evaluation and assignment.
/// </summary>
public interface ISlaService
{
    /// <summary>
    /// Evaluates a ticket against all active SLA rules and assigns the first matching rule.
    /// </summary>
    Task<SlaRule?> EvaluateAndAssignSlaAsync(Ticket ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates the SLA due date for a ticket based on its assigned SLA rule.
    /// </summary>
    DateTime? CalculateSlaDueDate(Ticket ticket, SlaRule rule);

    /// <summary>
    /// Updates the SLA status for a ticket based on current time.
    /// Returns true if status changed.
    /// </summary>
    Task<bool> UpdateSlaStatusAsync(Ticket ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the default number of hours to extend an SLA to reach
    /// 4pm the next business day in the organization's timezone.
    /// </summary>
    /// <param name="currentSlaDueAt">Current SLA due date, or null to calculate from now.</param>
    /// <param name="timezone">Organization timezone (IANA format), defaults to UTC.</param>
    /// <returns>Number of hours to extend by.</returns>
    int CalculateDefaultExtensionHours(DateTime? currentSlaDueAt, string? timezone);

    /// <summary>
    /// Calculates the new SLA due date after extending by the specified hours.
    /// </summary>
    /// <param name="currentSlaDueAt">Current SLA due date, or null to start from now.</param>
    /// <param name="extensionHours">Hours to add.</param>
    /// <returns>New due date in UTC.</returns>
    DateTime CalculateExtendedDueDate(DateTime? currentSlaDueAt, int extensionHours);
}

