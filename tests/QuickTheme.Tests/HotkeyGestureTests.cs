using QuickTheme.Models;

namespace QuickTheme.Tests;

public sealed class HotkeyGestureTests
{
    [Fact]
    public void ParsesAndFormatsControlAltShortcut()
    {
        Assert.True(HotkeyGesture.TryParse("Ctrl+Alt+D", out HotkeyGesture gesture));
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, gesture.Modifiers);
        Assert.Equal(Keys.D, gesture.Key);
        Assert.Equal("Ctrl+Alt+D", gesture.ToString());
    }

    [Fact]
    public void ParsesCaseInsensitiveShiftShortcut()
    {
        Assert.True(HotkeyGesture.TryParse("shift+f5", out HotkeyGesture gesture));
        Assert.Equal(HotkeyModifiers.Shift, gesture.Modifiers);
        Assert.Equal(Keys.F5, gesture.Key);
        Assert.Equal("Shift+F5", gesture.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("D")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+D+")]
    [InlineData("Meta+D")]
    [InlineData("Ctrl+NotAKey")]
    public void RejectsInvalidShortcuts(string text)
    {
        Assert.False(HotkeyGesture.TryParse(text, out _));
    }
}
