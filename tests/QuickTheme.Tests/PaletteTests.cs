using System.Drawing;
using QuickTheme.Models;
using QuickTheme.UI;

namespace QuickTheme.Tests;

[Collection("WinForms")]
public sealed class PaletteTests
{
    [Fact]
    public void HighContrast_UsesSystemColors()
    {
        try
        {
            Palette.Apply(FlyoutThemeMode.Dark, systemDark: true, highContrast: true);

            Assert.True(Palette.IsHighContrast);
            Assert.Equal(SystemColors.Window, Palette.SurfaceTop);
            Assert.Equal(SystemColors.WindowText, Palette.Text);
            Assert.Equal(SystemColors.Highlight, Palette.Accent);
            Assert.Equal(SystemColors.HighlightText, Palette.ToggleThumb);
        }
        finally
        {
            Palette.Apply(FlyoutThemeMode.System, systemDark: false, highContrast: false);
        }
    }

    [Fact]
    public void NormalMode_SelectsLightAndDark()
    {
        try
        {
            Palette.Apply(FlyoutThemeMode.Light, systemDark: true, highContrast: false);

            Assert.True(Palette.IsLight);
            Assert.False(Palette.IsHighContrast);

            Palette.Apply(FlyoutThemeMode.Dark, systemDark: false, highContrast: false);

            Assert.False(Palette.IsLight);
        }
        finally
        {
            Palette.Apply(FlyoutThemeMode.System, systemDark: false, highContrast: false);
        }
    }
}
