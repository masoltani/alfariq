using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public sealed class ResultServiceTests : IDisposable
{
    // L'horloge des tests est au 25 novembre 2026 à 10 h (UTC).
    private static readonly MatchInput Evening = new(new DateOnly(2026, 12, 2), new TimeOnly(20, 0), 5);

    private readonly FakeClock _clock = new();
    private readonly ServiceProvider _app;
    private readonly ResultService _results;

    public ResultServiceTests()
    {
        _app = MatchTestHost.Create(_clock);
        _results = _app.GetRequiredService<ResultService>();
    }

    public void Dispose() => _app.Dispose();

    private MatchService Matches => _app.GetRequiredService<MatchService>();

    private LineupService Lineups => _app.GetRequiredService<LineupService>();

    private SquadService Squads => _app.GetRequiredService<SquadService>();

    private IRepository<MatchEvent> Events => _app.GetRequiredService<IRepository<MatchEvent>>();

    private async Task<Guid> NewMatchAsync(MatchInput? input = null) => (await Matches.CreateAsync(input ?? Evening)).Id;

    private async Task<Guid> JoinAsync(Guid match, string name, TeamSide? side)
    {
        var id = (await Squads.AddNewPlayerAsync(match, name)).Id;
        await Lineups.AssignAsync(match, id, side);
        return id;
    }

    private async Task<MatchSheet> SheetAsync(Guid match) => (await _results.GetAsync(match))!;

    [Fact]
    public async Task A_planned_match_has_no_score()
    {
        var match = await NewMatchAsync();

        var sheet = await SheetAsync(match);

        Assert.Equal(MatchStatus.Planned, sheet.Match.Status);
        Assert.Null(sheet.Score);
        Assert.Empty(sheet.Players);
    }

    [Theory]
    [InlineData(3, 2)]
    [InlineData(0, 0)]
    public async Task Validating_a_score_finishes_the_match_without_requiring_scorers(int a, int b)
    {
        var match = await NewMatchAsync();

        await _results.SaveAsync(match, new ResultInput(a, b));
        var sheet = await SheetAsync(match);

        Assert.Equal(MatchStatus.Finished, sheet.Match.Status);
        Assert.Equal(new MatchScore(a, b), sheet.Score);
        Assert.Empty(sheet.Players);
    }

    [Fact]
    public async Task Scorers_and_assists_are_recorded_for_each_player()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami", TeamSide.A);
        var alice = await JoinAsync(match, "Alice", TeamSide.B);

        await _results.SaveAsync(
            match, new ResultInput(3, 1, [new PlayerTally(sami, Goals: 2, Assists: 1), new PlayerTally(alice, 1)]));
        var sheet = await SheetAsync(match);

        Assert.Equal(new PlayerTally(sami, 2, 1), sheet.Players[sami]);
        Assert.Equal(new PlayerTally(alice, 1, 0), sheet.Players[alice]);
        Assert.Equal(2, sheet.PlayerGoals(TeamSide.A));
        Assert.True(sheet.Score!.DiffersFrom(TeamSide.A, sheet.PlayerGoals(TeamSide.A)));
        Assert.False(sheet.Score.DiffersFrom(TeamSide.B, sheet.PlayerGoals(TeamSide.B)));
        Assert.Equal(4, (await Events.ListAsync()).Count);
    }

    [Fact]
    public async Task A_result_stays_editable_and_dates_its_last_change()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami", TeamSide.A);
        var alice = await JoinAsync(match, "Alice", TeamSide.A);
        await _results.SaveAsync(match, new ResultInput(3, 0, [new PlayerTally(sami, 3, 1)]));
        var first = (await SheetAsync(match)).Match.UpdatedAt;
        _clock.Advance(TimeSpan.FromHours(1));

        await _results.SaveAsync(match, new ResultInput(2, 2, [new PlayerTally(sami, 1), new PlayerTally(alice, 1)]));
        var sheet = await SheetAsync(match);

        Assert.Equal(new MatchScore(2, 2), sheet.Score);
        Assert.Equal(new PlayerTally(sami, 1, 0), sheet.Players[sami]);
        Assert.Equal(new PlayerTally(alice, 1, 0), sheet.Players[alice]);
        Assert.Equal(2, (await Events.ListAsync()).Count);
        Assert.True(sheet.Match.UpdatedAt > first);
    }

    [Fact]
    public async Task A_player_left_out_of_a_correction_loses_their_goals()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami", TeamSide.A);
        await _results.SaveAsync(match, new ResultInput(1, 0, [new PlayerTally(sami, 1)]));

        await _results.SaveAsync(match, new ResultInput(1, 0));

        Assert.Empty((await SheetAsync(match)).Players);
        Assert.Empty(await Events.ListAsync());
    }

    [Fact]
    public async Task Saving_a_score_keeps_the_team_settings()
    {
        var match = await NewMatchAsync();
        await Lineups.SetTeamAsync(match, TeamSide.A, "Les Verts", "green");

        await _results.SaveAsync(match, new ResultInput(4, 1));
        await Lineups.SetTeamAsync(match, TeamSide.B, "Les Jaunes", "yellow");
        var sheet = await SheetAsync(match);

        Assert.Equal("Les Verts", sheet.Lineup.TeamA.Name);
        Assert.Equal("green", sheet.Lineup.TeamA.ColorCode);
        Assert.Equal("Les Jaunes", sheet.Lineup.TeamB.Name);
        Assert.Equal(new MatchScore(4, 1), sheet.Score);
    }

    [Fact]
    public async Task A_scorer_must_play_in_a_team_otherwise_nothing_is_saved()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami", side: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _results.SaveAsync(match, new ResultInput(1, 0, [new PlayerTally(sami, 1)])));

        Assert.Equal(MatchStatus.Planned, (await SheetAsync(match)).Match.Status);
        Assert.Empty(await Events.ListAsync());
    }

    [Fact]
    public async Task Goals_of_a_player_removed_from_the_teams_no_longer_count_and_come_back_with_them()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami", TeamSide.A);
        await _results.SaveAsync(match, new ResultInput(2, 0, [new PlayerTally(sami, 2)]));

        await Squads.SetSelectedAsync(match, sami, selected: false);
        await _results.SaveAsync(match, new ResultInput(2, 1));

        Assert.Empty((await SheetAsync(match)).Players);

        await Squads.SetSelectedAsync(match, sami, selected: true);
        await Lineups.AssignAsync(match, sami, TeamSide.B);

        Assert.Equal(2, (await SheetAsync(match)).PlayerGoals(TeamSide.B));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, MatchSide.MaxScore + 1)]
    public async Task A_score_stays_within_its_limits(int a, int b)
    {
        var match = await NewMatchAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _results.SaveAsync(match, new ResultInput(a, b)));
    }

    [Fact]
    public async Task Only_sports_counting_goals_have_scorers()
    {
        var match = await NewMatchAsync(Evening with { SportCode = "basketball" });
        var sami = await JoinAsync(match, "Sami", TeamSide.A);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _results.SaveAsync(match, new ResultInput(80, 72, [new PlayerTally(sami, 10)])));

        // Des joueurs sans but ni passe ne gênent pas : l'écran les envoie tous.
        await _results.SaveAsync(match, new ResultInput(80, 72, [new PlayerTally(sami)]));

        Assert.Equal(new MatchScore(80, 72), (await SheetAsync(match)).Score);
    }

    [Theory]
    [InlineData("  Pluie  ", "Pluie")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public async Task A_match_is_cancelled_with_an_optional_reason(string? typed, string? expected)
    {
        var match = await NewMatchAsync();

        await Matches.CancelAsync(match, typed);
        var cancelled = (await Matches.FindAsync(match))!;

        Assert.Equal(MatchStatus.Cancelled, cancelled.Status);
        Assert.Equal(expected, cancelled.CancellationReason);
    }

    [Fact]
    public async Task A_cancelled_match_has_no_result_until_it_is_reinstated()
    {
        var match = await NewMatchAsync();
        await Matches.CancelAsync(match, "Pluie");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _results.SaveAsync(match, new ResultInput(1, 0)));

        var reinstated = await Matches.ReinstateAsync(match);

        Assert.Equal(MatchStatus.Planned, reinstated.Status);
        Assert.Null(reinstated.CancellationReason);
        await _results.SaveAsync(match, new ResultInput(1, 0));
    }

    [Fact]
    public async Task A_finished_match_cannot_be_cancelled_and_a_reason_has_a_limit()
    {
        var match = await NewMatchAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => Matches.CancelAsync(match, new string('x', Match.MaxCancellationReasonLength + 1)));

        await _results.SaveAsync(match, new ResultInput(1, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Matches.CancelAsync(match));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Matches.ReinstateAsync(match));
    }

    [Fact]
    public async Task Clearing_a_result_makes_the_match_planned_again()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami", TeamSide.A);
        await _results.SaveAsync(match, new ResultInput(1, 0, [new PlayerTally(sami, 1)]));

        await _results.ClearAsync(match);
        var sheet = await SheetAsync(match);

        Assert.Equal(MatchStatus.Planned, sheet.Match.Status);
        Assert.Null(sheet.Score);
        Assert.Empty(await Events.ListAsync());
        var sides = await _app.GetRequiredService<IRepository<MatchSide>>().ListAsync();
        Assert.All(sides, side => Assert.Null(side.Score));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _results.ClearAsync(match));
    }

    [Fact]
    public async Task A_finished_match_keeps_its_sport_and_format_but_stays_editable()
    {
        var match = await NewMatchAsync();
        await _results.SaveAsync(match, new ResultInput(1, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Matches.UpdateAsync(match, Evening with { PlayersPerSide = 7 }));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Matches.UpdateAsync(match, Evening with { SportCode = "futsal" }));

        var updated = await Matches.UpdateAsync(match, Evening with { Location = "Stade" });

        Assert.Equal("Stade", updated.Location);
        Assert.Equal(MatchStatus.Finished, updated.Status);
    }

    [Fact]
    public async Task Finished_and_cancelled_matches_are_not_the_next_match()
    {
        var finished = await NewMatchAsync();
        var cancelled = await NewMatchAsync(Evening with { Date = Evening.Date.AddDays(1) });
        var planned = await NewMatchAsync(Evening with { Date = Evening.Date.AddDays(2) });
        await _results.SaveAsync(finished, new ResultInput(1, 0));
        await Matches.CancelAsync(cancelled);

        var next = await _app.GetRequiredService<IUpcomingMatchQuery>().GetNextAsync();

        Assert.Equal(planned, next!.Id);
    }

    [Fact]
    public async Task Duplicating_a_finished_match_gives_a_planned_match_without_result()
    {
        var source = await NewMatchAsync();
        var sami = await JoinAsync(source, "Sami", TeamSide.A);
        await _results.SaveAsync(source, new ResultInput(1, 0, [new PlayerTally(sami, 1)]));
        var input = (await Matches.GetDuplicateInputAsync(source))!;

        var copy = await Matches.CreateAsync(input, duplicatedFrom: source);
        var sheet = await SheetAsync(copy.Id);

        Assert.Equal(MatchStatus.Planned, sheet.Match.Status);
        Assert.Null(sheet.Score);
        Assert.Empty(sheet.Players);
    }
}
