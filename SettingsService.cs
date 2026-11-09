using System.Reflection;

namespace quick_theme;

internal enum FlyoutThemeMode
{
    System,
    Dark,
    Light,
}

internal sealed class AppSettings
{
    public FlyoutThemeMode FlyoutTheme { get; set; } = FlyoutThemeMode.System;
    public string UpdateFeedUrl { get; set; } = string.Empty;
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }
}

internal static class SettingsService
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public static AppSettings Current { get; private set; } = new();

    public static string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Quick Theme");

    public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

    public static string? EffectiveUpdateFeedUrl
    {
        get
        {
            string? environmentValue = Environment.GetEnvironmentVariable("QUICKTHEME_UPDATE_FEED");
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue.Trim();
            }

            if (!string.IsNullOrWhiteSpace(Current.UpdateFeedUrl))
            {
                return Current.UpdateFeedUrl.Trim();
            }

            return Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "UpdateFeedUrl")
                ?.Value is { Length: > 0 } embedded
                ? embedded
                : null;
        }
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Current = new AppSettings();
                Save();
                return;
            }

            string json = File.ReadAllText(SettingsFilePath);
            Current = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                      ?? new AppSettings();
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public static void Reload() => Load();

    public static bool Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            string temporaryPath = SettingsFilePath + ".tmp";
            File.WriteAllText(temporaryPath, System.Text.Json.JsonSerializer.Serialize(Current, JsonOptions));
            File.Move(temporaryPath, SettingsFilePath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void EnsureSettingsFile()
    {
        if (!File.Exists(SettingsFilePath))
        {
            Save();
        }
    }
}
