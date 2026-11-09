using System.Drawing;
using System.Security;
using Microsoft.Win32;

namespace QuickTheme.Services;

internal static class ThemeService
{
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DwmPath = @"Software\Microsoft\Windows\DWM";

    private const string AppsLightValue = "AppsUseLightTheme";
    private const string SystemLightValue = "SystemUsesLightTheme";
    private const string TransparencyValue = "EnableTransparency";

    // Registry value absent -> Windows treats it as light mode / transparency on.
    private const int LightThemeDefault = 1;
    private const int TransparencyDefault = 1;

    private static Color _accentColor = ReadAccentColor();

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
}
