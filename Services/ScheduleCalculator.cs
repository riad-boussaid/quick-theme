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
        if (settings.ScheduleLightTime == settings.ScheduleDarkTime)
        {
            yield break;
        }

        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (!IsActiveDay(settings.ScheduleDays, date))
            {
                continue;
            }

            yield return new ScheduleTransition(date + settings.ScheduleLightTime.ToTimeSpan(), Dark: false);
            yield return new ScheduleTransition(date + settings.ScheduleDarkTime.ToTimeSpan(), Dark: true);
        }
    }
}
