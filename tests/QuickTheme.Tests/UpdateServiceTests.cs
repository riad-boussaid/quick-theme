using QuickTheme.Services;

namespace QuickTheme.Tests;

public sealed class UpdateServiceTests
{
    [Theory]
    [InlineData("1.2.1", "1.2.0", true)]
    [InlineData("1.3", "1.2.9", true)]
    [InlineData("1.2.0", "1.2.0", false)]
    [InlineData("1.2", "1.2.0", false)]
    [InlineData("1.2.0.1", "1.2.0", true)]
    [InlineData("not-a-version", "1.2.0", false)]
    [InlineData("", "1.2.0", false)]
    public void IsNewerVersion_ComparesNormalizedVersions(string candidate, string current, bool expected)
    {
        bool result = UpdateService.IsNewerVersion(candidate, Version.Parse(current));

        Assert.Equal(expected, result);
    }
}
