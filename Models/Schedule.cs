namespace QuickTheme.Models;

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
