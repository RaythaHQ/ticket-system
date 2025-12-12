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
}

