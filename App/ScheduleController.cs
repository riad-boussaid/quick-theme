using QuickTheme.Models;
using QuickTheme.Services;

namespace QuickTheme.App;

internal sealed class ScheduleController : IDisposable
{
    private readonly ThemeController _themes;
    private readonly System.Windows.Forms.Timer _timer;
    private bool? _lastScheduledDark;

    public ScheduleController(ThemeController themes)
    {
        _themes = themes;
        _timer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _timer.Tick += (_, _) => CheckSchedule();
    }

    public event Action? StateChanged;

    public void Start()
    {
        RefreshSchedule();
        _timer.Start();
    }

    public bool SetOverride(ScheduleOverrideKind kind)
    {
        AppSettings settings = SettingsService.Current;
        if (!settings.ScheduleEnabled || kind == ScheduleOverrideKind.None)
        {
            return false;
        }

        if (!ScheduleOverrideService.TryGetEnd(settings, kind, DateTime.Now, out DateTimeOffset until))
        {
            return false;
        }

        settings.ScheduleOverrideKind = kind;
        settings.ScheduleOverrideUntil = until;
        _lastScheduledDark = null;
        SettingsService.Save();

        if (ScheduleOverrideService.ForcedDark(kind) is bool dark)
        {
            _themes.ApplyScheduledTheme(dark);
        }

        StateChanged?.Invoke();
        return true;
    }

    public bool ClearOverride()
    {
        AppSettings settings = SettingsService.Current;
        if (!ClearOverrideState(settings))
        {
            return false;
        }

        SettingsService.Save();
        _lastScheduledDark = null;
        RefreshSchedule();
        StateChanged?.Invoke();
        return true;
    }

    public void ResumeSchedule()
    {
        AppSettings settings = SettingsService.Current;
        ClearOverrideState(settings);
        settings.SchedulePaused = false;
        SettingsService.Save();
        _lastScheduledDark = null;
        RefreshSchedule();
        StateChanged?.Invoke();
    }

    public void RefreshSchedule()
    {
        AppSettings settings = SettingsService.Current;

        if (!settings.ScheduleEnabled)
        {
            if (ClearOverrideState(settings))
            {
                SettingsService.Save();
                StateChanged?.Invoke();
            }

            return;
        }

        if (ScheduleOverrideService.ClearIfExpired(settings, DateTimeOffset.Now))
        {
            SettingsService.Save();
            StateChanged?.Invoke();
        }

        if (ScheduleOverrideService.IsActive(settings, DateTimeOffset.Now))
        {
            if (ScheduleOverrideService.ForcedDark(settings.ScheduleOverrideKind) is bool dark)
            {
                _themes.ApplyScheduledTheme(dark);
            }

            _lastScheduledDark = null;
            return;
        }

        if (settings.SchedulePaused)
        {
            return;
        }

        if (ScheduleCalculator.GetCurrentDark(settings, DateTime.Now) is bool current)
        {
            _themes.ApplyScheduledTheme(current);
            _lastScheduledDark = current;
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private static bool ClearOverrideState(AppSettings settings)
    {
        if (settings.ScheduleOverrideKind == ScheduleOverrideKind.None &&
            settings.ScheduleOverrideUntil is null)
        {
            return false;
        }

        settings.ScheduleOverrideKind = ScheduleOverrideKind.None;
        settings.ScheduleOverrideUntil = null;
        return true;
    }

    private void CheckSchedule()
    {
        AppSettings settings = SettingsService.Current;

        if (!settings.ScheduleEnabled)
        {
            if (ClearOverrideState(settings))
            {
                SettingsService.Save();
                StateChanged?.Invoke();
            }

            return;
        }

        if (ScheduleOverrideService.ClearIfExpired(settings, DateTimeOffset.Now))
        {
            SettingsService.Save();
            StateChanged?.Invoke();
            RefreshSchedule();
            return;
        }

        if (ScheduleOverrideService.IsActive(settings, DateTimeOffset.Now) || settings.SchedulePaused)
        {
            return;
        }

        if (ScheduleCalculator.GetCurrentDark(settings, DateTime.Now) is not bool current ||
            _lastScheduledDark == current)
        {
            return;
        }

        _themes.ApplyScheduledTheme(current);
        _lastScheduledDark = current;
    }
}
