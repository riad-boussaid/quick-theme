namespace QuickTheme;

internal static class AppInfo
{
    public static Version Version { get; } =
        typeof(AppInfo).Assembly.GetName().Version ?? new Version(1, 0, 0);

    public static string DisplayVersion => Version.ToString(3);
}
