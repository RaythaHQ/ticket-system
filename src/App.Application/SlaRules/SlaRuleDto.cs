using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using System.Text.Json;

namespace App.Application.SlaRules;

/// <summary>
/// SLA rule data transfer object.
/// </summary>
public record SlaRuleDto : BaseAuditableEntityDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Dictionary<string, object> Conditions { get; init; } = new();
    public int TargetResolutionMinutes { get; init; }
    public int? TargetCloseMinutes { get; init; }
    public bool BusinessHoursEnabled { get; init; }
    public BusinessHoursConfig? BusinessHoursConfig { get; init; }
    public bool IsActive { get; init; }
    public int Priority { get; init; }
    public BreachBehavior? BreachBehavior { get; init; }

    /// <summary>
    /// Human-readable resolution time string (e.g., "4 hours", "2 days")
    /// </summary>
    public string ResolutionTimeLabel => FormatDuration(TargetResolutionMinutes);

    public static SlaRuleDto MapFrom(SlaRule rule)
    {
        BusinessHoursConfig? businessHours = null;
        if (!string.IsNullOrEmpty(rule.BusinessHoursConfigJson))
        {
            try { businessHours = JsonSerializer.Deserialize<BusinessHoursConfig>(rule.BusinessHoursConfigJson); }
            catch { }
        }

        BreachBehavior? breachBehavior = null;
        if (!string.IsNullOrEmpty(rule.BreachBehaviorJson))
        {
            try { breachBehavior = JsonSerializer.Deserialize<BreachBehavior>(rule.BreachBehaviorJson); }
            catch { }
        }

        return new SlaRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            Conditions = rule.Conditions,
            TargetResolutionMinutes = rule.TargetResolutionMinutes,
            TargetCloseMinutes = rule.TargetCloseMinutes,
            BusinessHoursEnabled = rule.BusinessHoursEnabled,
            BusinessHoursConfig = businessHours,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            BreachBehavior = breachBehavior,
            CreationTime = rule.CreationTime,
            LastModificationTime = rule.LastModificationTime
        };
    }

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60)
            return $"{minutes} min";
        if (minutes < 1440)
            return $"{minutes / 60} hour{(minutes / 60 > 1 ? "s" : "")}";
        return $"{minutes / 1440} day{(minutes / 1440 > 1 ? "s" : "")}";
    }
}

public record BusinessHoursConfig
{
    public List<int> Workdays { get; init; } = new() { 1, 2, 3, 4, 5 }; // Mon-Fri
    public string StartTime { get; init; } = "08:00";
    public string EndTime { get; init; } = "18:00";
    public string? TimeZone { get; init; }
    public List<DateTime>? Holidays { get; init; }
}

public record BreachBehavior
{
    public bool UiMarkers { get; init; } = true;
    public bool NotifyAssignee { get; init; } = true;
    public bool NotifyTeam { get; init; }
    public string? WebhookUrl { get; init; }
}

