using QuickTheme.Models;
using QuickTheme.Services;

namespace QuickTheme.Tests;

public sealed class ScheduleCalculatorTests
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

    [Fact]
    public void Noon_IsLight()
    {
        Assert.False(ScheduleCalculator.GetCurrentDark(CreateSettings(), MondayNoon));
    }

    [Fact]
    public void Evening_IsDark()
    {
        Assert.True(ScheduleCalculator.GetCurrentDark(CreateSettings(), MondayNoon.AddHours(8)));
    }

    [Fact]
    public void EarlyMorning_KeepsPreviousEveningTheme()
    {
        Assert.True(ScheduleCalculator.GetCurrentDark(CreateSettings(), MondayNoon.AddHours(-6)));
    }

    [Fact]
    public void NextTransition_FromNoon_IsDarkAt19()
    {
        ScheduleTransition? next = ScheduleCalculator.GetNextTransition(CreateSettings(), MondayNoon);

        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 11, 9, 19, 0, 0), next.Value.At);
        Assert.True(next.Value.Dark);
    }

    [Fact]
    public void NextTransition_FromEvening_IsLightTomorrow()
    {
        ScheduleTransition? next = ScheduleCalculator.GetNextTransition(
            CreateSettings(),
            MondayNoon.AddHours(8));

        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 11, 10, 7, 0, 0), next.Value.At);
        Assert.False(next.Value.Dark);
    }

    [Fact]
    public void Weekdays_SkipWeekendTransitions()
    {
        AppSettings settings = CreateSettings(ScheduleDays.Weekdays);
        var saturdayNoon = new DateTime(2026, 11, 14, 12, 0, 0);

        Assert.True(ScheduleCalculator.GetCurrentDark(settings, saturdayNoon));

        ScheduleTransition? next = ScheduleCalculator.GetNextTransition(settings, saturdayNoon);
        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 11, 16, 7, 0, 0), next.Value.At);
        Assert.False(next.Value.Dark);
    }

    [Fact]
    public void Weekends_UseWeekendTransitions()
    {
        AppSettings settings = CreateSettings(ScheduleDays.Weekends);
        var saturdayNoon = new DateTime(2026, 11, 14, 12, 0, 0);

        Assert.False(ScheduleCalculator.GetCurrentDark(settings, saturdayNoon));

        ScheduleTransition? next = ScheduleCalculator.GetNextTransition(settings, saturdayNoon);
        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 11, 14, 19, 0, 0), next.Value.At);
        Assert.True(next.Value.Dark);
    }

    [Fact]
    public void ReversedTimes_StillResolveInOrder()
    {
        AppSettings settings = CreateSettings(light: new TimeOnly(19, 0), dark: new TimeOnly(7, 0));

        Assert.True(ScheduleCalculator.GetCurrentDark(settings, MondayNoon));

        ScheduleTransition? next = ScheduleCalculator.GetNextTransition(settings, MondayNoon);
        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 11, 9, 19, 0, 0), next.Value.At);
        Assert.False(next.Value.Dark);
    }

    [Fact]
    public void EqualTimes_ProduceNoTransitions()
    {
        AppSettings settings = CreateSettings(light: new TimeOnly(12, 0), dark: new TimeOnly(12, 0));

        Assert.Null(ScheduleCalculator.GetCurrentDark(settings, MondayNoon));
        Assert.Null(ScheduleCalculator.GetNextTransition(settings, MondayNoon));
    }
}
