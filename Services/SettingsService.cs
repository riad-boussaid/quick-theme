using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuickTheme.Models;

namespace QuickTheme.Services;

internal static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
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

            Current = Deserialize(File.ReadAllText(SettingsFilePath));
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
            File.WriteAllText(temporaryPath, Serialize(Current));
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

    internal static string Serialize(AppSettings settings) =>
        JsonSerializer.Serialize(settings, JsonOptions);

    internal static AppSettings Deserialize(string json) =>
        JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
}
