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
            ScheduleLightTime = new TimeOnly(6, 30),
            ScheduleDarkTime = new TimeOnly(20, 15),
            ScheduleScope = ScheduleScope.Apps,
            ScheduleDays = ScheduleDays.Weekdays,
        };

        string json = SettingsService.Serialize(settings);
        AppSettings roundTripped = SettingsService.Deserialize(json);

        Assert.Equal(FlyoutThemeMode.Light, roundTripped.FlyoutTheme);
        Assert.Equal("https://example.com/update.json", roundTripped.UpdateFeedUrl);
        Assert.False(roundTripped.CheckForUpdatesOnStartup);
        Assert.Equal(settings.LastUpdateCheckUtc, roundTripped.LastUpdateCheckUtc);
        Assert.True(roundTripped.ScheduleEnabled);
        Assert.True(roundTripped.SchedulePaused);
        Assert.Equal(new TimeOnly(6, 30), roundTripped.ScheduleLightTime);
        Assert.Equal(new TimeOnly(20, 15), roundTripped.ScheduleDarkTime);
        Assert.Equal(ScheduleScope.Apps, roundTripped.ScheduleScope);
        Assert.Equal(ScheduleDays.Weekdays, roundTripped.ScheduleDays);
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
