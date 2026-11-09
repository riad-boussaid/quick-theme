using QuickTheme.Models;
using QuickTheme.Services;

namespace QuickTheme.Tests;

public sealed class SolarCalculatorTests
{
    [Fact]
    public void LondonSummerSolstice_HasExpectedSunriseAndSunset()
    {
        var date = new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        SunTimes times = SolarCalculator.GetSunTimesUtc(date, 51.5074, -0.1278);

        Assert.NotNull(times.SunriseUtc);
        Assert.NotNull(times.SunsetUtc);
        AssertWithinMinutes(times.SunriseUtc.Value, new DateTime(2026, 6, 21, 3, 43, 0, DateTimeKind.Utc), 20);
        AssertWithinMinutes(times.SunsetUtc.Value, new DateTime(2026, 6, 21, 20, 21, 0, DateTimeKind.Utc), 20);
    }

    [Fact]
    public void EquatorAtEquinox_HasSunriseNearSixUtc()
    {
        var date = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);

        SunTimes times = SolarCalculator.GetSunTimesUtc(date, 0, 0);

        Assert.NotNull(times.SunriseUtc);
        Assert.NotNull(times.SunsetUtc);
        AssertWithinMinutes(times.SunriseUtc.Value, date.AddHours(6), 20);
        AssertWithinMinutes(times.SunsetUtc.Value, date.AddHours(18), 20);
    }

    [Fact]
    public void PolarSummer_ReturnsNoSunriseOrSunset()
    {
        var date = new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        SunTimes times = SolarCalculator.GetSunTimesUtc(date, 69.6492, 18.9553);

        Assert.Null(times.SunriseUtc);
        Assert.Null(times.SunsetUtc);
    }

    [Fact]
    public void InvalidCoordinates_ReturnNoTimes()
    {
        SunTimes times = SolarCalculator.GetSunTimesUtc(DateTime.UtcNow, 91, 0);

        Assert.Null(times.SunriseUtc);
        Assert.Null(times.SunsetUtc);
    }

    private static void AssertWithinMinutes(DateTime actual, DateTime expected, int minutes)
    {
        TimeSpan difference = (actual - expected).Duration();
        Assert.True(
            difference <= TimeSpan.FromMinutes(minutes),
            $"Expected {actual:O} to be within {minutes} minutes of {expected:O}.");
    }
}
