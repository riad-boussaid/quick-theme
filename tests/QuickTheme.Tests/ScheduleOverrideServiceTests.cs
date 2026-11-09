using QuickTheme.Models;
using QuickTheme.Services;

namespace QuickTheme.Tests;

public sealed class ScheduleOverrideServiceTests
{
    private static readonly DateTime MondayNoon = new(2026, 11, 9, 12, 0, 0);

    private static AppSettings CreateSettings(
        ScheduleDays days = ScheduleDays.EveryDay,
        TimeOnly? light = null,
        TimeOnly? dark = null) => new()
    {
        ScheduleLightTime = light ?? new TimeOnly(7, 0),
        ScheduleDarkTime = dark ?? new TimeOnly(19, 0),
        ScheduleDays = days,
    };

    [Theory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void TryGetEnd_ForcedTheme_EndsInOneHour(string kindName)
    {
        ScheduleOverrideKind kind = Enum.Parse<ScheduleOverrideKind>(kindName);
        bool result = ScheduleOverrideService.TryGetEnd(
            CreateSettings(),
            kind,
            MondayNoon,
            out DateTimeOffset until);

        Assert.True(result);
        Assert.Equal(MondayNoon.AddHours(1), until.LocalDateTime);
    }

    [Fact]
    public void TryGetEnd_UntilNextSwitch_ReturnsNextTransition()
    {
        bool result = ScheduleOverrideService.TryGetEnd(
            CreateSettings(),
            ScheduleOverrideKind.UntilNextSwitch,
            MondayNoon,
            out DateTimeOffset until);

        Assert.True(result);
        Assert.Equal(new DateTime(2026, 11, 9, 19, 0, 0), until.LocalDateTime);
    }

    [Fact]
    public void TryGetEnd_PauseUntilTomorrow_SkipsTonight()
    {
        bool result = ScheduleOverrideService.TryGetEnd(
            CreateSettings(),
            ScheduleOverrideKind.PauseUntilTomorrow,
            MondayNoon,
            out DateTimeOffset until);

        Assert.True(result);
        Assert.Equal(new DateTime(2026, 11, 10, 7, 0, 0), until.LocalDateTime);
    }

    [Fact]
    public void TryGetEnd_PauseUntilTomorrow_UsesNextActiveDay()
    {
        var fridayNoon = new DateTime(2026, 11, 13, 12, 0, 0);

        bool result = ScheduleOverrideService.TryGetEnd(
            CreateSettings(ScheduleDays.Weekdays),
            ScheduleOverrideKind.PauseUntilTomorrow,
            fridayNoon,
            out DateTimeOffset until);

        Assert.True(result);
        Assert.Equal(new DateTime(2026, 11, 16, 7, 0, 0), until.LocalDateTime);
    }

    [Theory]
    [InlineData("UntilNextSwitch")]
    [InlineData("PauseUntilTomorrow")]
    public void TryGetEnd_WithoutTransitions_ReturnsFalse(string kindName)
    {
        ScheduleOverrideKind kind = Enum.Parse<ScheduleOverrideKind>(kindName);
        AppSettings settings = CreateSettings(light: new TimeOnly(12, 0), dark: new TimeOnly(12, 0));

        Assert.False(ScheduleOverrideService.TryGetEnd(settings, kind, MondayNoon, out _));
    }

    [Fact]
    public void IsActive_ChecksExpiration()
    {
        AppSettings settings = CreateSettings();
        settings.ScheduleOverrideKind = ScheduleOverrideKind.Dark;
        settings.ScheduleOverrideUntil = new DateTimeOffset(MondayNoon).AddMinutes(30);

        Assert.True(ScheduleOverrideService.IsActive(settings, new DateTimeOffset(MondayNoon)));
        Assert.False(ScheduleOverrideService.IsActive(settings, new DateTimeOffset(MondayNoon).AddHours(1)));
    }

    [Fact]
    public void IsActive_ReturnsFalseForNone()
    {
        Assert.False(ScheduleOverrideService.IsActive(CreateSettings(), new DateTimeOffset(MondayNoon)));
    }

    [Fact]
    public void ClearIfExpired_ResetsState()
    {
        AppSettings settings = CreateSettings();
        settings.ScheduleOverrideKind = ScheduleOverrideKind.Light;
        settings.ScheduleOverrideUntil = new DateTimeOffset(MondayNoon).AddMinutes(30);

        bool cleared = ScheduleOverrideService.ClearIfExpired(
            settings,
            new DateTimeOffset(MondayNoon).AddHours(1));

        Assert.True(cleared);
        Assert.Equal(ScheduleOverrideKind.None, settings.ScheduleOverrideKind);
        Assert.Null(settings.ScheduleOverrideUntil);
    }

    [Fact]
    public void ClearIfExpired_KeepsActiveOverride()
    {
        AppSettings settings = CreateSettings();
        settings.ScheduleOverrideKind = ScheduleOverrideKind.Dark;
        settings.ScheduleOverrideUntil = new DateTimeOffset(MondayNoon).AddHours(1);

        Assert.False(ScheduleOverrideService.ClearIfExpired(settings, new DateTimeOffset(MondayNoon)));
        Assert.Equal(ScheduleOverrideKind.Dark, settings.ScheduleOverrideKind);
    }

    [Fact]
    public void ClearIfExpired_TreatsMissingEndAsExpired()
    {
        AppSettings settings = CreateSettings();
        settings.ScheduleOverrideKind = ScheduleOverrideKind.UntilNextSwitch;

        Assert.True(ScheduleOverrideService.ClearIfExpired(settings, new DateTimeOffset(MondayNoon)));
        Assert.Equal(ScheduleOverrideKind.None, settings.ScheduleOverrideKind);
    }

    [Theory]
    [InlineData("Dark", true)]
    [InlineData("Light", false)]
    [InlineData("UntilNextSwitch", null)]
    [InlineData("PauseUntilTomorrow", null)]
    [InlineData("None", null)]
    public void ForcedDark_MapsOverrideKinds(string kindName, bool? expected)
    {
        ScheduleOverrideKind kind = Enum.Parse<ScheduleOverrideKind>(kindName);

        Assert.Equal(expected, ScheduleOverrideService.ForcedDark(kind));
    }

    [Fact]
    public void Describe_ShowsActiveOverride()
    {
        AppSettings settings = CreateSettings();
        settings.ScheduleOverrideKind = ScheduleOverrideKind.Dark;
        settings.ScheduleOverrideUntil = new DateTimeOffset(MondayNoon).AddHours(1);

        string? text = ScheduleOverrideService.Describe(settings, new DateTimeOffset(MondayNoon));

        Assert.NotNull(text);
        Assert.StartsWith("Dark override until ", text);
        Assert.Contains("13:00", text);
    }

    [Fact]
    public void Describe_ReturnsNullWhenExpired()
    {
        AppSettings settings = CreateSettings();
        settings.ScheduleOverrideKind = ScheduleOverrideKind.Dark;
        settings.ScheduleOverrideUntil = new DateTimeOffset(MondayNoon).AddMinutes(30);

        Assert.Null(ScheduleOverrideService.Describe(settings, new DateTimeOffset(MondayNoon).AddHours(1)));
    }
}
