using System.Diagnostics;
using System.Security;
using quick_theme.UI;

namespace quick_theme;

internal sealed class Program : ApplicationContext
{
    private const string MutexName = "QuickTheme.SingleInstance";
    private const string ShowEventName = "QuickTheme.ShowPanel";

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const int HotkeyToggleDark = 1;
    private const int HotkeyOpenPanel = 2;

    private readonly Form1 _form;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _flyoutThemeItem;
    private readonly Dictionary<FlyoutThemeMode, ToolStripMenuItem> _flyoutThemeItems = new();
    private readonly ToolStripMenuItem _updateItem;
    private readonly HotkeyWindow _hotkeys;
    private readonly RegisteredWaitHandle _showRegistration;
    private UpdateManifest? _availableUpdate;
    private bool _updateCheckRunning;
    private DateTime _hiddenAt = DateTime.MinValue;

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        // One-shot theme commands work whether or not the tray app is running.
        if (TryRunThemeCommand(args))
        {
            return;
        }

        if (!createdNew)
        {
            SignalFirstInstance();
            return;
        }

        ApplicationConfiguration.Initialize();

        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        using var context = new Program(showEvent, showOnStart: args.Any(IsShowArgument));
        Application.Run(context);
    }

    private Program(EventWaitHandle showEvent, bool showOnStart)
    {
        SettingsService.Load();
        Palette.Apply(SettingsService.Current.FlyoutTheme, ThemeService.SystemUsesDarkTheme);

        _form = new Form1();
        _form.ThemeChanged += (_, _) => UpdateTrayAppearance();
        _form.VisibleChanged += (_, _) =>
        {
            if (_form.Visible)
            {
                UpdateTrayAppearance();
            }
            else
            {
                _hiddenAt = DateTime.UtcNow;
            }
        };

        _startupItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true,
        };
        _startupItem.CheckedChanged += (_, _) => UpdateStartupText();
        _startupItem.Click += (_, _) => ThemeService.SetStartup(_startupItem.Checked);
        UpdateStartupText();

        var openItem = new ToolStripMenuItem("Open Quick Theme", null, (_, _) => ShowPanel())
        {
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ShortcutKeyDisplayString = "Ctrl+Alt+Q",
        };
        var toggleDarkItem = new ToolStripMenuItem("Toggle dark mode", null, (_, _) => ToggleDarkMode())
        {
            ShortcutKeyDisplayString = "Ctrl+Alt+D",
        };
        var toggleAppsItem = new ToolStripMenuItem("Toggle app theme", null, (_, _) => ToggleAppsTheme());
        var toggleSystemItem = new ToolStripMenuItem("Toggle system theme", null, (_, _) => ToggleSystemTheme());
        var toggleTransparencyItem = new ToolStripMenuItem("Toggle transparency", null, (_, _) => ToggleTransparency());

        _flyoutThemeItem = new ToolStripMenuItem("Flyout theme");
        foreach (FlyoutThemeMode mode in Enum.GetValues<FlyoutThemeMode>())
        {
            var modeItem = new ToolStripMenuItem(FlyoutThemeLabel(mode))
            {
                Tag = mode,
            };
            modeItem.Click += (_, _) => SetFlyoutTheme((FlyoutThemeMode)modeItem.Tag);
            _flyoutThemeItems[mode] = modeItem;
            _flyoutThemeItem.DropDownItems.Add(modeItem);
        }
        UpdateFlyoutThemeMenu();

        _updateItem = new ToolStripMenuItem("Check for updates...", null, (_, _) => OnUpdateItemClick());
        var aboutItem = new ToolStripMenuItem("About Quick Theme...", null, (_, _) => ShowAbout());

        var quitItem = new ToolStripMenuItem("Quit", null, (_, _) => Quit());

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
            _flyoutThemeItem,
            new ToolStripSeparator(),
            _updateItem,
            aboutItem,
            new ToolStripSeparator(),
            _startupItem,
            new ToolStripSeparator(),
            quitItem,
        ]);
        _menu.Opening += (_, _) =>
        {
            _startupItem.Checked = ThemeService.IsStartupEnabled;
            UpdateFlyoutThemeMenu();
            UpdateUpdateItemText();
        };

        _tray = new NotifyIcon
        {
            Text = "Quick Theme",
            Visible = true,
            ContextMenuStrip = _menu,
            Icon = Properties.Resources.themes_16,
        };
        _tray.MouseClick += OnTrayMouseClick;
        _tray.BalloonTipClicked += (_, _) =>
        {
            if (_availableUpdate is not null)
            {
                PromptInstallUpdate(_availableUpdate);
            }
        };

        _hotkeys = new HotkeyWindow();
        _hotkeys.HotkeyPressed += OnHotkeyPressed;
        _hotkeys.Register(HotkeyToggleDark, ModControl | ModAlt, Keys.D);
        _hotkeys.Register(HotkeyOpenPanel, ModControl | ModAlt, Keys.Q);

        UpdateTrayAppearance();

        // Create the handle up front so cross-instance show requests and theme
        // broadcasts can be received while the flyout is hidden.
        _ = _form.Handle;
        _showRegistration = ThreadPool.RegisterWaitForSingleObject(
            showEvent,
            OnShowRequested,
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        Application.ApplicationExit += (_, _) => _tray.Visible = false;

        if (showOnStart)
        {
            _form.BeginInvoke(ShowPanel);
        }

        if (SettingsService.Current.CheckForUpdatesOnStartup &&
            !string.IsNullOrWhiteSpace(SettingsService.EffectiveUpdateFeedUrl))
        {
            DateTimeOffset? lastCheck = SettingsService.Current.LastUpdateCheckUtc;
            if (lastCheck is null || DateTimeOffset.UtcNow - lastCheck.Value >= TimeSpan.FromHours(24))
            {
                _form.BeginInvoke(() => _ = CheckForUpdatesAsync(silent: true));
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeys.HotkeyPressed -= OnHotkeyPressed;
            _hotkeys.Dispose();
            _showRegistration.Unregister(null);
            _tray.Visible = false;
            _tray.Dispose();
            _menu.Dispose();
            _form.Dispose();
        }

        base.Dispose(disposing);
    }

    private static bool TryRunThemeCommand(string[] args)
    {
        string? command = args.FirstOrDefault(IsThemeCommand);
        if (command is null)
        {
            return false;
        }

        try
        {
            if (command.Equals("--dark", StringComparison.OrdinalIgnoreCase))
            {
                ThemeService.SetBothThemes(dark: true);
            }
            else if (command.Equals("--light", StringComparison.OrdinalIgnoreCase))
            {
                ThemeService.SetBothThemes(dark: false);
            }
            else
            {
                ThemeService.ToggleBothThemes();
            }

            ThemeService.NotifyThemeChangedNow();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException or InvalidOperationException)
        {
            Environment.ExitCode = 1;
        }

        return true;
    }

    private static bool IsThemeCommand(string argument) =>
        argument.Equals("--dark", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("--light", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("--toggle", StringComparison.OrdinalIgnoreCase);

    private static bool IsShowArgument(string argument) =>
        argument.Equals("--show", StringComparison.OrdinalIgnoreCase);

    private static void SignalFirstInstance()
    {
        try
        {
            using var handle = EventWaitHandle.OpenExisting(ShowEventName);
            handle.Set();
        }
        catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException or UnauthorizedAccessException)
        {
            // The first instance is not ready yet; there is nothing else to do.
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

    private void OnHotkeyPressed(int id)
    {
        switch (id)
        {
            case HotkeyToggleDark:
                ToggleDarkMode();
                break;
            case HotkeyOpenPanel:
                ShowPanel();
                break;
        }
    }

    private void OnTrayMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            ToggleDarkMode();
            return;
        }

        if (e.Button != MouseButtons.Left)
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

    private void ToggleDarkMode()
    {
        ApplyChange(() => ThemeService.ToggleBothThemes());
    }

    private void ToggleAppsTheme() =>
        ApplyChange(() => ThemeService.AppsUseDarkTheme = !ThemeService.AppsUseDarkTheme);

    private void ToggleSystemTheme() =>
        ApplyChange(() => ThemeService.SystemUsesDarkTheme = !ThemeService.SystemUsesDarkTheme);

    private void ToggleTransparency() =>
        ApplyChange(() => ThemeService.TransparencyEnabled = !ThemeService.TransparencyEnabled);

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

    private void SetFlyoutTheme(FlyoutThemeMode mode)
    {
        SettingsService.Current.FlyoutTheme = mode;
        SettingsService.Save();

        _form.RefreshThemeState();
        _menu.Invalidate(true);
        UpdateFlyoutThemeMenu();
    }

    private void OnUpdateItemClick()
    {
        if (_availableUpdate is not null)
        {
            PromptInstallUpdate(_availableUpdate);
            return;
        }

        _ = CheckForUpdatesAsync(silent: false);
    }

    private void UpdateUpdateItemText()
    {
        _updateItem.Text = _availableUpdate is null
            ? "Check for updates..."
            : $"Update to {_availableUpdate.Version}...";
        _updateItem.Enabled = !_updateCheckRunning;
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (_updateCheckRunning)
        {
            return;
        }

        _updateCheckRunning = true;

        if (!silent)
        {
            SettingsService.Reload();
            _form.RefreshThemeState();
            UpdateFlyoutThemeMenu();
            _updateItem.Text = "Checking for updates...";
        }

        _updateItem.Enabled = false;

        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            UpdateCheckResult result = await UpdateService.CheckAsync(cancellation.Token);

            if (!result.IsConfigured)
            {
                if (!silent)
                {
                    ShowUpdateNotConfigured();
                }

                return;
            }

            if (result.Error is not null)
            {
                if (!silent)
                {
                    ShowTaskDialog(
                        "Could not check for updates",
                        result.Error,
                        TaskDialogIcon.Error,
                        TaskDialogButton.OK);
                }

                return;
            }

            SettingsService.Current.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
            SettingsService.Save();

            if (result.IsNewer && result.Manifest is not null)
            {
                _availableUpdate = result.Manifest;

                if (silent)
                {
                    _tray.ShowBalloonTip(
                        5000,
                        "Quick Theme",
                        $"Version {result.Manifest.Version} is available.",
                        ToolTipIcon.Info);
                }
                else
                {
                    PromptInstallUpdate(result.Manifest);
                }

                return;
            }

            _availableUpdate = null;

            if (!silent)
            {
                ShowTaskDialog(
                    "You're up to date",
                    $"Quick Theme {AppInfo.DisplayVersion} is the latest version.",
                    TaskDialogIcon.Information,
                    TaskDialogButton.OK);
            }
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                ShowTaskDialog(
                    "Could not check for updates",
                    ex.Message,
                    TaskDialogIcon.Error,
                    TaskDialogButton.OK);
            }
        }
        finally
        {
            _updateCheckRunning = false;
            UpdateUpdateItemText();
        }
    }

    private void ShowUpdateNotConfigured()
    {
        var openSettingsButton = new TaskDialogButton("Open settings file");

        var page = new TaskDialogPage
        {
            Caption = "Quick Theme",
            Heading = "Update feed not configured",
            Text =
                "Add an UpdateFeedUrl to settings.json, or build the app with an embedded update feed.\n\n" +
                "The feed is a JSON manifest containing version, url, and sha256 fields.",
            Icon = TaskDialogIcon.Information,
            Buttons = { openSettingsButton, TaskDialogButton.OK },
            AllowCancel = true,
        };

        if (ShowTaskDialog(page) != openSettingsButton)
        {
            return;
        }

        SettingsService.EnsureSettingsFile();

        try
        {
            Process.Start(new ProcessStartInfo(SettingsService.SettingsFilePath)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            ShowTaskDialog(
                "Could not open settings",
                ex.Message,
                TaskDialogIcon.Error,
                TaskDialogButton.OK);
        }
    }

    private void PromptInstallUpdate(UpdateManifest manifest)
    {
        var installButton = new TaskDialogButton("Download and install");

        var page = new TaskDialogPage
        {
            Caption = "Quick Theme",
            Heading = $"Quick Theme {manifest.Version} is available",
            Text = $"You have version {AppInfo.DisplayVersion}.\n\n" +
                   (string.IsNullOrWhiteSpace(manifest.Notes)
                       ? "A newer version is ready."
                       : manifest.Notes),
            Icon = TaskDialogIcon.Information,
            Buttons = { installButton, TaskDialogButton.Cancel },
            DefaultButton = installButton,
            AllowCancel = true,
        };

        if (ShowTaskDialog(page) == installButton)
        {
            StartUpdate(manifest);
        }
    }

    private void StartUpdate(UpdateManifest manifest)
    {
        using var progressForm = new UpdateProgressForm(manifest);
        DialogResult result = progressForm.ShowDialog();

        if (result == DialogResult.OK && progressForm.DownloadedPath is { Length: > 0 } downloadedPath)
        {
            try
            {
                UpdateService.LaunchUpdaterAndExit(downloadedPath);
                Application.Exit();
            }
            catch (Exception ex)
            {
                ShowTaskDialog(
                    "Could not start the update",
                    ex.Message,
                    TaskDialogIcon.Error,
                    TaskDialogButton.OK);
            }

            return;
        }

        if (result == DialogResult.Abort && progressForm.Error is { } error)
        {
            ShowTaskDialog(
                "Could not download the update",
                error.Message,
                TaskDialogIcon.Error,
                TaskDialogButton.OK);
        }
    }

    private void ShowAbout()
    {
        var checkButton = new TaskDialogButton("Check for updates");
        bool configured = !string.IsNullOrWhiteSpace(SettingsService.EffectiveUpdateFeedUrl);

        var page = new TaskDialogPage
        {
            Caption = "Quick Theme",
            Heading = $"Quick Theme {AppInfo.DisplayVersion}",
            Text =
                "A lightweight tray utility for Windows light and dark appearance.\n\n" +
                $".NET {Environment.Version.Major} · win-x64\n" +
                $"Update feed: {(configured ? "configured" : "not configured")}",
            Icon = TaskDialogIcon.Information,
            Buttons = { checkButton, TaskDialogButton.OK },
            AllowCancel = true,
        };

        if (ShowTaskDialog(page) == checkButton)
        {
            _ = CheckForUpdatesAsync(silent: false);
        }
    }

    private static TaskDialogButton ShowTaskDialog(TaskDialogPage page) =>
        TaskDialog.ShowDialog(page);

    private static TaskDialogButton ShowTaskDialog(
        string heading,
        string text,
        TaskDialogIcon icon,
        params TaskDialogButton[] buttons)
    {
        var page = new TaskDialogPage
        {
            Caption = "Quick Theme",
            Heading = heading,
            Text = text,
            Icon = icon,
            AllowCancel = true,
        };

        foreach (TaskDialogButton button in buttons)
        {
            page.Buttons.Add(button);
        }

        return ShowTaskDialog(page);
    }

    private void ApplyChange(Action change)
    {
        try
        {
            change();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException or InvalidOperationException)
        {
            ShowThemeError(ex);
            return;
        }

        ThemeService.NotifyThemeChanged();
        _form.RefreshThemeState();
        UpdateTrayAppearance();
    }

    private void ShowThemeError(Exception exception)
    {
        var page = new TaskDialogPage
        {
            Caption = "Quick Theme",
            Heading = "Could not change the theme",
            Text = exception.Message,
            Icon = TaskDialogIcon.Error,
            Buttons = { TaskDialogButton.OK },
        };

        if (_form.Visible)
        {
            TaskDialog.ShowDialog(_form, page);
        }
        else
        {
            TaskDialog.ShowDialog(page);
        }
    }

    private void ShowPanel() => _form.ShowPanel();

    private void Quit()
    {
        _tray.Visible = false;
        Application.Exit();
    }

    private void UpdateStartupText()
    {
        _startupItem.Text = _startupItem.Checked
            ? "\u2713  Start with Windows"
            : "Start with Windows";
    }

    private void UpdateTrayAppearance()
    {
        bool largeIcon = SystemInformation.SmallIconSize.Width > 20;

        _tray.Icon = ThemeService.TaskbarUsesLightTheme
            ? (largeIcon ? Properties.Resources.themes_32_black : Properties.Resources.themes_16_black)
            : (largeIcon ? Properties.Resources.themes_32 : Properties.Resources.themes_16);

        _tray.Text = ThemeService.SystemUsesDarkTheme
            ? "Quick Theme - dark system theme"
            : "Quick Theme - light system theme";
    }
}
