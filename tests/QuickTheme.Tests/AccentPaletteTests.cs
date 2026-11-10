using System.Drawing;
using QuickTheme.Services;

namespace QuickTheme.Tests;

public sealed class AccentPaletteTests
{
    [Fact]
    public void Build_UsesWindowsPalette_ForStandardBlue()
    {
        Color[] palette = AccentPaletteBuilder.Build(Color.FromArgb(0, 120, 212));

        Assert.Equal(
            ["#99EBFF", "#4CC2FF", "#0091F8", "#0078D4", "#0067C0", "#003E92", "#001A68"],
            palette.Select(ToHex));
    }

    [Fact]
    public void Build_UsesWindowsPalette_ForWindows10Blue()
    {
        Color[] palette = AccentPaletteBuilder.Build(Color.FromArgb(0, 120, 215));

        Assert.Equal(
            ["#A6D8FF", "#76B9ED", "#429CE3", "#0078D7", "#005A9E", "#004275", "#002642"],
            palette.Select(ToHex));
    }

    [Fact]
    public void Build_MatchesWindows_ForCustomPurple()
    {
        Color[] palette = AccentPaletteBuilder.Build(Color.FromArgb(0x72, 0x68, 0xBD));

        Assert.Equal(
            ["#E6DAF3", "#C4B9E4", "#897FC9", "#7369BF", "#5349AC", "#3A3580", "#151358"],
            palette.Select(ToHex));
    }

    [Fact]
    public void Build_HandlesVeryDarkAndGrayColors()
    {
        Color[] black = AccentPaletteBuilder.Build(Color.Black);
        Color[] gray = AccentPaletteBuilder.Build(Color.FromArgb(118, 118, 118));

        Assert.Equal(7, black.Length);
        Assert.Equal(7, gray.Length);
        Assert.All(black, color => Assert.Equal(255, color.A));
        Assert.Equal("#E6E6E6", ToHex(gray[0]));
    }

    [Fact]
    public void Abgr_RoundTrips()
    {
        Color color = Color.FromArgb(18, 52, 86);

        Color roundTripped = AccentColorService.FromAbgr(AccentColorService.ToAbgr(color));

        Assert.Equal(color.ToArgb(), roundTripped.ToArgb());
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
