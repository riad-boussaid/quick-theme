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

    public void Start()
    {
        RefreshSchedule();
        _timer.Start();
    }

    public void RefreshSchedule()
    {
        if (!IsActive)
        {
            return;
        }

        if (ScheduleCalculator.GetCurrentDark(SettingsService.Current, DateTime.Now) is bool dark)
        {
            _themes.ApplyScheduledTheme(dark);
            _lastScheduledDark = dark;
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private static bool IsActive =>
        SettingsService.Current.ScheduleEnabled && !SettingsService.Current.SchedulePaused;

    private void CheckSchedule()
    {
        if (!IsActive)
        {
            return;
        }

        if (ScheduleCalculator.GetCurrentDark(SettingsService.Current, DateTime.Now) is not bool dark ||
            _lastScheduledDark == dark)
        {
            return;
        }

        _themes.ApplyScheduledTheme(dark);
        _lastScheduledDark = dark;
    }
}
