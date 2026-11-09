namespace QuickTheme.Models;

internal enum FlyoutThemeMode
{
    System,
    Dark,
    Light,
}

internal sealed class AppSettings
{
    public FlyoutThemeMode FlyoutTheme { get; set; } = FlyoutThemeMode.System;
    public string UpdateFeedUrl { get; set; } = string.Empty;
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }

    public bool ScheduleEnabled { get; set; }
    public bool SchedulePaused { get; set; }
    public ScheduleMode ScheduleMode { get; set; } = ScheduleMode.FixedTimes;
    public TimeOnly ScheduleLightTime { get; set; } = new(7, 0);
    public TimeOnly ScheduleDarkTime { get; set; } = new(19, 0);
    public double ScheduleLatitude { get; set; }
    public double ScheduleLongitude { get; set; }
    public ScheduleScope ScheduleScope { get; set; } = ScheduleScope.Both;
    public ScheduleDays ScheduleDays { get; set; } = ScheduleDays.EveryDay;

    public string HotkeyToggleDark { get; set; } = "Ctrl+Alt+D";
    public string HotkeyOpenPanel { get; set; } = "Ctrl+Alt+Q";
}
