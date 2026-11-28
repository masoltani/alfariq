namespace AlFariq.Domain.Tests;

public class MatchSearchTests
{
    private static readonly DateOnly Day = new(2026, 12, 2);

    private static MatchRecord Record(DateTimeOffset startsAt, params string[] players) =>
        new(new Match { StartsAt = startsAt }, players.Select(name => new Player { Name = name }).ToList());

    private static MatchRecord Evening(int day, params string[] players) =>
        Record(new DateTimeOffset(2026, 12, day, 20, 0, 0, TimeSpan.Zero), players);

    private static IReadOnlyList<FoundMatch> Find(MatchCriteria criteria, params MatchRecord[] records) =>
        MatchSearch.Find(records, criteria, TimeZoneInfo.Utc);

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Without_criteria_every_match_is_kept(string? name)
    {
        var criteria = new MatchCriteria(name);

        var found = Find(criteria, Evening(2, "Sami"), Evening(3));

        Assert.True(criteria.IsEmpty);
        Assert.Equal(2, found.Count);
        Assert.All(found, match => Assert.Empty(match.NamedPlayers));
    }

    [Theory]
    [InlineData("elo")]
    [InlineData("ÉLODIE")]
    [InlineData("  lodie ")]
    public void A_match_is_found_by_part_of_a_player_name(string name)
    {
        var played = Evening(2, "Sami", "Élodie");

        var found = Find(new MatchCriteria(name), played, Evening(3, "Sami"), Evening(4));

        var match = Assert.Single(found);
        Assert.Same(played.Match, match.Match);
        Assert.Equal(new[] { "Élodie" }, match.NamedPlayers.Select(player => player.Name));
    }

    [Fact]
    public void Every_player_whose_name_matches_is_cited()
    {
        var found = Find(new MatchCriteria("kar"), Evening(2, "Karima", "Alice", "Karim"));

        Assert.Equal(new[] { "Karim", "Karima" }, Assert.Single(found).NamedPlayers.Select(player => player.Name));
    }

    [Fact]
    public void A_match_is_found_by_its_date()
    {
        var wanted = Evening(2);

        var found = Find(new MatchCriteria(Date: Day), Evening(1), wanted, Evening(3));

        Assert.Same(wanted.Match, Assert.Single(found).Match);
    }

    [Fact]
    public void The_date_is_the_one_displayed_in_the_time_zone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        // Le 1er décembre à 23 h 30 UTC, il est déjà le 2 à Paris.
        var record = Record(new DateTimeOffset(2026, 12, 1, 23, 30, 0, TimeSpan.Zero));

        Assert.Single(MatchSearch.Find([record], new MatchCriteria(Date: Day), zone));
        Assert.Empty(MatchSearch.Find([record], new MatchCriteria(Date: Day.AddDays(-1)), zone));
    }

    [Fact]
    public void Date_and_name_must_both_match()
    {
        var wanted = Evening(2, "Sami");

        var found = Find(new MatchCriteria("sami", Day), wanted, Evening(2, "Alice"), Evening(3, "Sami"));

        Assert.Same(wanted.Match, Assert.Single(found).Match);
    }

    [Fact]
    public void Only_the_statuses_present_are_offered()
    {
        Match[] matches =
        [
            new() { Status = MatchStatus.Cancelled },
            new() { Status = MatchStatus.Planned },
            new() { Status = MatchStatus.Cancelled },
        ];

        Assert.Equal(new[] { MatchStatus.Planned, MatchStatus.Cancelled }, MatchSearch.Statuses(matches));
    }

    [Fact]
    public void Matches_are_filtered_by_status_keeping_their_order()
    {
        var first = new Match { Status = MatchStatus.Finished };
        var planned = new Match { Status = MatchStatus.Planned };
        var second = new Match { Status = MatchStatus.Finished };
        Match[] matches = [first, planned, second];

        Assert.Equal(new[] { first, second }, MatchSearch.WithStatus(matches, MatchStatus.Finished));
        Assert.Equal(matches, MatchSearch.WithStatus(matches, null));
    }
}
