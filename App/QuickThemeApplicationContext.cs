using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI;

namespace QuickTheme.App;

internal sealed class QuickThemeApplicationContext : ApplicationContext
{
    private readonly ThemeFlyoutForm _form;
    private readonly TrayController _tray;
    private readonly ThemeController _themes;
    private readonly UpdateCoordinator _updates;
    private readonly ScheduleController _schedule;
    private readonly Hotkeys _hotkeys;
    private readonly RegisteredWaitHandle _showRegistration;
    private SettingsForm? _settingsForm;
    private DateTime _hiddenAt = DateTime.MinValue;

    public QuickThemeApplicationContext(EventWaitHandle showEvent, bool showOnStart)
    {
        SettingsService.Load();
        Palette.Apply(SettingsService.Current.FlyoutTheme, ThemeService.SystemUsesDarkTheme);

        _form = new ThemeFlyoutForm();
        _tray = new TrayController();
        _themes = new ThemeController(_form, _tray);
        _updates = new UpdateCoordinator(_form, _tray);
        _schedule = new ScheduleController(_themes);
        _hotkeys = new Hotkeys(
            SettingsService.Current.HotkeyToggleDark,
            SettingsService.Current.HotkeyOpenPanel);

        if ((!string.IsNullOrEmpty(_hotkeys.ToggleGestureText) &&
             SettingsService.Current.HotkeyToggleDark != _hotkeys.ToggleGestureText) ||
            (!string.IsNullOrEmpty(_hotkeys.OpenGestureText) &&
             SettingsService.Current.HotkeyOpenPanel != _hotkeys.OpenGestureText))
        {
            if (!string.IsNullOrEmpty(_hotkeys.ToggleGestureText))
            {
                SettingsService.Current.HotkeyToggleDark = _hotkeys.ToggleGestureText;
            }

            if (!string.IsNullOrEmpty(_hotkeys.OpenGestureText))
            {
                SettingsService.Current.HotkeyOpenPanel = _hotkeys.OpenGestureText;
            }

            SettingsService.Save();
        }

        _form.VisibleChanged += OnFormVisibleChanged;
        _form.SettingsRequested += ShowSettings;
        _form.ResumeScheduleRequested += ResumeSchedule;
        _schedule.StateChanged += OnScheduleStateChanged;
        _tray.OpenRequested += ShowPanel;
        _tray.ToggleDarkRequested += _themes.ToggleDark;
        _tray.ToggleAppsRequested += _themes.ToggleApps;
        _tray.ToggleSystemRequested += _themes.ToggleSystem;
        _tray.ToggleTransparencyRequested += _themes.ToggleTransparency;
        _tray.FlyoutThemeRequested += _themes.SetFlyoutTheme;
        _tray.PresetRequested += _themes.ApplyPreset;
        _tray.SettingsRequested += ShowSettings;
        _tray.SchedulePauseChanged += OnSchedulePauseChanged;
        _tray.ScheduleOverrideRequested += OnScheduleOverrideRequested;
        _tray.ClearScheduleOverrideRequested += OnClearScheduleOverrideRequested;
        _tray.QuitRequested += Quit;
        _tray.TrayClicked += OnTrayClicked;

        _hotkeys.ToggleDarkRequested += _themes.ToggleDark;
        _hotkeys.OpenPanelRequested += ShowPanel;

        // Create the handle up front so cross-instance show requests and theme
        // broadcasts can be received while the flyout is hidden.
        _ = _form.Handle;
        _schedule.Start();
        _showRegistration = ThreadPool.RegisterWaitForSingleObject(
            showEvent,
            OnShowRequested,
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        Application.ApplicationExit += (_, _) => _tray.Hide();

        if (showOnStart)
        {
            _form.BeginInvoke(ShowPanel);
        }

        _updates.ScheduleStartupCheck();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeys.Dispose();
            _showRegistration.Unregister(null);
            _schedule.Dispose();
            _settingsForm?.Dispose();
            _tray.Dispose();
            _form.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnFormVisibleChanged(object? sender, EventArgs e)
    {
        if (_form.Visible)
        {
            _tray.UpdateAppearance();
        }
        else
        {
            _hiddenAt = DateTime.UtcNow;
        }
    }

    private void OnTrayClicked(MouseButtons button)
    {
        if (button == MouseButtons.Middle)
        {
            _themes.ToggleDark();
            return;
        }

        if (button != MouseButtons.Left)
        {
            return;
        }

        // Clicking the tray icon makes the flyout lose focus and hide before the click
        // arrives, so ignore clicks that land right after an auto-hide.
        if ((DateTime.UtcNow - _hiddenAt).TotalMilliseconds < 200)
        {
            return;
        }

        if (_form.Visible)
        {
            _form.HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    private void OnShowRequested(object? state, bool timedOut)
    {
        if (timedOut || _form.IsDisposed)
        {
            return;
        }

        try
        {
            _form.BeginInvoke(ShowPanel);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // The form went away while the request was in flight.
        }
    }

    private void ShowPanel() => _form.ShowPanel();

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            _settingsForm.BringToFront();
            return;
        }

        _settingsForm = new SettingsForm(
            OnScheduleSettingsChanged,
            _updates.RequestCheck,
            ApplyHotkeys);
        _settingsForm.FormClosed += (_, _) =>
        {
            _settingsForm = null;
            _tray.RefreshMenuState();
        };
        _settingsForm.Show();
    }

    private string? ApplyHotkeys(string toggle, string open)
    {
        if (!_hotkeys.TryUpdate(toggle, open, out string? error))
        {
            return error;
        }

        SettingsService.Current.HotkeyToggleDark = _hotkeys.ToggleGestureText;
        SettingsService.Current.HotkeyOpenPanel = _hotkeys.OpenGestureText;
        SettingsService.Save();
        _tray.RefreshMenuState();
        return null;
    }

    private void OnScheduleSettingsChanged()
    {
        _schedule.RefreshSchedule();
        _form.RefreshScheduleStatus();
        _tray.RefreshMenuState();
    }

    private void OnSchedulePauseChanged()
    {
        if (SettingsService.Current.SchedulePaused)
        {
            _schedule.ClearOverride();
        }
        else
        {
            _schedule.RefreshSchedule();
        }

        _form.RefreshScheduleStatus();
        _settingsForm?.RefreshFromSettings();
        _tray.RefreshMenuState();
    }

    private void OnScheduleOverrideRequested(ScheduleOverrideKind kind)
    {
        if (!_schedule.SetOverride(kind))
        {
            _tray.ShowBalloon(
                "Quick Theme",
                "There is no upcoming scheduled switch to override.",
                ToolTipIcon.Info);
        }
    }

    private void OnClearScheduleOverrideRequested()
    {
        _schedule.ClearOverride();
    }

    private void ResumeSchedule()
    {
        _schedule.ResumeSchedule();
    }

    private void OnScheduleStateChanged()
    {
        _tray.RefreshMenuState();
        _form.RefreshScheduleStatus();
        _settingsForm?.RefreshFromSettings();
    }

    private void Quit()
    {
        _tray.Hide();
        Application.Exit();
    }
}
