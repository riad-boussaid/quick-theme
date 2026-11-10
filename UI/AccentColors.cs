using System.Drawing;

namespace QuickTheme.UI;

internal static class AccentColors
{
    public static readonly (string Name, Color Color)[] Standard =
    [
        ("Yellow", Color.FromArgb(255, 185, 0)),
        ("Orange", Color.FromArgb(247, 99, 12)),
        ("Red", Color.FromArgb(232, 17, 35)),
        ("Magenta", Color.FromArgb(227, 0, 140)),
        ("Plum", Color.FromArgb(191, 0, 119)),
        ("Purple", Color.FromArgb(136, 23, 152)),
        ("Violet", Color.FromArgb(135, 100, 184)),
        ("Blue", Color.FromArgb(0, 120, 212)),
        ("Teal", Color.FromArgb(0, 183, 195)),
        ("Green", Color.FromArgb(0, 204, 106)),
        ("Forest", Color.FromArgb(16, 124, 16)),
        ("Gray", Color.FromArgb(118, 118, 118)),
        ("Brown", Color.FromArgb(142, 86, 46)),
        ("Slate", Color.FromArgb(105, 121, 126)),
        ("Charcoal", Color.FromArgb(76, 74, 72)),
        ("Amber", Color.FromArgb(255, 140, 0)),
    ];

    public static readonly (string Name, Color Color)[] Quick =
    [
        ("Blue", Color.FromArgb(0, 120, 212)),
        ("Purple", Color.FromArgb(136, 23, 152)),
        ("Pink", Color.FromArgb(227, 0, 140)),
        ("Red", Color.FromArgb(232, 17, 35)),
        ("Orange", Color.FromArgb(247, 99, 12)),
        ("Yellow", Color.FromArgb(255, 185, 0)),
        ("Green", Color.FromArgb(16, 124, 16)),
        ("Teal", Color.FromArgb(0, 183, 195)),
    ];
}
