namespace App.Application.TicketViews;

/// <summary>
/// Relative date presets for date filters.
/// </summary>
public static class RelativeDatePresets
{
    public static readonly IReadOnlyList<RelativeDatePreset> Presets = new List<RelativeDatePreset>
    {
        new("today", "today"),
        new("yesterday", "yesterday"),
        new("tomorrow", "tomorrow"),
        new("this_week", "this week"),
        new("last_week", "last week"),
        new("next_week", "next week"),
        new("this_month", "this month"),
        new("last_month", "last month"),
        new("next_month", "next month"),
        new("days_ago", "number of days ago..."),
        new("days_from_now", "number of days from now..."),
        new("exact_date", "exact date..."),
    };

    /// <summary>
    /// Resolve a relative date preset to an actual DateTime.
    /// </summary>
    /// <param name="preset">The preset value (e.g., "today", "this_week").</param>
    /// <param name="customValue">Custom value for "days_ago" or "days_from_now".</param>
    /// <param name="timezone">The timezone to use for date calculations.</param>
    /// <returns>A tuple of (startDate, endDate) for range presets, or (date, date) for exact presets.</returns>
    public static (DateTime Start, DateTime End) Resolve(string preset, int? customValue, TimeZoneInfo timezone)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var today = now.Date;

        return preset switch
        {
            "today" => (today, today.AddDays(1).AddTicks(-1)),
            "yesterday" => (today.AddDays(-1), today.AddTicks(-1)),
            "tomorrow" => (today.AddDays(1), today.AddDays(2).AddTicks(-1)),
            "this_week" => GetWeekRange(today, 0),
            "last_week" => GetWeekRange(today, -1),
            "next_week" => GetWeekRange(today, 1),
            "this_month" => GetMonthRange(today, 0),
            "last_month" => GetMonthRange(today, -1),
            "next_month" => GetMonthRange(today, 1),
            "days_ago" => (today.AddDays(-(customValue ?? 0)), today.AddDays(-(customValue ?? 0) + 1).AddTicks(-1)),
            "days_from_now" => (today.AddDays(customValue ?? 0), today.AddDays((customValue ?? 0) + 1).AddTicks(-1)),
            _ => (today, today.AddDays(1).AddTicks(-1))
        };
    }

    private static (DateTime Start, DateTime End) GetWeekRange(DateTime date, int weekOffset)
    {
        var daysToSubtract = (int)date.DayOfWeek;
        var weekStart = date.AddDays(-daysToSubtract + (weekOffset * 7));
        var weekEnd = weekStart.AddDays(7).AddTicks(-1);
        return (weekStart, weekEnd);
    }

    private static (DateTime Start, DateTime End) GetMonthRange(DateTime date, int monthOffset)
    {
        var monthStart = new DateTime(date.Year, date.Month, 1).AddMonths(monthOffset);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        return (monthStart, monthEnd);
    }
}

public record RelativeDatePreset(string Value, string Label);

