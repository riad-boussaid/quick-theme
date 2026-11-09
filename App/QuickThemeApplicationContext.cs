using QuickTheme.Services;
using QuickTheme.UI;

namespace QuickTheme.App;

internal sealed class QuickThemeApplicationContext : ApplicationContext
{
    private readonly ThemeFlyoutForm _form;
    private readonly TrayController _tray;
    private readonly ThemeController _themes;
    private readonly UpdateCoordinator _updates;
    private readonly Hotkeys _hotkeys;
    private readonly RegisteredWaitHandle _showRegistration;
    private DateTime _hiddenAt = DateTime.MinValue;

    public QuickThemeApplicationContext(EventWaitHandle showEvent, bool showOnStart)
    {
        SettingsService.Load();
        Palette.Apply(SettingsService.Current.FlyoutTheme, ThemeService.SystemUsesDarkTheme);

        _form = new ThemeFlyoutForm();
        _tray = new TrayController();
        _themes = new ThemeController(_form, _tray);
        _updates = new UpdateCoordinator(_form, _tray);
        _hotkeys = new Hotkeys();

        _form.VisibleChanged += OnFormVisibleChanged;
        _tray.OpenRequested += ShowPanel;
        _tray.ToggleDarkRequested += _themes.ToggleDark;
        _tray.ToggleAppsRequested += _themes.ToggleApps;
        _tray.ToggleSystemRequested += _themes.ToggleSystem;
        _tray.ToggleTransparencyRequested += _themes.ToggleTransparency;
        _tray.FlyoutThemeRequested += _themes.SetFlyoutTheme;
        _tray.QuitRequested += Quit;
        _tray.TrayClicked += OnTrayClicked;

        _hotkeys.ToggleDarkRequested += _themes.ToggleDark;
        _hotkeys.OpenPanelRequested += ShowPanel;

        // Create the handle up front so cross-instance show requests and theme
        // broadcasts can be received while the flyout is hidden.
        _ = _form.Handle;
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

    private void Quit()
    {
        _tray.Hide();
        Application.Exit();
    }
}
