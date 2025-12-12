using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace App.Domain.Entities;

/// <summary>
/// Configuration defining service level expectations.
/// Rules are evaluated in priority order; first match wins.
/// </summary>
public class SlaRule : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// JSON object containing matching criteria: { "priority": "urgent", "category": "...", "owning_team_id": "..." }
    /// </summary>
    public string? ConditionsJson { get; set; }

    /// <summary>
    /// Target resolution time in minutes.
    /// </summary>
    public int TargetResolutionMinutes { get; set; }

    /// <summary>
    /// Optional target close time in minutes.
    /// </summary>
    public int? TargetCloseMinutes { get; set; }

    /// <summary>
    /// When true, SLA calculations only count business hours.
    /// </summary>
    public bool BusinessHoursEnabled { get; set; }

    /// <summary>
    /// JSON object containing business hours config: { "workdays": [...], "start_time": "08:00", "end_time": "18:00" }
    /// </summary>
    public string? BusinessHoursConfigJson { get; set; }

    /// <summary>
    /// When false, this SLA rule is not evaluated.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Order for rule evaluation. Lower numbers are evaluated first.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// JSON object containing breach behavior: { "ui_markers": true, "notify_assignee": true, "webhook_url": "..." }
    /// </summary>
    public string? BreachBehaviorJson { get; set; }

    [NotMapped]
    public Dictionary<string, object> Conditions
    {
        get => string.IsNullOrEmpty(ConditionsJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(ConditionsJson) ?? new Dictionary<string, object>();
        set => ConditionsJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public TimeSpan TargetResolutionTime => TimeSpan.FromMinutes(TargetResolutionMinutes);

    [NotMapped]
    public TimeSpan? TargetCloseTime => TargetCloseMinutes.HasValue
        ? TimeSpan.FromMinutes(TargetCloseMinutes.Value)
        : null;
}

