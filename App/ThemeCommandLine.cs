using System.Drawing;
using System.Globalization;
using System.Security;
using QuickTheme.Services;

namespace QuickTheme.App;

internal enum AccentCommandKind
{
    None,
    Manual,
    Automatic,
    Invalid,
}

internal readonly record struct AccentCommand(AccentCommandKind Kind, string? Value);

internal static class ThemeCommandLine
{
    public static bool IsShowArgument(string argument) =>
        argument.Equals("--show", StringComparison.OrdinalIgnoreCase);

    public static bool TryExecute(string[] args)
    {
        AccentCommand accentCommand = FindAccentCommand(args);
        bool executed = false;

        if (accentCommand.Kind == AccentCommandKind.Invalid)
        {
            Environment.ExitCode = 2;
            return true;
        }

        if (accentCommand.Kind != AccentCommandKind.None)
        {
            executed = true;

            try
            {
                if (accentCommand.Kind == AccentCommandKind.Automatic)
                {
                    AccentColorService.EnableAutomaticAccentAsync().GetAwaiter().GetResult();
                }
                else if (TryParseAccentColor(accentCommand.Value ?? string.Empty, out Color color))
                {
                    AccentColorService.Apply(
                        color,
                        AccentColorService.AccentOnTaskbar,
                        AccentColorService.AccentOnTitleBars);
                }
                else
                {
                    Environment.ExitCode = 2;
                    return true;
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException or InvalidOperationException)
            {
                Environment.ExitCode = 1;
            }
        }

        string? command = FindThemeCommand(args);
        if (command is null)
        {
            return executed;
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

    internal static AccentCommand FindAccentCommand(string[] args)
    {
        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];

            if (argument.Equals("--accent-auto", StringComparison.OrdinalIgnoreCase))
            {
                return new AccentCommand(AccentCommandKind.Automatic, null);
            }

            if (argument.Equals("--accent", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 < args.Length &&
                    !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return new AccentCommand(AccentCommandKind.Manual, args[index + 1]);
                }

                return new AccentCommand(AccentCommandKind.Invalid, null);
            }

            if (argument.StartsWith("--accent=", StringComparison.OrdinalIgnoreCase))
            {
                return new AccentCommand(
                    AccentCommandKind.Manual,
                    argument["--accent=".Length..]);
            }
        }

        return new AccentCommand(AccentCommandKind.None, null);
    }

    internal static bool TryParseAccentColor(string value, out Color color)
    {
        color = default;

        string text = value.Trim();
        if (text.StartsWith('#'))
        {
            text = text[1..];
        }

        if (text.Length != 6 ||
            !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            return false;
        }

        color = Color.FromArgb(
            255,
            (rgb >> 16) & 0xFF,
            (rgb >> 8) & 0xFF,
            rgb & 0xFF);
        return true;
    }

    internal static string? FindThemeCommand(string[] args) =>
        args.FirstOrDefault(argument =>
            argument.Equals("--dark", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("--light", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("--toggle", StringComparison.OrdinalIgnoreCase));
}
