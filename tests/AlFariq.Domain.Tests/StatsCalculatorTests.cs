namespace AlFariq.Domain.Tests;

public class StatsCalculatorTests
{
    private static readonly Player Alice = new() { Name = "Alice" };
    private static readonly Player Karim = new() { Name = "Karim" };
    private static readonly Player Sami = new() { Name = "Sami" };

    private static Appearance On(Player player, TeamSide side, int goals = 0, int assists = 0) =>
        new(player.Id, side, goals, assists);

    private static FinishedMatch Played(int scoreA, int scoreB, params Appearance[] appearances) =>
        PlayedOn(new DateOnly(2026, 11, 20), scoreA, scoreB, appearances);

    private static FinishedMatch PlayedOn(DateOnly day, int scoreA, int scoreB, params Appearance[] appearances) =>
        new(
            new Match
            {
                StartsAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(20, 0)), TimeSpan.Zero),
                Status = MatchStatus.Finished,
            },
            new MatchScore(scoreA, scoreB),
            appearances);

    private static StatsSummary Compute(params FinishedMatch[] matches) =>
        StatsCalculator.Compute(matches, new StatsPeriod(), TimeZoneInfo.Utc);

    [Fact]
    public void Wins_draws_losses_goals_and_assists_add_up()
    {
        var summary = Compute(
            Played(3, 1, On(Sami, TeamSide.A, goals: 2, assists: 1), On(Alice, TeamSide.B, goals: 1)),
            Played(0, 2, On(Sami, TeamSide.A), On(Alice, TeamSide.B)),
            Played(2, 2, On(Sami, TeamSide.B, goals: 1), On(Alice, TeamSide.A)));

        Assert.Equal(3, summary.Matches);
        Assert.Equal(new PlayerStats(Sami.Id, 3, 1, 1, 1, 3, 1), summary.For(Sami.Id));
        Assert.Equal(new PlayerStats(Alice.Id, 3, 1, 1, 1, 1, 0), summary.For(Alice.Id));
    }

    [Theory]
    [InlineData(MatchStatus.Planned)]
    [InlineData(MatchStatus.Cancelled)]
    public void Only_finished_matches_count(MatchStatus status)
    {
        var match = Played(1, 0, On(Sami, TeamSide.A, goals: 1));
        match.Match.Status = status;

        var summary = Compute(match);

        Assert.Equal(0, summary.Matches);
        Assert.Empty(summary.Players);
    }

    [Fact]
    public void A_match_without_teams_is_reported_apart_and_does_not_lower_attendance()
    {
        var summary = Compute(Played(1, 0, On(Sami, TeamSide.A)), Played(4, 4));

        Assert.Equal(1, summary.Matches);
        Assert.Equal(1, summary.MatchesWithoutTeams);
        Assert.Equal(100, summary.AttendancePercent(Sami.Id));
    }

    [Fact]
    public void Attendance_is_the_share_of_the_matches_played()
    {
        var summary = Compute(
            Played(1, 0, On(Sami, TeamSide.A), On(Alice, TeamSide.B)),
            Played(1, 0, On(Sami, TeamSide.A)),
            Played(1, 0, On(Sami, TeamSide.A)));

        Assert.Equal(33, summary.AttendancePercent(Alice.Id));
        Assert.Equal(0, summary.AttendancePercent(Karim.Id));
        Assert.Equal(new PlayerStats(Karim.Id), summary.For(Karim.Id));
    }

    [Fact]
    public void Without_a_match_there_is_no_attendance()
    {
        Assert.Null(Compute().AttendancePercent(Sami.Id));
    }

    [Fact]
    public void The_period_keeps_both_of_its_ends()
    {
        FinishedMatch[] matches =
        [
            PlayedOn(new DateOnly(2026, 9, 30), 1, 0, On(Sami, TeamSide.A, goals: 1)),
            PlayedOn(new DateOnly(2026, 10, 1), 1, 0, On(Sami, TeamSide.A, goals: 2)),
            PlayedOn(new DateOnly(2026, 10, 31), 1, 0, On(Sami, TeamSide.A, goals: 4)),
            PlayedOn(new DateOnly(2026, 11, 1), 1, 0, On(Sami, TeamSide.A, goals: 8)),
        ];
        var october = new StatsPeriod(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        var summary = StatsCalculator.Compute(matches, october, TimeZoneInfo.Utc);

        Assert.Equal(2, summary.Matches);
        Assert.Equal(6, summary.For(Sami.Id).Goals);
    }

    [Fact]
    public void The_period_uses_the_date_displayed_in_the_time_zone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        var match = Played(1, 0, On(Sami, TeamSide.A));
        // Le 30 septembre à 23 h 30 UTC, on est déjà en octobre à Paris.
        match.Match.StartsAt = new DateTimeOffset(2026, 9, 30, 23, 30, 0, TimeSpan.Zero);
        var october = new StatsPeriod(From: new DateOnly(2026, 10, 1));

        Assert.Equal(1, StatsCalculator.Compute([match], october, zone).Matches);
        Assert.Equal(0, StatsCalculator.Compute([match], october, TimeZoneInfo.Utc).Matches);
    }

    private static IEnumerable<(int Rank, string Name)> Places(IEnumerable<RankedPlayer> ranking) =>
        ranking.Select(entry => (entry.Rank, entry.Player.Name));

    [Fact]
    public void Players_are_ranked_by_wins_or_by_goals()
    {
        var summary = Compute(
            Played(2, 3, On(Sami, TeamSide.A), On(Alice, TeamSide.B, goals: 3)),
            Played(1, 0, On(Sami, TeamSide.A, goals: 1), On(Alice, TeamSide.B)),
            Played(2, 0, On(Sami, TeamSide.A), On(Alice, TeamSide.B)));
        Player[] players = [Alice, Karim, Sami];

        var byWins = StatsCalculator.Rank(summary, players, RankingCriterion.Wins);
        var byGoals = StatsCalculator.Rank(summary, players, RankingCriterion.Goals);

        // Karim n'a pas joué : il n'est pas classé.
        Assert.Equal(new[] { (1, "Sami"), (2, "Alice") }, Places(byWins));
        Assert.Equal(new[] { (1, "Alice"), (2, "Sami") }, Places(byGoals));
    }

    [Fact]
    public void With_equal_wins_fewer_matches_rank_higher_and_equals_share_a_place()
    {
        var summary = Compute(
            Played(1, 0, On(Sami, TeamSide.A), On(Karim, TeamSide.A), On(Alice, TeamSide.A)),
            Played(0, 1, On(Alice, TeamSide.A)));

        var ranking = StatsCalculator.Rank(summary, [Sami, Karim, Alice], RankingCriterion.Wins);

        Assert.Equal(new[] { (1, "Karim"), (1, "Sami"), (3, "Alice") }, Places(ranking));
    }
}
