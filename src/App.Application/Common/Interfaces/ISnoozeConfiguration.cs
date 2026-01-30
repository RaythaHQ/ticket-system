namespace App.Application.Common.Interfaces;

/// <summary>
/// Configuration for ticket snooze feature.
/// </summary>
public interface ISnoozeConfiguration
{
    /// <summary>
    /// Maximum number of days a ticket can be snoozed. Default: 90.
    /// </summary>
    int MaxDurationDays { get; }
}
