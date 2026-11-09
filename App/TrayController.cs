using System.Drawing;
using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI;

namespace QuickTheme.App;

internal sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _flyoutThemeItem;
    private readonly Dictionary<FlyoutThemeMode, ToolStripMenuItem> _flyoutThemeItems = new();
    private readonly ToolStripMenuItem _pauseScheduleItem;
    private readonly ToolStripMenuItem _openItem;
    private readonly ToolStripMenuItem _toggleDarkItem;
    private readonly ToolStripMenuItem _updateItem;
    private bool _disposed;

    public TrayController()
    {
        _startupItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true,
        };
        _startupItem.CheckedChanged += (_, _) => UpdateStartupText();
        _startupItem.Click += (_, _) => StartupService.SetEnabled(_startupItem.Checked);
        UpdateStartupText();

        var openItem = new ToolStripMenuItem("Open Quick Theme", null, (_, _) => OpenRequested?.Invoke())
        {
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ShortcutKeyDisplayString = "Ctrl+Alt+Q",
        };
        _openItem = openItem;

        var toggleDarkItem = new ToolStripMenuItem("Toggle dark mode", null, (_, _) => ToggleDarkRequested?.Invoke())
        {
            ShortcutKeyDisplayString = "Ctrl+Alt+D",
        };
        _toggleDarkItem = toggleDarkItem;
        var toggleAppsItem = new ToolStripMenuItem("Toggle app theme", null, (_, _) => ToggleAppsRequested?.Invoke());
        var toggleSystemItem = new ToolStripMenuItem("Toggle system theme", null, (_, _) => ToggleSystemRequested?.Invoke());
        var toggleTransparencyItem = new ToolStripMenuItem("Toggle transparency", null, (_, _) => ToggleTransparencyRequested?.Invoke());

        _flyoutThemeItem = new ToolStripMenuItem("Flyout theme");
        foreach (FlyoutThemeMode mode in Enum.GetValues<FlyoutThemeMode>())
        {
            var modeItem = new ToolStripMenuItem(FlyoutThemeLabel(mode))
            {
                Tag = mode,
            };
            modeItem.Click += (_, _) => FlyoutThemeRequested?.Invoke((FlyoutThemeMode)modeItem.Tag);
            _flyoutThemeItems[mode] = modeItem;
            _flyoutThemeItem.DropDownItems.Add(modeItem);
        }

        UpdateFlyoutThemeMenu();

        var presetsItem = new ToolStripMenuItem("Presets");
        var allDarkItem = new ToolStripMenuItem("All dark", null, (_, _) => PresetRequested?.Invoke(ThemePreset.AllDark));
        var allLightItem = new ToolStripMenuItem("All light", null, (_, _) => PresetRequested?.Invoke(ThemePreset.AllLight));
        var followSystemItem = new ToolStripMenuItem("Apps follow system", null, (_, _) => PresetRequested?.Invoke(ThemePreset.AppsFollowSystem));
        presetsItem.DropDownItems.AddRange([allDarkItem, allLightItem, followSystemItem]);

        _pauseScheduleItem = new ToolStripMenuItem("Pause schedule", null, (_, _) => ToggleSchedulePause());

        _updateItem = new ToolStripMenuItem("Check for updates...", null, (_, _) => CheckForUpdatesRequested?.Invoke());
        var settingsItem = new ToolStripMenuItem("Settings...", null, (_, _) => SettingsRequested?.Invoke());
        var aboutItem = new ToolStripMenuItem("About Quick Theme...", null, (_, _) => AboutRequested?.Invoke());
        var quitItem = new ToolStripMenuItem("Quit", null, (_, _) => QuitRequested?.Invoke());

        _menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            BackColor = Palette.MenuSurface,
            ForeColor = Palette.Text,
            Font = new Font("Segoe UI", 9F),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Padding(3, 5, 3, 5),
        };

        _menu.Items.AddRange(
        [
            openItem,
            new ToolStripSeparator(),
            toggleDarkItem,
            toggleAppsItem,
            toggleSystemItem,
            toggleTransparencyItem,
            new ToolStripSeparator(),
            presetsItem,
            _flyoutThemeItem,
            _pauseScheduleItem,
            new ToolStripSeparator(),
            settingsItem,
            _updateItem,
            aboutItem,
            new ToolStripSeparator(),
            _startupItem,
            new ToolStripSeparator(),
            quitItem,
        ]);
        _menu.Opening += (_, _) => RefreshMenuState();

        _tray = new NotifyIcon
        {
            Text = "Quick Theme",
            Visible = true,
            ContextMenuStrip = _menu,
            Icon = Properties.Resources.themes_16,
        };
        _tray.MouseClick += (_, e) => TrayClicked?.Invoke(e.Button);
        _tray.BalloonTipClicked += (_, _) => BalloonTipClicked?.Invoke();

        UpdateAppearance();
    }

    public event Action? OpenRequested;
    public event Action? ToggleDarkRequested;
    public event Action? ToggleAppsRequested;
    public event Action? ToggleSystemRequested;
    public event Action? ToggleTransparencyRequested;
    public event Action<FlyoutThemeMode>? FlyoutThemeRequested;
    public event Action<ThemePreset>? PresetRequested;
    public event Action? SettingsRequested;
    public event Action? SchedulePauseChanged;
    public event Action? CheckForUpdatesRequested;
    public event Action? AboutRequested;
    public event Action? QuitRequested;
    public event Action? BalloonTipClicked;
    public event Action<MouseButtons>? TrayClicked;

    public void UpdateAppearance()
    {
        if (_disposed)
        {
            return;
        }

        bool largeIcon = SystemInformation.SmallIconSize.Width > 20;

        _tray.Icon = ThemeService.TaskbarUsesLightTheme
            ? (largeIcon ? Properties.Resources.themes_32_black : Properties.Resources.themes_16_black)
            : (largeIcon ? Properties.Resources.themes_32 : Properties.Resources.themes_16);

        _tray.Text = ThemeService.SystemUsesDarkTheme
            ? "Quick Theme - dark system theme"
            : "Quick Theme - light system theme";
    }

    public void RefreshMenuState()
    {
        _startupItem.Checked = StartupService.IsEnabled;
        UpdateFlyoutThemeMenu();

        AppSettings settings = SettingsService.Current;
        _pauseScheduleItem.Enabled = settings.ScheduleEnabled;
        _pauseScheduleItem.Text = settings.SchedulePaused ? "Resume schedule" : "Pause schedule";
        _openItem.ShortcutKeyDisplayString = settings.HotkeyOpenPanel;
        _toggleDarkItem.ShortcutKeyDisplayString = settings.HotkeyToggleDark;
    }

    public void SetUpdateItem(string text, bool enabled)
    {
        _updateItem.Text = text;
        _updateItem.Enabled = enabled;
    }

    public void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        _tray.ShowBalloonTip(5000, title, text, icon);
    }

    public void Hide()
    {
        if (!_disposed)
        {
            _tray.Visible = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
    }

    private static string FlyoutThemeLabel(FlyoutThemeMode mode) => mode switch
    {
        FlyoutThemeMode.Light => "Light",
        FlyoutThemeMode.Dark => "Dark",
        _ => "System",
    };

    private void UpdateFlyoutThemeMenu()
    {
        FlyoutThemeMode activeMode = SettingsService.Current.FlyoutTheme;
        _flyoutThemeItem.Text = $"Flyout theme: {FlyoutThemeLabel(activeMode)}";

        foreach ((FlyoutThemeMode mode, ToolStripMenuItem item) in _flyoutThemeItems)
        {
            item.Text = (mode == activeMode ? "\u2713  " : "    ") + FlyoutThemeLabel(mode);
        }
    }

    private void ToggleSchedulePause()
    {
        SettingsService.Current.SchedulePaused = !SettingsService.Current.SchedulePaused;
        SettingsService.Save();
        RefreshMenuState();
        SchedulePauseChanged?.Invoke();
    }

    private void UpdateStartupText()
    {
        _startupItem.Text = _startupItem.Checked
            ? "\u2713  Start with Windows"
            : "Start with Windows";
    }
}
