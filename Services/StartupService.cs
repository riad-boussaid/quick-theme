using System.Security;
using Microsoft.Win32;

namespace QuickTheme.Services;

internal static class StartupService
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "QuickTheme";

    public static bool IsEnabled
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

    public static void SetEnabled(bool enabled)
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

    private static bool IsRegistryException(Exception ex) =>
        ex is SecurityException or UnauthorizedAccessException or IOException;
}
