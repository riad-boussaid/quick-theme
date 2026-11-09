using System.Diagnostics;
using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI;

namespace QuickTheme.App;

internal sealed class UpdateCoordinator
{
    private readonly ThemeFlyoutForm _form;
    private readonly TrayController _tray;
    private UpdateManifest? _availableUpdate;
    private bool _updateCheckRunning;

    public UpdateCoordinator(ThemeFlyoutForm form, TrayController tray)
    {
        _form = form;
        _tray = tray;

        _tray.CheckForUpdatesRequested += HandleMenuRequest;
        _tray.AboutRequested += ShowAbout;
        _tray.BalloonTipClicked += HandleBalloonClick;

        UpdateUpdateItemText();
    }

    public void RequestCheck() => HandleMenuRequest();

    public void ScheduleStartupCheck()
    {
        if (!SettingsService.Current.CheckForUpdatesOnStartup ||
            string.IsNullOrWhiteSpace(SettingsService.EffectiveUpdateFeedUrl))
        {
            return;
        }

        DateTimeOffset? lastCheck = SettingsService.Current.LastUpdateCheckUtc;
        if (lastCheck is null || DateTimeOffset.UtcNow - lastCheck.Value >= TimeSpan.FromHours(24))
        {
            _form.BeginInvoke(() => _ = CheckForUpdatesAsync(silent: true));
        }
    }

    private void HandleMenuRequest()
    {
        if (_availableUpdate is not null)
        {
            PromptInstallUpdate(_availableUpdate);
            return;
        }

        _ = CheckForUpdatesAsync(silent: false);
    }

    private void HandleBalloonClick()
    {
        if (_availableUpdate is not null)
        {
            PromptInstallUpdate(_availableUpdate);
        }
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
            _tray.RefreshMenuState();
            _tray.UpdateAppearance();
            _tray.SetUpdateItem("Checking for updates...", enabled: false);
        }

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
                    UiDialogs.Show(
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
                    _tray.ShowBalloon(
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
                UiDialogs.Show(
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
                UiDialogs.Show(
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

        if (UiDialogs.Show(page) != openSettingsButton)
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
            UiDialogs.Show(
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

        if (UiDialogs.Show(page) == installButton)
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
                UiDialogs.Show(
                    "Could not start the update",
                    ex.Message,
                    TaskDialogIcon.Error,
                    TaskDialogButton.OK);
            }

            return;
        }

        if (result == DialogResult.Abort && progressForm.Error is { } error)
        {
            UiDialogs.Show(
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

        if (UiDialogs.Show(page) == checkButton)
        {
            _ = CheckForUpdatesAsync(silent: false);
        }
    }

    private void UpdateUpdateItemText()
    {
        _tray.SetUpdateItem(
            _availableUpdate is null
                ? "Check for updates..."
                : $"Update to {_availableUpdate.Version}...",
            enabled: !_updateCheckRunning);
    }
}
