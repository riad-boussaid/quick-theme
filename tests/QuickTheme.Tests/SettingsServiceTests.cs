using QuickTheme.Models;
using QuickTheme.Services;

namespace QuickTheme.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsSettings()
    {
        var settings = new AppSettings
        {
            FlyoutTheme = FlyoutThemeMode.Light,
            UpdateFeedUrl = "https://example.com/update.json",
            CheckForUpdatesOnStartup = false,
            LastUpdateCheckUtc = new DateTimeOffset(2026, 11, 9, 13, 0, 0, TimeSpan.Zero),
            ScheduleEnabled = true,
            SchedulePaused = true,
            ScheduleMode = ScheduleMode.SunriseSunset,
            ScheduleLightTime = new TimeOnly(6, 30),
            ScheduleDarkTime = new TimeOnly(20, 15),
            ScheduleLatitude = 51.5074,
            ScheduleLongitude = -0.1278,
            ScheduleScope = ScheduleScope.Apps,
            ScheduleDays = ScheduleDays.Weekdays,
            HotkeyToggleDark = "Ctrl+Shift+D",
            HotkeyOpenPanel = "Ctrl+Shift+Q",
        };

        string json = SettingsService.Serialize(settings);
        AppSettings roundTripped = SettingsService.Deserialize(json);

        Assert.Equal(FlyoutThemeMode.Light, roundTripped.FlyoutTheme);
        Assert.Equal("https://example.com/update.json", roundTripped.UpdateFeedUrl);
        Assert.False(roundTripped.CheckForUpdatesOnStartup);
        Assert.Equal(settings.LastUpdateCheckUtc, roundTripped.LastUpdateCheckUtc);
        Assert.True(roundTripped.ScheduleEnabled);
        Assert.True(roundTripped.SchedulePaused);
        Assert.Equal(ScheduleMode.SunriseSunset, roundTripped.ScheduleMode);
        Assert.Equal(new TimeOnly(6, 30), roundTripped.ScheduleLightTime);
        Assert.Equal(new TimeOnly(20, 15), roundTripped.ScheduleDarkTime);
        Assert.Equal(51.5074, roundTripped.ScheduleLatitude);
        Assert.Equal(-0.1278, roundTripped.ScheduleLongitude);
        Assert.Equal(ScheduleScope.Apps, roundTripped.ScheduleScope);
        Assert.Equal(ScheduleDays.Weekdays, roundTripped.ScheduleDays);
        Assert.Equal("Ctrl+Shift+D", roundTripped.HotkeyToggleDark);
        Assert.Equal("Ctrl+Shift+Q", roundTripped.HotkeyOpenPanel);
    }

    [Fact]
    public void Deserialize_UsesDefaultsForMissingValues()
    {
        AppSettings settings = SettingsService.Deserialize("{}");

        Assert.Equal(FlyoutThemeMode.System, settings.FlyoutTheme);
        Assert.Equal(string.Empty, settings.UpdateFeedUrl);
        Assert.True(settings.CheckForUpdatesOnStartup);
        Assert.Null(settings.LastUpdateCheckUtc);
    }

    [Fact]
    public void Serialize_WritesThemeAsAString()
    {
        string json = SettingsService.Serialize(new AppSettings { FlyoutTheme = FlyoutThemeMode.Dark });

        Assert.Contains("\"Dark\"", json);
    }
}
