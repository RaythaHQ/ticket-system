using App.Application.Common.Interfaces;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace App.Application.SlaRules.Services;

/// <summary>
/// Implementation of SLA evaluation and status management.
/// </summary>
public class SlaService : ISlaService
{
    private readonly IAppDbContext _db;
    private const double ApproachingBreachThreshold = 0.75; // 75% of time elapsed

    public SlaService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<SlaRule?> EvaluateAndAssignSlaAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        // Get all active SLA rules ordered by priority
        var rules = await _db.SlaRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (MatchesConditions(ticket, rule))
            {
                // Assign SLA to ticket
                ticket.SlaRuleId = rule.Id;
                ticket.SlaDueAt = CalculateSlaDueDate(ticket, rule);
                ticket.SlaStatus = SlaStatus.ON_TRACK;
                ticket.SlaBreachedAt = null;

                return rule;
            }
        }

        // No matching rule
        ticket.SlaRuleId = null;
        ticket.SlaDueAt = null;
        ticket.SlaStatus = null;
        ticket.SlaBreachedAt = null;

        return null;
    }

    public DateTime? CalculateSlaDueDate(Ticket ticket, SlaRule rule)
    {
        var startTime = ticket.CreationTime;

        if (!rule.BusinessHoursEnabled)
        {
            // Simple calculation without business hours
            return startTime.AddMinutes(rule.TargetResolutionMinutes);
        }

        // Business hours calculation
        BusinessHoursConfig? config = null;
        if (!string.IsNullOrEmpty(rule.BusinessHoursConfigJson))
        {
            try { config = JsonSerializer.Deserialize<BusinessHoursConfig>(rule.BusinessHoursConfigJson); }
            catch { }
        }

        if (config == null)
        {
            // Fallback to simple calculation
            return startTime.AddMinutes(rule.TargetResolutionMinutes);
        }

        // Calculate business hours (simplified implementation)
        return CalculateBusinessHoursDue(startTime, rule.TargetResolutionMinutes, config);
    }

    public async Task<bool> UpdateSlaStatusAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        if (!ticket.SlaDueAt.HasValue || !ticket.SlaRuleId.HasValue)
            return false;

        var now = DateTime.UtcNow;
        var oldStatus = ticket.SlaStatus;

        // Check if already completed
        if (ticket.Status == TicketStatus.CLOSED || ticket.Status == TicketStatus.RESOLVED)
        {
            ticket.SlaStatus = SlaStatus.COMPLETED;
            return oldStatus != ticket.SlaStatus;
        }

        // Check if breached
        if (now >= ticket.SlaDueAt.Value)
        {
            if (ticket.SlaStatus != SlaStatus.BREACHED)
            {
                ticket.SlaStatus = SlaStatus.BREACHED;
                ticket.SlaBreachedAt = now;
                ticket.AddDomainEvent(new SlaBreachedEvent(ticket));
            }
            return oldStatus != ticket.SlaStatus;
        }

        // Check if approaching breach
        var rule = await _db.SlaRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == ticket.SlaRuleId, cancellationToken);

        if (rule != null)
        {
            var totalDuration = TimeSpan.FromMinutes(rule.TargetResolutionMinutes);
            var elapsed = now - ticket.CreationTime;
            var percentElapsed = elapsed.TotalMinutes / totalDuration.TotalMinutes;

            if (percentElapsed >= ApproachingBreachThreshold)
            {
                if (ticket.SlaStatus != SlaStatus.APPROACHING_BREACH)
                {
                    ticket.SlaStatus = SlaStatus.APPROACHING_BREACH;
                    ticket.AddDomainEvent(new SlaApproachingBreachEvent(ticket));
                }
                return oldStatus != ticket.SlaStatus;
            }
        }

        ticket.SlaStatus = SlaStatus.ON_TRACK;
        return oldStatus != ticket.SlaStatus;
    }

    private bool MatchesConditions(Ticket ticket, SlaRule rule)
    {
        if (string.IsNullOrEmpty(rule.ConditionsJson))
            return true; // No conditions = matches all

        var conditions = rule.Conditions;
        if (!conditions.Any())
            return true;

        foreach (var condition in conditions)
        {
            var key = condition.Key.ToLower();
            var valueStr = condition.Value?.ToString();

            if (string.IsNullOrEmpty(valueStr))
                continue;

            switch (key)
            {
                case "priority":
                    if (!string.Equals(ticket.Priority, valueStr, StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;

                case "category":
                    if (!string.Equals(ticket.Category, valueStr, StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;

                case "owning_team_id":
                case "owningteamid":
                    if (Guid.TryParse(valueStr, out var teamId))
                    {
                        if (ticket.OwningTeamId != teamId)
                            return false;
                    }
                    break;

                case "status":
                    if (!string.Equals(ticket.Status, valueStr, StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;
            }
        }

        return true;
    }

    private DateTime CalculateBusinessHoursDue(DateTime startTime, int targetMinutes, BusinessHoursConfig config)
    {
        // Parse start/end times
        if (!TimeSpan.TryParse(config.StartTime, out var businessStart))
            businessStart = TimeSpan.FromHours(8);
        if (!TimeSpan.TryParse(config.EndTime, out var businessEnd))
            businessEnd = TimeSpan.FromHours(18);

        var businessDayMinutes = (businessEnd - businessStart).TotalMinutes;
        var current = startTime;
        var remainingMinutes = (double)targetMinutes;

        // Limit iterations to prevent infinite loops
        for (int i = 0; i < 365 && remainingMinutes > 0; i++)
        {
            // Skip non-working days
            if (!config.Workdays.Contains((int)current.DayOfWeek))
            {
                current = current.Date.AddDays(1).Add(businessStart);
                continue;
            }

            // Skip holidays
            if (config.Holidays?.Any(h => h.Date == current.Date) == true)
            {
                current = current.Date.AddDays(1).Add(businessStart);
                continue;
            }

            var currentTime = current.TimeOfDay;

            // Before business hours
            if (currentTime < businessStart)
            {
                current = current.Date.Add(businessStart);
                currentTime = businessStart;
            }

            // After business hours
            if (currentTime >= businessEnd)
            {
                current = current.Date.AddDays(1).Add(businessStart);
                continue;
            }

            // Calculate remaining time in current business day
            var remainingInDay = (businessEnd - currentTime).TotalMinutes;

            if (remainingMinutes <= remainingInDay)
            {
                return current.AddMinutes(remainingMinutes);
            }

            remainingMinutes -= remainingInDay;
            current = current.Date.AddDays(1).Add(businessStart);
        }

        // Fallback
        return startTime.AddMinutes(targetMinutes);
    }
}

