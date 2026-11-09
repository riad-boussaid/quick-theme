using System.Security;
using QuickTheme.Services;

namespace QuickTheme.App;

internal static class ThemeCommandLine
{
    public static bool IsShowArgument(string argument) =>
        argument.Equals("--show", StringComparison.OrdinalIgnoreCase);

    public static bool TryExecute(string[] args)
    {
        string? command = FindThemeCommand(args);
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

            SystemThemeNotifier.NotifyThemeChangedNow();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException or InvalidOperationException)
        {
            Environment.ExitCode = 1;
        }

        return true;
    }

    internal static string? FindThemeCommand(string[] args) =>
        args.FirstOrDefault(argument =>
            argument.Equals("--dark", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("--light", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("--toggle", StringComparison.OrdinalIgnoreCase));
}
