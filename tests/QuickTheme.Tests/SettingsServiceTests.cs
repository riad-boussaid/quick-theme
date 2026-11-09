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
        };

        string json = SettingsService.Serialize(settings);
        AppSettings roundTripped = SettingsService.Deserialize(json);

        Assert.Equal(FlyoutThemeMode.Light, roundTripped.FlyoutTheme);
        Assert.Equal("https://example.com/update.json", roundTripped.UpdateFeedUrl);
        Assert.False(roundTripped.CheckForUpdatesOnStartup);
        Assert.Equal(settings.LastUpdateCheckUtc, roundTripped.LastUpdateCheckUtc);
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
