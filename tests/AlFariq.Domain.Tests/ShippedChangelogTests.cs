using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

/// <summary>Contrôles du journal réellement livré (src/AlFariq.Domain/Releases/changelog.json).</summary>
public class ShippedChangelogTests
{
    [Fact]
    public void The_embedded_changelog_is_valid_and_not_empty()
    {
        var changelog = Changelog.LoadEmbedded(); // la validation (versions, dates, français) est faite par Parse

        Assert.NotEmpty(changelog.Entries);
    }

    [Fact]
    public void The_latest_entry_is_the_version_of_the_app()
    {
        // Publier = changer <Version> dans Directory.Build.props ET ajouter l'entrée en tête de changelog.json.
        var changelog = Changelog.LoadEmbedded();

        Assert.Equal(AppInfo.SemanticVersion, changelog.Latest!.Version);
    }

    [Fact]
    public void Every_change_has_a_french_text()
    {
        var changelog = Changelog.LoadEmbedded();

        foreach (var change in changelog.Entries.SelectMany(e => e.Changes))
        {
            Assert.False(string.IsNullOrWhiteSpace(change.Text.Resolve("fr").Text));
        }
    }

    [Fact]
    public void Every_change_is_translated_in_english_and_arabic_for_now()
    {
        // Contrôle volontairement strict tant que le journal est petit : il pourra être assoupli (repli sur le français).
        var changelog = Changelog.LoadEmbedded();

        foreach (var change in changelog.Entries.SelectMany(e => e.Changes))
        {
            Assert.Contains("en", change.Text.Values.Keys);
            Assert.Contains("ar", change.Text.Values.Keys);
        }
    }
}
