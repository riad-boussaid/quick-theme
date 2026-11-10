using System.Drawing;
using System.Runtime.InteropServices;

namespace QuickTheme.Services;

internal static class AccentColorService
{
    private const string DwmPath = @"Software\Microsoft\Windows\DWM";
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ExplorerAccentPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";
    private const string DesktopPath = @"Control Panel\Desktop";
    private const string ThemesHistoryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\History";

    private const string AccentColorValue = "AccentColor";
    private const string AccentColorMenuValue = "AccentColorMenu";
    private const string StartColorMenuValue = "StartColorMenu";
    private const string AccentPaletteValue = "AccentPalette";
    private const string ColorPrevalenceValue = "ColorPrevalence";
    private const string ColorizationColorValue = "ColorizationColor";
    private const string ColorizationAfterglowValue = "ColorizationAfterglow";
    private const string AutoColorizationValue = "AutoColorization";
    private const string AutoColorValue = "AutoColor";

    private const int ColorizationAlpha = 0xC4;
    private const int UnusedPaletteDefault = 0xF7630C;

    // shell32's UpdateWallpaperTransition(14, 0xFFFFFFFF) posts this request.
    // It makes Explorer recalculate the wallpaper color for Automatic accents.
    private const uint WmUpdateWallpaperTransition = 0x052C;
    private const int WallpaperTransitionRefresh = 14;
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint SmtoErrorOnExit = 0x0020;

    private static Color? _lastManualAccent;
    private static int _manualAccentVersion;
    private static readonly IRegistryStore DefaultRegistry = new WindowsRegistryStore();
    [ThreadStatic] private static IRegistryStore? _testRegistry;

    private static IRegistryStore Registry => _testRegistry ?? DefaultRegistry;

    internal static IDisposable UseRegistryForTests(IRegistryStore store)
    {
        IRegistryStore? previous = _testRegistry;
        _testRegistry = store;
        return new RegistryScope(previous);
    }

    public static Color CurrentAccent
    {
        get
        {
            if (ReadDword(DwmPath, AccentColorValue) is int abgr)
            {
                return FromAbgr(abgr);
            }

            return Color.FromArgb(255, 76, 194, 255);
        }
    }

    public static bool AutomaticAccent
    {
        get => ReadDword(DesktopPath, AutoColorizationValue, 0) != 0;
    }

    public static bool AccentOnTaskbar
    {
        get => ReadDword(PersonalizePath, ColorPrevalenceValue, 0) != 0;
        set => WriteDword(PersonalizePath, ColorPrevalenceValue, value ? 1 : 0);
    }

    public static bool AccentOnTitleBars
    {
        get => ReadDword(DwmPath, ColorPrevalenceValue, 0) != 0;
        set => WriteDword(DwmPath, ColorPrevalenceValue, value ? 1 : 0);
    }

    public static void Apply(Color accent, bool accentOnTaskbar, bool accentOnTitleBars)
    {
        _lastManualAccent = accent;
        _manualAccentVersion++;

        ApplyToRegistry(accent, accentOnTaskbar, accentOnTitleBars);

        ThemeService.RefreshAccentColor();
        SystemThemeNotifier.NotifyAccentChanged();
    }

    internal static void ApplyToRegistry(Color accent, bool accentOnTaskbar, bool accentOnTitleBars)
    {
        // These are the same registry values Windows Settings writes when a
        // manual accent color is chosen.
        WriteDword(DesktopPath, AutoColorizationValue, 0);
        WriteDword(ThemesHistoryPath, AutoColorValue, 0);

        Color[] palette = AccentPaletteBuilder.Build(accent);
        Color unused = ReadUnusedPaletteColor();

        WriteDword(DwmPath, AccentColorValue, ToAbgr(accent));
        WriteDword(DwmPath, ColorizationColorValue, ToColorizationArgb(accent));
        WriteDword(DwmPath, ColorizationAfterglowValue, ToColorizationArgb(accent));
        WriteDword(DwmPath, ColorPrevalenceValue, accentOnTitleBars ? 1 : 0);

        WriteDword(ExplorerAccentPath, AccentColorMenuValue, ToAbgr(accent));
        WriteDword(ExplorerAccentPath, StartColorMenuValue, ToAbgr(palette[4]));
        WriteBinary(ExplorerAccentPath, AccentPaletteValue, BuildPaletteBytes(palette, unused));

        WriteDword(PersonalizePath, ColorPrevalenceValue, accentOnTaskbar ? 1 : 0);
    }

