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
}
