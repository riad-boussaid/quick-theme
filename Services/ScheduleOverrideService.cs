using System.Globalization;
using QuickTheme.Models;

namespace QuickTheme.Services;

internal static class ScheduleOverrideService
{
    public static DateTime GetTomorrowThreshold(DateTime now) =>
        now.Date.AddDays(1).AddTicks(-1);

    public static ScheduleTransition? GetTomorrowTransition(AppSettings settings, DateTime now) =>
        ScheduleCalculator.GetNextTransition(settings, GetTomorrowThreshold(now));

    public static bool TryGetEnd(
        AppSettings settings,
        ScheduleOverrideKind kind,
        DateTime now,
        out DateTimeOffset until)
    {
        until = default;

        DateTime? end = kind switch
        {
            ScheduleOverrideKind.Dark or ScheduleOverrideKind.Light => now.AddHours(1),
            ScheduleOverrideKind.UntilNextSwitch => ScheduleCalculator.GetNextTransition(settings, now)?.At,
            ScheduleOverrideKind.PauseUntilTomorrow => GetTomorrowTransition(settings, now)?.At,
            _ => null,
        };

        if (end is null)
        {
            return false;
        }

        until = new DateTimeOffset(end.Value);
        return true;
    }

    public static bool IsActive(AppSettings settings, DateTimeOffset now) =>
        settings.ScheduleOverrideKind != ScheduleOverrideKind.None &&
        settings.ScheduleOverrideUntil is DateTimeOffset until &&
        until > now;

    public static bool ClearIfExpired(AppSettings settings, DateTimeOffset now)
    {
        if (settings.ScheduleOverrideKind == ScheduleOverrideKind.None)
        {
            return false;
        }

        if (settings.ScheduleOverrideUntil is DateTimeOffset until && until > now)
        {
            return false;
        }

        settings.ScheduleOverrideKind = ScheduleOverrideKind.None;
        settings.ScheduleOverrideUntil = null;
        return true;
    }

    public static bool? ForcedDark(ScheduleOverrideKind kind) => kind switch
    {
        ScheduleOverrideKind.Dark => true,
        ScheduleOverrideKind.Light => false,
        _ => null,
    };

    public static string? Describe(AppSettings settings, DateTimeOffset now)
    {
        if (!IsActive(settings, now))
        {
            return null;
        }

        DateTime localUntil = settings.ScheduleOverrideUntil!.Value.ToLocalTime().DateTime;
        string time = localUntil.ToString("ddd HH:mm", CultureInfo.CurrentCulture);

        return settings.ScheduleOverrideKind switch
        {
            ScheduleOverrideKind.Dark => $"Dark override until {time}",
            ScheduleOverrideKind.Light => $"Light override until {time}",
            ScheduleOverrideKind.UntilNextSwitch => $"Current theme until {time}",
            ScheduleOverrideKind.PauseUntilTomorrow => $"Schedule paused until {time}",
            _ => null,
        };
    }
}
