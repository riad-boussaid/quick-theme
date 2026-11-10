using System.Drawing;
using QuickTheme.App;

namespace QuickTheme.Tests;

public sealed class ThemeCommandLineTests
{
    [Theory]
    [InlineData("--dark", "--dark")]
    [InlineData("--light", "--light")]
    [InlineData("--toggle", "--toggle")]
    [InlineData("--DARK", "--DARK")]
    public void FindThemeCommand_FindsThemeSwitch(string argument, string expected)
    {
        string? result = ThemeCommandLine.FindThemeCommand(["--show", argument]);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FindThemeCommand_ReturnsNullForNonThemeArguments()
    {
        Assert.Null(ThemeCommandLine.FindThemeCommand(["--show"]));
        Assert.Null(ThemeCommandLine.FindThemeCommand([]));
    }

    [Theory]
    [InlineData("--show", true)]
    [InlineData("--SHOW", true)]
    [InlineData("--dark", false)]
    public void IsShowArgument_RecognizesShowSwitch(string argument, bool expected)
    {
        Assert.Equal(expected, ThemeCommandLine.IsShowArgument(argument));
    }

    [Theory]
    [InlineData("--accent", "#0078D4")]
    [InlineData("--accent", "0078d4")]
    [InlineData("--accent=#0078D4", null)]
    [InlineData("--accent=0078d4", null)]
    public void FindAccentCommand_FindsManualColor(string first, string? second)
    {
        string[] args = second is null ? [first] : [first, second];

        AccentCommand result = ThemeCommandLine.FindAccentCommand(args);

        Assert.Equal(AccentCommandKind.Manual, result.Kind);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void FindAccentCommand_FindsAutomatic()
    {
        AccentCommand result = ThemeCommandLine.FindAccentCommand(["--accent-auto"]);

        Assert.Equal(AccentCommandKind.Automatic, result.Kind);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("--accent")]
    [InlineData("--accent", "--dark")]
    public void FindAccentCommand_RejectsMissingColor(params string[] args)
    {
        AccentCommand result = ThemeCommandLine.FindAccentCommand(args);

        Assert.Equal(AccentCommandKind.Invalid, result.Kind);
    }

    [Fact]
    public void FindAccentCommand_ReturnsNoneWhenAbsent()
    {
        Assert.Equal(AccentCommandKind.None, ThemeCommandLine.FindAccentCommand(["--show"]).Kind);
        Assert.Equal(AccentCommandKind.None, ThemeCommandLine.FindAccentCommand([]).Kind);
    }

    [Theory]
    [InlineData("#0078D4", 0, 120, 212)]
    [InlineData("0078d4", 0, 120, 212)]
    [InlineData("  #FFB900  ", 255, 185, 0)]
    public void TryParseAccentColor_ParsesHex(string value, int red, int green, int blue)
    {
        bool parsed = ThemeCommandLine.TryParseAccentColor(value, out Color color);

        Assert.True(parsed);
        Assert.Equal(Color.FromArgb(red, green, blue).ToArgb(), color.ToArgb());
    }

    [Theory]
    [InlineData("")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("blue")]
    public void TryParseAccentColor_RejectsInvalidValues(string value)
    {
        Assert.False(ThemeCommandLine.TryParseAccentColor(value, out _));
    }
}
