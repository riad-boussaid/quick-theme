using QuickTheme.Models;
using QuickTheme.Services;
using QuickTheme.UI;

namespace QuickTheme.App;

internal sealed class ThemeController
{
    private readonly ThemeFlyoutForm _form;
    private readonly TrayController _tray;

    public ThemeController(ThemeFlyoutForm form, TrayController tray)
    {
        _form = form;
        _tray = tray;
        _form.SystemThemeChanged += (_, _) => _tray.UpdateAppearance();
        _form.SettingToggleRequested += OnSettingToggleRequested;
    }

    public void ToggleDark() => ApplyChange(() => ThemeService.ToggleBothThemes());

    public void ToggleApps() =>
        ApplyChange(() => ThemeService.AppsUseDarkTheme = !ThemeService.AppsUseDarkTheme);

    public void ToggleSystem() =>
        ApplyChange(() => ThemeService.SystemUsesDarkTheme = !ThemeService.SystemUsesDarkTheme);

    public void ToggleTransparency() =>
        ApplyChange(() => ThemeService.TransparencyEnabled = !ThemeService.TransparencyEnabled);

    public void SetFlyoutTheme(FlyoutThemeMode mode)
    {
        SettingsService.Current.FlyoutTheme = mode;
        SettingsService.Save();

        _form.RefreshThemeState();
        _tray.RefreshMenuState();
    }

    public void ApplyPreset(ThemePreset preset)
    {
        switch (preset)
        {
            case ThemePreset.AllDark:
                ApplyChange(() => ThemeService.SetBothThemes(dark: true));
                break;
            case ThemePreset.AllLight:
                ApplyChange(() => ThemeService.SetBothThemes(dark: false));
                break;
            case ThemePreset.AppsFollowSystem:
                ApplyChange(() => ThemeService.AppsUseDarkTheme = ThemeService.SystemUsesDarkTheme);
                break;
        }
    }

    public void ApplyScheduledTheme(bool dark)
    {
        ScheduleScope scope = SettingsService.Current.ScheduleScope;
        bool appsNeeded = scope is ScheduleScope.Both or ScheduleScope.Apps;
        bool systemNeeded = scope is ScheduleScope.Both or ScheduleScope.System;

        bool changeApps = appsNeeded && ThemeService.AppsUseDarkTheme != dark;
        bool changeSystem = systemNeeded && ThemeService.SystemUsesDarkTheme != dark;

        if (!changeApps && !changeSystem)
        {
            return;
        }

        ApplyChange(() =>
        {
            if (changeApps)
            {
                ThemeService.AppsUseDarkTheme = dark;
            }

            if (changeSystem)
            {
                ThemeService.SystemUsesDarkTheme = dark;
            }
        });
    }

    public void RefreshFromSettings()
    {
        _form.RefreshThemeState();
        _tray.RefreshMenuState();
        _tray.UpdateAppearance();
    }

    private void OnSettingToggleRequested(ThemeSetting setting, bool value)
    {
        ApplyChange(() =>
        {
            switch (setting)
            {
                case ThemeSetting.Apps:
                    ThemeService.AppsUseDarkTheme = value;
                    break;
                case ThemeSetting.System:
                    ThemeService.SystemUsesDarkTheme = value;
                    break;
                case ThemeSetting.Transparency:
                    ThemeService.TransparencyEnabled = value;
                    break;
            }
        });
    }

    private void ApplyChange(Action change)
    {
        try
        {
            change();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or InvalidOperationException)
        {
            _form.RefreshThemeState();
            _form.ShowThemeError(ex.Message);
            return;
        }

        SystemThemeNotifier.NotifyThemeChanged();
        _form.RefreshThemeState();
        _tray.UpdateAppearance();
    }
}
