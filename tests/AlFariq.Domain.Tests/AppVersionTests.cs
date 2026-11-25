using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("0.1.0", 0, 1, 0, null)]
    [InlineData("1.22.333", 1, 22, 333, null)]
    [InlineData("1.0.0-beta.1", 1, 0, 0, "beta.1")]
    [InlineData("2.3.4+abc123", 2, 3, 4, null)]
    [InlineData("2.3.4-rc.1+abc123", 2, 3, 4, "rc.1")]
    [InlineData("  0.3.0  ", 0, 3, 0, null)]
    public void TryParse_reads_valid_versions(string text, int major, int minor, int patch, string? prerelease)
    {
        Assert.True(AppVersion.TryParse(text, out var version));
        Assert.Equal(new AppVersion(major, minor, patch, prerelease), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("01.2.3")]
    [InlineData("1.2.x")]
    [InlineData("v1.2.3")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3-beta..1")]
    [InlineData("99999999999.0.0")]
    public void TryParse_rejects_invalid_versions(string? text)
    {
        Assert.False(AppVersion.TryParse(text, out _));
    }

    [Fact]
    public void Parse_throws_a_clear_error_on_invalid_text()
    {
        var error = Assert.Throws<FormatException>(() => AppVersion.Parse("abc"));
        Assert.Contains("abc", error.Message);
    }

    [Fact]
    public void ToString_round_trips()
    {
        Assert.Equal("1.2.3", new AppVersion(1, 2, 3).ToString());
        Assert.Equal("1.2.3-beta.2", AppVersion.Parse("1.2.3-beta.2+build9").ToString());
    }

    [Theory]
    [InlineData("0.2.0", "0.10.0", -1)]      // comparaison numérique, pas lexicale
    [InlineData("1.0.0", "0.99.99", 1)]
    [InlineData("1.2.3", "1.2.3", 0)]
    [InlineData("1.2.3", "1.2.4", -1)]
    [InlineData("1.0.0-beta", "1.0.0", -1)]  // une pré-version précède la version finale
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)]
    [InlineData("1.0.0-beta.2", "1.0.0-beta.11", -1)]
    [InlineData("1.0.0-beta.1", "1.0.0-beta", 1)]
    [InlineData("1.0.0-1", "1.0.0-alpha", -1)] // numérique avant alphanumérique
    [InlineData("1.2.3+a", "1.2.3+b", 0)]      // les métadonnées de build sont ignorées
    public void CompareTo_follows_semver_rules(string left, string right, int expectedSign)
    {
        var a = AppVersion.Parse(left);
        var b = AppVersion.Parse(right);

        Assert.Equal(expectedSign, Math.Sign(a.CompareTo(b)));
        Assert.Equal(-expectedSign, Math.Sign(b.CompareTo(a)));
        Assert.Equal(expectedSign < 0, a < b);
        Assert.Equal(expectedSign > 0, a > b);
        Assert.Equal(expectedSign <= 0, a <= b);
        Assert.Equal(expectedSign >= 0, a >= b);
    }

    [Fact]
    public void AppInfo_exposes_a_parsed_version_matching_the_text_version()
    {
        Assert.Equal(AppInfo.Version, AppInfo.SemanticVersion.ToString());
    }
}
