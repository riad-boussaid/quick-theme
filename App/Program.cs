using QuickTheme.App;

namespace QuickTheme;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var singleInstance = new SingleInstance();

        // One-shot theme commands work whether or not the tray app is running.
        if (ThemeCommandLine.TryExecute(args))
        {
            return;
        }

        if (!singleInstance.IsFirstInstance)
        {
            singleInstance.SignalFirstInstance();
            return;
        }

        ApplicationConfiguration.Initialize();

        using var showEvent = singleInstance.CreateShowEvent();
        using var context = new QuickThemeApplicationContext(
            showEvent,
            showOnStart: args.Any(ThemeCommandLine.IsShowArgument));

        Application.Run(context);
    }
}
