using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SlaRules.Services;

/// <summary>
/// Implementation of SLA evaluation and status management.
/// </summary>
public class SlaService : ISlaService
{
    private readonly IAppDbContext _db;
    private readonly ITicketConfigService _configService;
    private readonly ICurrentOrganization _currentOrganization;
    private const double ApproachingBreachThreshold = 0.75; // 75% of time elapsed

    public SlaService(
        IAppDbContext db,
        ITicketConfigService configService,
        ICurrentOrganization currentOrganization
    )
    {
        _db = db;
        _configService = configService;
        _currentOrganization = currentOrganization;
    }

    public async Task<SlaRule?> EvaluateAndAssignSlaAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default
    )
    {
        // Get all active SLA rules ordered by priority
        var rules = await _db
            .SlaRules.AsNoTracking()
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
        var startTimeUtc = ticket.CreationTime;

        if (!rule.BusinessHoursEnabled)
        {
            // Simple calculation without business hours
            return startTimeUtc.AddMinutes(rule.TargetResolutionMinutes);
        }

        // Business hours calculation
        BusinessHoursConfig? config = null;
        if (!string.IsNullOrEmpty(rule.BusinessHoursConfigJson))
        {
            try
            {
                config = JsonSerializer.Deserialize<BusinessHoursConfig>(
                    rule.BusinessHoursConfigJson
                );
            }
            catch
            {
                // Intentionally swallowed: malformed JSON should use default behavior
            }
        }

        if (config == null)
        {
            // Fallback to simple calculation
            return startTimeUtc.AddMinutes(rule.TargetResolutionMinutes);
        }

        // Use the organization's timezone for business hours calculation
        var timeZone = _currentOrganization.TimeZone;

        // Calculate business hours with proper timezone handling
        return CalculateBusinessHoursDue(
            startTimeUtc,
            rule.TargetResolutionMinutes,
            config,
            timeZone
        );
    }

    public async Task<bool> UpdateSlaStatusAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default
    )
    {
        if (!ticket.SlaDueAt.HasValue || !ticket.SlaRuleId.HasValue)
            return false;

        var now = DateTime.UtcNow;
        var oldStatus = ticket.SlaStatus;

        // Check if already completed (status type is Closed)
        var isClosedType = await _configService.IsStatusClosedTypeAsync(
            ticket.Status,
            cancellationToken
        );
        if (isClosedType)
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
        var rule = await _db
            .SlaRules.AsNoTracking()
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
                    if (
                        !string.Equals(
                            ticket.Priority,
                            valueStr,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                        return false;
                    break;

                case "category":
                    if (
                        !string.Equals(
                            ticket.Category,
                            valueStr,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
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

    public int CalculateDefaultExtensionHours(DateTime? currentSlaDueAt, string? timezone)
    {
        var effectiveTimezone = string.IsNullOrEmpty(timezone)
            ? DateTimeExtensions.DEFAULT_TIMEZONE
            : timezone;

        var now = DateTime.UtcNow;
        var baseTime = currentSlaDueAt ?? now;

        // Convert to local timezone for business day calculation
        var localNow = now.UtcToTimeZone(effectiveTimezone);

        // Target 4pm next business day
        var targetDate = localNow.Date.AddDays(1);

        // Skip weekends
        while (
            targetDate.DayOfWeek == DayOfWeek.Saturday || targetDate.DayOfWeek == DayOfWeek.Sunday
        )
        {
            targetDate = targetDate.AddDays(1);
        }

        // Target 4pm (16:00) on that day
        var targetTimeLocal = targetDate.AddHours(16);

        // Convert target back to UTC and calculate hours from baseTime
        var targetTimeUtc = targetTimeLocal.TimeZoneToUtc(effectiveTimezone);
        var hoursUntilTarget = (targetTimeUtc - baseTime).TotalHours;

        // Ensure at least 1 hour
        return Math.Max(1, (int)Math.Ceiling(hoursUntilTarget));
    }

    public DateTime CalculateExtendedDueDate(DateTime? currentSlaDueAt, int extensionHours)
    {
        var baseTime = currentSlaDueAt ?? DateTime.UtcNow;
        return baseTime.AddHours(extensionHours);
    }

    private DateTime CalculateBusinessHoursDue(
        DateTime startTimeUtc,
        int targetMinutes,
        BusinessHoursConfig config,
        string timeZone
    )
    {
        // Parse start/end times (these are in local business time)
        if (!TimeSpan.TryParse(config.StartTime, out var businessStart))
            businessStart = TimeSpan.FromHours(8);
        if (!TimeSpan.TryParse(config.EndTime, out var businessEnd))
            businessEnd = TimeSpan.FromHours(18);

        // Convert start time from UTC to local timezone for calculation
        var currentLocal = startTimeUtc.UtcToTimeZone(timeZone);
        var remainingMinutes = (double)targetMinutes;

        // Limit iterations to prevent infinite loops
        for (int i = 0; i < 365 && remainingMinutes > 0; i++)
        {
            // Skip non-working days
            if (!config.Workdays.Contains((int)currentLocal.DayOfWeek))
            {
                currentLocal = currentLocal.Date.AddDays(1).Add(businessStart);
                continue;
            }

            // Skip holidays
            if (config.Holidays?.Any(h => h.Date == currentLocal.Date) == true)
            {
                currentLocal = currentLocal.Date.AddDays(1).Add(businessStart);
                continue;
            }

            var currentTime = currentLocal.TimeOfDay;

            // Before business hours - move to start of business day
            if (currentTime < businessStart)
            {
                currentLocal = currentLocal.Date.Add(businessStart);
                currentTime = businessStart;
            }

            // After business hours - move to start of next business day
            if (currentTime >= businessEnd)
            {
                currentLocal = currentLocal.Date.AddDays(1).Add(businessStart);
                continue;
            }

            // Calculate remaining time in current business day
            var remainingInDay = (businessEnd - currentTime).TotalMinutes;

            if (remainingMinutes <= remainingInDay)
            {
                // Add remaining minutes and convert back to UTC
                var dueLocalTime = currentLocal.AddMinutes(remainingMinutes);
                return dueLocalTime.TimeZoneToUtc(timeZone);
            }

            remainingMinutes -= remainingInDay;
            currentLocal = currentLocal.Date.AddDays(1).Add(businessStart);
        }

        // Fallback - just add minutes to original UTC time
        return startTimeUtc.AddMinutes(targetMinutes);
    }
}
