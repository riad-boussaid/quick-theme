using System.Drawing;
using System.Windows.Forms;
using QuickTheme.Models;
using QuickTheme.Services;

namespace QuickTheme.UI;

internal sealed record PaletteColors(
    Color SurfaceTop,
    Color SurfaceBottom,
    Color Border,
    Color Separator,
    Color Text,
    Color TextMuted,
    Color TextDisabled,
    Color RowHover,
    Color TrackOff,
    Color MenuSurface,
    Color MenuHover,
    Color Danger,
    bool IsLight);

internal static class Palette
{
    private static readonly PaletteColors Dark = new(
        SurfaceTop: Color.FromArgb(29, 29, 35),
        SurfaceBottom: Color.FromArgb(20, 20, 25),
        Border: Color.FromArgb(48, 48, 58),
        Separator: Color.FromArgb(38, 38, 46),
        Text: Color.FromArgb(242, 242, 247),
        TextMuted: Color.FromArgb(146, 146, 158),
        TextDisabled: Color.FromArgb(96, 96, 108),
        RowHover: Color.FromArgb(38, 38, 47),
        TrackOff: Color.FromArgb(58, 58, 68),
        MenuSurface: Color.FromArgb(26, 26, 32),
        MenuHover: Color.FromArgb(44, 44, 55),
        Danger: Color.FromArgb(255, 130, 130),
        IsLight: false);

    private static readonly PaletteColors Light = new(
        SurfaceTop: Color.FromArgb(251, 251, 253),
        SurfaceBottom: Color.FromArgb(241, 241, 245),
        Border: Color.FromArgb(214, 214, 223),
        Separator: Color.FromArgb(227, 227, 234),
        Text: Color.FromArgb(27, 27, 32),
        TextMuted: Color.FromArgb(102, 102, 114),
        TextDisabled: Color.FromArgb(160, 160, 170),
        RowHover: Color.FromArgb(236, 236, 243),
        TrackOff: Color.FromArgb(199, 199, 209),
        MenuSurface: Color.FromArgb(248, 248, 251),
        MenuHover: Color.FromArgb(231, 231, 239),
        Danger: Color.FromArgb(179, 38, 30),
        IsLight: true);

    private static PaletteColors _current = Dark;
    private static bool _highContrast;

    public static bool IsLight => _current.IsLight;
    public static bool IsHighContrast => _highContrast;
    public static Color SurfaceTop => _current.SurfaceTop;
    public static Color SurfaceBottom => _current.SurfaceBottom;
    public static Color Border => _current.Border;
    public static Color Separator => _current.Separator;
    public static Color Text => _current.Text;
    public static Color TextMuted => _current.TextMuted;
    public static Color TextDisabled => _current.TextDisabled;
    public static Color RowHover => _current.RowHover;
    public static Color TrackOff => _current.TrackOff;
    public static Color MenuSurface => _current.MenuSurface;
    public static Color MenuHover => _current.MenuHover;
    public static Color Danger => _current.Danger;

    public static Color Accent
    {
        get
        {
            if (_highContrast)
            {
                return SystemColors.Highlight;
            }

            Color accent = ThemeService.AccentColor;
            if (!IsLight)
            {
                return accent;
            }

            float brightness = accent.GetBrightness();
            return brightness > 0.62f
                ? ControlPaint.Dark(accent, Math.Min(0.45f, brightness - 0.5f))
                : accent;
        }
    }

    public static Color ToggleThumb =>
        _highContrast ? SystemColors.HighlightText : Color.White;

    public static void Apply(FlyoutThemeMode mode, bool systemDark) =>
        Apply(mode, systemDark, SystemInformation.HighContrast);

    internal static void Apply(FlyoutThemeMode mode, bool systemDark, bool highContrast)
    {
        _highContrast = highContrast;

        if (highContrast)
        {
            _current = CreateHighContrast();
            return;
        }

        bool light = mode == FlyoutThemeMode.Light ||
                     (mode == FlyoutThemeMode.System && !systemDark);

        _current = light ? Light : Dark;
    }

    private static PaletteColors CreateHighContrast() => new(
        SurfaceTop: SystemColors.Window,
        SurfaceBottom: SystemColors.Window,
        Border: SystemColors.WindowFrame,
        Separator: SystemColors.ControlDark,
        Text: SystemColors.WindowText,
        TextMuted: SystemColors.WindowText,
        TextDisabled: SystemColors.GrayText,
        RowHover: SystemColors.Control,
        TrackOff: SystemColors.Control,
        MenuSurface: SystemColors.Menu,
        MenuHover: SystemColors.Control,
        Danger: SystemColors.WindowText,
        IsLight: SystemColors.Window.GetBrightness() > 0.5f);
}
