namespace App.Domain.ValueObjects;

/// <summary>
/// Relative date presets for date filter conditions.
/// </summary>
public class RelativeDatePreset : ValueObject
{
    public const string TODAY = "today";
    public const string YESTERDAY = "yesterday";
    public const string TOMORROW = "tomorrow";
    public const string THIS_WEEK = "this_week";
    public const string LAST_WEEK = "last_week";
    public const string NEXT_WEEK = "next_week";
    public const string THIS_MONTH = "this_month";
    public const string LAST_MONTH = "last_month";
    public const string NEXT_MONTH = "next_month";
    public const string DAYS_AGO = "days_ago";
    public const string DAYS_FROM_NOW = "days_from_now";
    public const string EXACT_DATE = "exact_date";

    static RelativeDatePreset() { }

    public RelativeDatePreset() { }

    private RelativeDatePreset(string label, string developerName, bool requiresCustomValue = false)
    {
        Label = label;
        DeveloperName = developerName;
        RequiresCustomValue = requiresCustomValue;
    }

    public static RelativeDatePreset From(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
        {
            return Today;
        }

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());
        return type ?? throw new RelativeDatePresetNotFoundException(developerName);
    }

    public static RelativeDatePreset? TryFrom(string developerName)
    {
        try { return From(developerName); }
        catch { return null; }
    }

    public static RelativeDatePreset Today => new("today", TODAY);
    public static RelativeDatePreset Yesterday => new("yesterday", YESTERDAY);
    public static RelativeDatePreset Tomorrow => new("tomorrow", TOMORROW);
    public static RelativeDatePreset ThisWeek => new("this week", THIS_WEEK);
    public static RelativeDatePreset LastWeek => new("last week", LAST_WEEK);
    public static RelativeDatePreset NextWeek => new("next week", NEXT_WEEK);
    public static RelativeDatePreset ThisMonth => new("this month", THIS_MONTH);
    public static RelativeDatePreset LastMonth => new("last month", LAST_MONTH);
    public static RelativeDatePreset NextMonth => new("next month", NEXT_MONTH);
    public static RelativeDatePreset DaysAgo => new("number of days ago...", DAYS_AGO, true);
    public static RelativeDatePreset DaysFromNow => new("number of days from now...", DAYS_FROM_NOW, true);
    public static RelativeDatePreset ExactDate => new("exact date...", EXACT_DATE, true);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public bool RequiresCustomValue { get; set; }

    public static implicit operator string(RelativeDatePreset preset) => preset.DeveloperName;

    public static explicit operator RelativeDatePreset(string type) => From(type);

    public override string ToString() => Label;

    public static IEnumerable<RelativeDatePreset> SupportedTypes
    {
        get
        {
            yield return Today;
            yield return Yesterday;
            yield return Tomorrow;
            yield return ThisWeek;
            yield return LastWeek;
            yield return NextWeek;
            yield return ThisMonth;
            yield return LastMonth;
            yield return NextMonth;
            yield return DaysAgo;
            yield return DaysFromNow;
            yield return ExactDate;
        }
    }

    /// <summary>
    /// Resolve a relative date preset to an actual DateTime range.
    /// </summary>
    /// <param name="customValue">Custom value for "days_ago" or "days_from_now".</param>
    /// <param name="timezone">The timezone to use for date calculations.</param>
    /// <returns>A tuple of (startDate, endDate) for range presets.</returns>
    public (DateTime Start, DateTime End) Resolve(int? customValue, TimeZoneInfo timezone)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var today = now.Date;

        return DeveloperName switch
        {
            TODAY => (today, today.AddDays(1).AddTicks(-1)),
            YESTERDAY => (today.AddDays(-1), today.AddTicks(-1)),
            TOMORROW => (today.AddDays(1), today.AddDays(2).AddTicks(-1)),
            THIS_WEEK => GetWeekRange(today, 0),
            LAST_WEEK => GetWeekRange(today, -1),
            NEXT_WEEK => GetWeekRange(today, 1),
            THIS_MONTH => GetMonthRange(today, 0),
            LAST_MONTH => GetMonthRange(today, -1),
            NEXT_MONTH => GetMonthRange(today, 1),
            DAYS_AGO => (today.AddDays(-(customValue ?? 0)), today.AddDays(-(customValue ?? 0) + 1).AddTicks(-1)),
            DAYS_FROM_NOW => (today.AddDays(customValue ?? 0), today.AddDays((customValue ?? 0) + 1).AddTicks(-1)),
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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public class RelativeDatePresetNotFoundException : Exception
{
    public RelativeDatePresetNotFoundException(string developerName)
        : base($"Relative date preset '{developerName}' is not supported.")
    {
    }
}

