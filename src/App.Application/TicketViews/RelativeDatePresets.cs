using App.Domain.ValueObjects;

namespace App.Application.TicketViews;

/// <summary>
/// Relative date preset DTOs for UI display, backed by domain RelativeDatePreset ValueObjects.
/// </summary>
public static class RelativeDatePresets
{
    public static readonly IReadOnlyList<RelativeDatePresetDto> Presets =
        RelativeDatePreset.SupportedTypes.Select(p => new RelativeDatePresetDto
        {
            Value = p.DeveloperName,
            Label = p.Label,
            RequiresCustomValue = p.RequiresCustomValue
        }).ToList();

    /// <summary>
    /// Resolve a relative date preset to an actual DateTime range.
    /// Delegates to the domain ValueObject for resolution logic.
    /// </summary>
    public static (DateTime Start, DateTime End) Resolve(string preset, int? customValue, TimeZoneInfo timezone)
    {
        var presetObj = RelativeDatePreset.TryFrom(preset);
        if (presetObj == null)
        {
            // Default to today
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
            return (now.Date, now.Date.AddDays(1).AddTicks(-1));
        }

        return presetObj.Resolve(customValue, timezone);
    }
}

/// <summary>
/// DTO for relative date presets to use in UI.
/// </summary>
public record RelativeDatePresetDto
{
    public string Value { get; init; } = null!;
    public string Label { get; init; } = null!;
    public bool RequiresCustomValue { get; init; }
}
