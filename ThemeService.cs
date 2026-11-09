using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security;
using Microsoft.Win32;

namespace quick_theme;

internal static class ThemeService
{
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DwmPath = @"Software\Microsoft\Windows\DWM";
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "QuickTheme";

    private const string AppsLightValue = "AppsUseLightTheme";
    private const string SystemLightValue = "SystemUsesLightTheme";
    private const string TransparencyValue = "EnableTransparency";

    // Registry value absent -> Windows treats it as light mode / transparency on.
    private const int LightThemeDefault = 1;
    private const int TransparencyDefault = 1;

    public static bool AppsUseDarkTheme
    {
        get => ReadDword(PersonalizePath, AppsLightValue, LightThemeDefault) == 0;
        set => WriteDword(PersonalizePath, AppsLightValue, value ? 0 : 1);
    }

    public static bool SystemUsesDarkTheme
    {
        get => ReadDword(PersonalizePath, SystemLightValue, LightThemeDefault) == 0;
        set => WriteDword(PersonalizePath, SystemLightValue, value ? 0 : 1);
    }

    public static bool TransparencyEnabled
    {
        get => ReadDword(PersonalizePath, TransparencyValue, TransparencyDefault) != 0;
        set => WriteDword(PersonalizePath, TransparencyValue, value ? 1 : 0);
    }

    public static bool TaskbarUsesLightTheme => !SystemUsesDarkTheme;

    private static Color _accentColor = ReadAccentColor();

    public static Color AccentColor => _accentColor;

    public static void RefreshAccentColor() => _accentColor = ReadAccentColor();

    public static void SetBothThemes(bool dark)
    {
        AppsUseDarkTheme = dark;
        SystemUsesDarkTheme = dark;
    }

    public static bool ToggleBothThemes()
    {
        bool targetDark = !(AppsUseDarkTheme && SystemUsesDarkTheme);
        SetBothThemes(targetDark);
        return targetDark;
    }

    public static bool IsStartupEnabled
    {
        get
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunPath);
                return key?.GetValue(RunValue) is string value && value.Length > 0;
            }
            catch (Exception ex) when (IsRegistryException(ex))
            {
                return false;
            }
        }
    }

    public static void SetStartup(bool enabled)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RunPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (enabled)
            {
                string? executable = Environment.ProcessPath;
                if (string.IsNullOrEmpty(executable))
                {
                    return;
                }

                key.SetValue(RunValue, $"\"{executable}\"", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(RunValue, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (IsRegistryException(ex))
        {
            // The menu re-reads the actual state every time it opens.
        }
    }

    public static void NotifyThemeChanged()
    {
        // Best effort, off the UI thread: let listening apps refresh without restarting
        // Explorer. This must never block the flyout.
        ThreadPool.QueueUserWorkItem(static _ => NotifyThemeChangedNow());
    }

    public static void NotifyThemeChangedNow()
    {
        NotifySettingChanged("ImmersiveColorSet");
    }

    public static async Task RestartExplorerAsync()
    {
        foreach (Process process in Process.GetProcessesByName("explorer"))
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // Explorer may already be gone.
            }
            finally
            {
                process.Dispose();
            }
        }

        // Windows normally restarts the shell by itself; start it manually if it did not.
        for (int attempt = 0; attempt < 24; attempt++)
        {
            await Task.Delay(250).ConfigureAwait(false);

            Process[] current = Process.GetProcessesByName("explorer");
            bool running = current.Length > 0;
            foreach (Process process in current)
            {
                process.Dispose();
            }

            if (running)
            {
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // Nothing else we can do here.
        }
    }

    private static int ReadDword(string path, string name, int fallback)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(path);
            return key?.GetValue(name) is int value ? value : fallback;
        }
        catch (Exception ex) when (IsRegistryException(ex))
        {
            return fallback;
        }
    }

    private static void WriteDword(string path, string name, int value)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException($"Could not open HKCU\\{path}.");

        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static Color ReadAccentColor()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(DwmPath);
            if (key?.GetValue("AccentColor") is int abgr)
            {
                // AccentColor is stored as 0xAABBGGRR.
                Color color = Color.FromArgb(
                    255,
                    abgr & 0xFF,
                    (abgr >> 8) & 0xFF,
                    (abgr >> 16) & 0xFF);

                if (color.GetSaturation() >= 0.15f && color.GetBrightness() >= 0.25f)
                {
                    return color;
                }
            }
        }
        catch (Exception ex) when (IsRegistryException(ex))
        {
            // Fall through to the default accent.
        }

        return Color.FromArgb(76, 194, 255);
    }

    private static bool IsRegistryException(Exception ex) =>
        ex is SecurityException or UnauthorizedAccessException or IOException;

    private static void NotifySettingChanged(string setting)
    {
        _ = SendMessageTimeout(
            HwndBroadcast,
            WmSettingChange,
            IntPtr.Zero,
            setting,
            SmtoAbortIfHung,
            timeout: 250,
            out _);
    }

    private const IntPtr HwndBroadcast = (IntPtr)0xFFFF;
    private const uint WmSettingChange = 0x001A;
    private const uint SmtoAbortIfHung = 0x0002;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        string lParam,
        uint fuFlags,
        uint timeout,
        out IntPtr result);
}
