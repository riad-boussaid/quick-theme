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
}
