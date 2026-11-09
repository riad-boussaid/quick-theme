namespace QuickTheme.Models;

internal enum ScheduleMode
{
    FixedTimes,
    SunriseSunset,
}

internal enum ScheduleScope
{
    Both,
    Apps,
    System,
}

internal enum ScheduleDays
{
    EveryDay,
    Weekdays,
    Weekends,
}

internal enum ThemePreset
{
    AllDark,
    AllLight,
    AppsFollowSystem,
}

internal readonly record struct ScheduleTransition(DateTime At, bool Dark);

internal readonly record struct SunTimes(DateTime? SunriseUtc, DateTime? SunsetUtc);
