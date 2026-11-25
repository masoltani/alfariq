using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class ChangelogTests
{
    private const string Valid = """
        {
          "entries": [
            { "version": "0.2.0", "date": "2026-11-25",
              "changes": [
                { "type": "added", "text": { "fr": "Deux", "en": "Two", "ar": "اثنان" } },
                { "type": "fixed", "text": { "fr": "Correction" } }
              ] },
            { "version": "0.1.0", "date": "2026-11-23",
              "changes": [ { "type": "changed", "text": { "fr": "Un" } } ] }
          ]
        }
        """;

    private static string WithEntry(string entry) => "{ \"entries\": [ " + entry + " ] }";

    // ---------- Analyse ----------

    [Fact]
    public void Parse_reads_entries_changes_types_and_dates()
    {
        var changelog = Changelog.Parse(Valid);

        Assert.Equal(2, changelog.Entries.Count);
        Assert.Equal(new AppVersion(0, 2, 0), changelog.Latest!.Version);
        Assert.Equal(new DateOnly(2026, 11, 25), changelog.Latest.Date);
        Assert.Equal(new[] { ChangeType.Added, ChangeType.Fixed }, changelog.Latest.Changes.Select(c => c.Type).ToArray());
        Assert.Equal(ChangeType.Changed, changelog.Entries[1].Changes[0].Type);
    }

    [Fact]
    public void Latest_is_null_for_an_empty_changelog()
    {
        Assert.Null(Changelog.Parse("""{ "entries": [] }""").Latest);
    }

    [Theory]
    [InlineData("pas du json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{ "entries": {} }""")]
    public void Parse_rejects_a_malformed_document(string json)
    {
        Assert.Throws<FormatException>(() => Changelog.Parse(json));
    }

    [Theory]
    [InlineData("""{ "version": "1.0", "date": "2026-01-01", "changes": [ { "type": "added", "text": { "fr": "x" } } ] }""", "1.0")]
    [InlineData("""{ "version": "1.0.0", "date": "01/01/2026", "changes": [ { "type": "added", "text": { "fr": "x" } } ] }""", "01/01/2026")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-02-30", "changes": [ { "type": "added", "text": { "fr": "x" } } ] }""", "2026-02-30")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01", "changes": [] }""", "au moins un changement")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01" }""", "au moins un changement")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01", "changes": [ { "type": "removed", "text": { "fr": "x" } } ] }""", "removed")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01", "changes": [ { "type": "added" } ] }""", "text")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01", "changes": [ { "type": "added", "text": { "en": "only english" } } ] }""", "français")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01", "changes": [ { "type": "added", "text": { "fr": "x", "es": "x" } } ] }""", "es")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01", "changes": [ { "type": "added", "text": { "fr": "  " } } ] }""", "vide")]
    [InlineData("""{ "version": "1.0.0", "date": "2026-01-01", "changes": [ { "type": "added", "text": { "fr": 12 } } ] }""", "vide")]
    public void Parse_rejects_an_invalid_entry_and_says_why(string entry, string expectedInMessage)
    {
        var error = Assert.Throws<FormatException>(() => Changelog.Parse(WithEntry(entry)));

        Assert.Contains(expectedInMessage, error.Message);
    }

    [Fact]
    public void Parse_rejects_duplicate_versions()
    {
        const string entry = """{ "version": "1.0.0", "date": "2026-01-01", "changes": [ { "type": "added", "text": { "fr": "x" } } ] }""";

        Assert.Throws<FormatException>(() => Changelog.Parse(WithEntry(entry + "," + entry)));
    }

    [Fact]
    public void Parse_rejects_entries_not_sorted_from_newest_to_oldest()
    {
        const string older = """{ "version": "0.1.0", "date": "2026-01-01", "changes": [ { "type": "added", "text": { "fr": "x" } } ] }""";
        const string newer = """{ "version": "0.2.0", "date": "2026-02-01", "changes": [ { "type": "added", "text": { "fr": "x" } } ] }""";

        Assert.Throws<FormatException>(() => Changelog.Parse(WithEntry(older + "," + newer)));
    }

    // ---------- Langues ----------

    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("fr-FR", "fr")]
    [InlineData("EN-us", "en")]
    [InlineData("ar-MA", "ar")]
    [InlineData("es", "fr")]   // langue non prévue : français
    [InlineData("", "fr")]
    [InlineData(null, "fr")]
    public void FromCulture_maps_a_culture_to_a_changelog_language(string? culture, string expected)
    {
        Assert.Equal(expected, ChangelogLanguages.FromCulture(culture));
    }

    [Fact]
    public void Resolve_returns_the_text_in_the_requested_language()
    {
        var text = Changelog.Parse(Valid).Latest!.Changes[0].Text;

        Assert.Equal(new ResolvedText("Deux", "fr"), text.Resolve("fr-FR"));
        Assert.Equal(new ResolvedText("Two", "en"), text.Resolve("en-GB"));
        Assert.Equal("اثنان", text.Resolve("ar").Text);
    }

    [Fact]
    public void Resolve_falls_back_to_french_and_reports_the_language_actually_used()
    {
        var onlyFrench = Changelog.Parse(Valid).Latest!.Changes[1].Text;

        var resolved = onlyFrench.Resolve("ar");

        Assert.Equal("Correction", resolved.Text);
        Assert.Equal("fr", resolved.Language);   // pour lang="fr"
        Assert.Equal("ltr", resolved.Direction); // et dir="ltr", même dans une interface arabe
    }

    [Fact]
    public void Arabic_text_is_right_to_left()
    {
        var resolved = Changelog.Parse(Valid).Latest!.Changes[0].Text.Resolve("ar");

        Assert.Equal("ar", resolved.Language);
        Assert.Equal("rtl", resolved.Direction);
    }

    [Fact]
    public void LocalizedText_requires_french()
    {
        var values = new Dictionary<string, string> { ["en"] = "x" };

        Assert.Throws<ArgumentException>(() => new LocalizedText(values));
    }

    // ---------- Annonce des nouveautés ----------

    [Fact]
    public void EntriesSince_returns_entries_after_the_last_seen_up_to_the_current_version()
    {
        var changelog = Changelog.Parse(Valid);

        var news = changelog.EntriesSince(new AppVersion(0, 1, 0), new AppVersion(0, 2, 0));

        Assert.Equal(new[] { new AppVersion(0, 2, 0) }, news.Select(e => e.Version).ToArray());
    }

    [Fact]
    public void EntriesSince_ignores_versions_newer_than_the_installed_one()
    {
        var changelog = Changelog.Parse(Valid);

        Assert.Empty(changelog.EntriesSince(new AppVersion(0, 0, 1), new AppVersion(0, 0, 9)));
        Assert.Single(changelog.EntriesSince(new AppVersion(0, 0, 1), new AppVersion(0, 1, 0)));
    }

    [Theory]
    [InlineData("0.1.0", "0.2.0", true)]    // mise à jour : annoncer
    [InlineData("0.2.0", "0.2.0", false)]   // déjà vu
    [InlineData("0.2.0", "0.1.0", false)]   // retour en arrière : rien
    [InlineData("0.2.0", "0.2.1", false)]   // pas d'entrée entre les deux : rien à dire
    [InlineData(null, "0.2.0", false)]      // premier lancement : jamais d'annonce
    [InlineData("", "0.2.0", false)]
    [InlineData("n'importe quoi", "0.2.0", false)] // valeur mémorisée illisible
    public void HasNewsSince_announces_only_real_updates(string? lastSeen, string current, bool expected)
    {
        var changelog = Changelog.Parse(Valid);

        Assert.Equal(expected, changelog.HasNewsSince(lastSeen, AppVersion.Parse(current)));
    }
}
