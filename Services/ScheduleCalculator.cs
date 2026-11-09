using QuickTheme.Models;

namespace QuickTheme.Services;

internal static class ScheduleCalculator
{
    public static bool IsActiveDay(ScheduleDays days, DateTime date) => days switch
    {
        ScheduleDays.Weekdays => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
        ScheduleDays.Weekends => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
        _ => true,
    };

    /// <summary>
    /// Returns the theme that should be active at <paramref name="now"/>, or null when
    /// the settings contain no usable transitions.
    /// </summary>
    public static bool? GetCurrentDark(AppSettings settings, DateTime now)
    {
        ScheduleTransition? latest = null;

        foreach (ScheduleTransition transition in Enumerate(settings, now.Date.AddDays(-8), now.Date))
        {
            if (transition.At <= now && (latest is null || transition.At > latest.Value.At))
            {
                latest = transition;
            }
        }

        return latest?.Dark;
    }

    public static ScheduleTransition? GetNextTransition(AppSettings settings, DateTime now)
    {
        ScheduleTransition? next = null;

        foreach (ScheduleTransition transition in Enumerate(settings, now.Date, now.Date.AddDays(8)))
        {
            if (transition.At > now && (next is null || transition.At < next.Value.At))
            {
                next = transition;
            }
        }

        return next;
    }

    private static IEnumerable<ScheduleTransition> Enumerate(
        AppSettings settings,
        DateTime startDate,
        DateTime endDate)
    {
        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (!IsActiveDay(settings.ScheduleDays, date))
            {
                continue;
            }

            (DateTime light, DateTime dark) = GetTransitionTimes(settings, date);
            if (light == dark)
            {
                continue;
            }

            yield return new ScheduleTransition(light, Dark: false);
            yield return new ScheduleTransition(dark, Dark: true);
        }
    }

    private static (DateTime Light, DateTime Dark) GetTransitionTimes(AppSettings settings, DateTime date)
    {
        if (settings.ScheduleMode == ScheduleMode.SunriseSunset &&
            TryGetSolarTimes(settings, date, out DateTime sunrise, out DateTime sunset))
        {
            return (sunrise, sunset);
        }

        return (
            date + settings.ScheduleLightTime.ToTimeSpan(),
            date + settings.ScheduleDarkTime.ToTimeSpan());
    }

    private static bool TryGetSolarTimes(
        AppSettings settings,
        DateTime date,
        out DateTime sunrise,
        out DateTime sunset)
    {
        sunrise = default;
        sunset = default;

        try
        {
            DateTime localNoon = date.Date.AddHours(12);
            DateTime utcNoon = TimeZoneInfo.ConvertTimeToUtc(localNoon, TimeZoneInfo.Local);
            SunTimes times = SolarCalculator.GetSunTimesUtc(
                utcNoon,
                settings.ScheduleLatitude,
                settings.ScheduleLongitude);

            if (times.SunriseUtc is null || times.SunsetUtc is null)
            {
                return false;
            }

            sunrise = TimeZoneInfo.ConvertTimeFromUtc(times.SunriseUtc.Value, TimeZoneInfo.Local);
            sunset = TimeZoneInfo.ConvertTimeFromUtc(times.SunsetUtc.Value, TimeZoneInfo.Local);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