    public static async Task<bool> EnableAutomaticAccentAsync()
    {
        Color previousAccent = CurrentAccent;
        PrepareAutomaticAccent();

        int manualVersion = _manualAccentVersion;
        bool refreshed = await Task.Run(TryRequestShellColorRefresh);

        if (refreshed && AutomaticAccent)
        {
            SyncAccentFromColorization();
            ThemeService.RefreshAccentColor();
        }
        else if (!AutomaticAccent)
        {
            // Automatic was turned off while the shell request was running.
            // Keep the color that was active before, or the manual color the
            // user picked in the meantime.
            Color fallback = _manualAccentVersion != manualVersion &&
                             _lastManualAccent is Color manual
                ? manual
                : previousAccent;

            Apply(fallback, AccentOnTaskbar, AccentOnTitleBars);
        }

        SystemThemeNotifier.NotifyAccentChanged();
        return refreshed;
    }

    internal static void PrepareAutomaticAccent()
    {
        WriteDword(DesktopPath, AutoColorizationValue, 1);
        DeleteValue(ThemesHistoryPath, AutoColorValue);
    }

    public static void DisableAutomaticAccent()
    {
        DisableAutomaticAccentInRegistry();
        SystemThemeNotifier.NotifyAccentChanged();
    }

    internal static void DisableAutomaticAccentInRegistry()
    {
        WriteDword(DesktopPath, AutoColorizationValue, 0);
        WriteDword(ThemesHistoryPath, AutoColorValue, 0);
    }

    // DWM and Explorer\Accent store colors as 0xAABBGGRR.
    internal static int ToAbgr(Color color) =>
        (255 << 24) | (color.B << 16) | (color.G << 8) | color.R;

    internal static Color FromAbgr(int value) =>
        Color.FromArgb(255, value & 0xFF, (value >> 8) & 0xFF, (value >> 16) & 0xFF);

    // ColorizationColor/ColorizationAfterglow use 0xAARRGGBB with the standard
    // glass alpha Windows writes for accent colors.
    private static int ToColorizationArgb(Color color) =>
        (ColorizationAlpha << 24) | (color.R << 16) | (color.G << 8) | color.B;

    private static byte[] BuildPaletteBytes(Color[] palette, Color unused)
    {
        byte[] bytes = new byte[32];

        for (int index = 0; index < 7; index++)
        {
            int offset = index * 4;
            bytes[offset] = palette[index].R;
            bytes[offset + 1] = palette[index].G;
            bytes[offset + 2] = palette[index].B;
            bytes[offset + 3] = 255;
        }

        bytes[28] = unused.R;
        bytes[29] = unused.G;
        bytes[30] = unused.B;
        bytes[31] = 255;

        return bytes;
    }

    private static Color ReadUnusedPaletteColor()
    {
        byte[]? bytes = ReadBinary(ExplorerAccentPath, AccentPaletteValue);
        if (bytes is { Length: >= 32 })
        {
            return Color.FromArgb(255, bytes[28], bytes[29], bytes[30]);
        }

        return FromRgb24(UnusedPaletteDefault);
    }

    private static int ReadDword(string path, string name, int fallback) =>
        Registry.ReadDword(path, name) ?? fallback;

    private static int? ReadDword(string path, string name) =>
        Registry.ReadDword(path, name);

    private static void WriteDword(string path, string name, int value) =>
        Registry.WriteDword(path, name, value);

    private static void DeleteValue(string path, string name) =>
        Registry.DeleteValue(path, name);

    private static byte[]? ReadBinary(string path, string name) =>
        Registry.ReadBinary(path, name);

    private static void WriteBinary(string path, string name, byte[] value) =>
        Registry.WriteBinary(path, name, value);

    internal static void SyncAccentFromColorization()
    {
        if (ReadDword(DwmPath, ColorizationColorValue) is not int colorization)
        {
            return;
        }

        Color accent = Color.FromArgb(
            255,
            (colorization >> 16) & 0xFF,
            (colorization >> 8) & 0xFF,
            colorization & 0xFF);

        WriteDword(DwmPath, AccentColorValue, ToAbgr(accent));
        WriteDword(ExplorerAccentPath, AccentColorMenuValue, ToAbgr(accent));
    }

    private static bool TryRequestShellColorRefresh()
    {
        IntPtr shell = GetShellWindow();
        if (shell == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            IntPtr sent = SendMessageTimeout(
                shell,
                WmUpdateWallpaperTransition,
                new IntPtr(WallpaperTransitionRefresh),
                new IntPtr(0xFFFFFFFFL),
                SmtoAbortIfHung | SmtoErrorOnExit,
                5000,
                out IntPtr result);

            return sent != IntPtr.Zero && result == IntPtr.Zero;
        }
        catch (Exception)
        {
            // Best effort; Windows will update the automatic color on the next
            // wallpaper change or sign-in.
            return false;
        }
    }

    private static Color FromRgb24(int rgb) =>
        Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    private sealed class RegistryScope : IDisposable
    {
        private readonly IRegistryStore? _previous;
        private bool _disposed;

        public RegistryScope(IRegistryStore? previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _testRegistry = _previous;
        }
    }
}
