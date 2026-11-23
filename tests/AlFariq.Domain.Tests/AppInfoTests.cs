using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class AppInfoTests
{
    [Theory]
    [InlineData("0.1.0+3f2a9c1", "0.1.0")]
    [InlineData("1.2.3-beta.1+abc", "1.2.3-beta.1")]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("", "0.0.0")]
    [InlineData("   ", "0.0.0")]
    [InlineData(null, "0.0.0")]
    public void StripBuildMetadata_removes_the_build_suffix(string? input, string expected)
    {
        Assert.Equal(expected, AppInfo.StripBuildMetadata(input));
    }

    [Fact]
    public void Name_is_AlFariq()
    {
        Assert.Equal("AlFariq", AppInfo.Name);
    }

    [Fact]
    public void Version_is_never_empty_and_has_no_build_metadata()
    {
        Assert.False(string.IsNullOrWhiteSpace(AppInfo.Version));
        Assert.DoesNotContain('+', AppInfo.Version);
    }
}
