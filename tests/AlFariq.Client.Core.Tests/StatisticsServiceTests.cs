using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

/// <summary>Statistiques (US-014) calculées à partir de ce qui est réellement enregistré.</summary>
public sealed class StatisticsServiceTests : IDisposable
{
    private static readonly MatchInput Evening = new(new DateOnly(2026, 11, 20), new TimeOnly(20, 0), 5);

    private readonly FakeClock _clock = new();
    private readonly ServiceProvider _app;

    public StatisticsServiceTests() => _app = MatchTestHost.Create(_clock);

    public void Dispose() => _app.Dispose();

    private StatisticsService Statistics => _app.GetRequiredService<StatisticsService>();

    private MatchService Matches => _app.GetRequiredService<MatchService>();

    private SquadService Squads => _app.GetRequiredService<SquadService>();

    private LineupService Lineups => _app.GetRequiredService<LineupService>();

    private ResultService Results => _app.GetRequiredService<ResultService>();

    private async Task<Guid> NewMatchAsync() => (await Matches.CreateAsync(Evening)).Id;

    private async Task<Guid> JoinAsync(Guid match, string name, TeamSide? side = TeamSide.A)
    {
        var player = (await Squads.AddNewPlayerAsync(match, name)).Id;
        await Lineups.AssignAsync(match, player, side);
        return player;
    }

    // Les écritures successives ont des dates distinctes, comme sur un appareil.
    private async Task SaveAsync(Guid match, int a, int b, params PlayerTally[] players)
    {
        _clock.Advance(TimeSpan.FromSeconds(1));
        await Results.SaveAsync(match, new ResultInput(a, b, players));
    }

    private async Task<StatsSummary> SummaryAsync() =>
        StatsCalculator.Compute((await Statistics.GetAsync()).Matches, new StatsPeriod(), TimeZoneInfo.Utc);

    [Fact]
    public async Task The_statistics_come_from_the_teams_the_score_and_the_scorers()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");
        var alice = await JoinAsync(match, "Alice", TeamSide.B);
        var karim = await JoinAsync(match, "Karim", side: null);
        await SaveAsync(match, 2, 0, new PlayerTally(sami, Goals: 2, Assists: 1));

        var summary = await SummaryAsync();

        Assert.Equal(new PlayerStats(sami, 1, 1, 0, 0, 2, 1), summary.For(sami));
        Assert.Equal(new PlayerStats(alice, 1, 0, 0, 1), summary.For(alice));
        // Sans équipe, on n'a pas joué (RG-STA-04).
        Assert.False(summary.Players.ContainsKey(karim));
    }

    [Fact]
    public async Task A_corrected_result_changes_the_statistics()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");
        await SaveAsync(match, 3, 0, new PlayerTally(sami, Goals: 3));

        await SaveAsync(match, 0, 1, new PlayerTally(sami));

        Assert.Equal(new PlayerStats(sami, 1, 0, 0, 1), (await SummaryAsync()).For(sami));
    }

    [Fact]
    public async Task Unfinished_cancelled_cleared_and_deleted_matches_are_left_out()
    {
        var planned = await NewMatchAsync();
        await JoinAsync(planned, "Sami");
        await Matches.CancelAsync(await NewMatchAsync());
        var cleared = await NewMatchAsync();
        await JoinAsync(cleared, "Alice");
        await SaveAsync(cleared, 1, 0);
        await Results.ClearAsync(cleared);
        var deleted = await NewMatchAsync();
        await JoinAsync(deleted, "Karim");
        await SaveAsync(deleted, 1, 0);
        await Matches.DeleteAsync(deleted);

        Assert.Empty((await Statistics.GetAsync()).Matches);
    }

    [Fact]
    public async Task A_player_who_left_the_match_loses_its_goals_and_an_archived_one_keeps_them()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");
        var alice = await JoinAsync(match, "Alice");
        await SaveAsync(match, 2, 0, new PlayerTally(sami, Goals: 1), new PlayerTally(alice, Goals: 1));

        await Squads.SetSelectedAsync(match, sami, selected: false);
        await _app.GetRequiredService<PlayerService>().SetArchivedAsync(alice, archived: true);

        var summary = await SummaryAsync();
        Assert.False(summary.Players.ContainsKey(sami));
        Assert.Equal(1, summary.For(alice).Goals);
        Assert.NotNull((await Statistics.GetAsync()).Find(alice));
    }

    [Fact]
    public async Task The_own_player_is_the_one_of_the_profile()
    {
        await _app.GetRequiredService<PlayerService>().AddAsync(new PlayerInput("Alice"));
        Assert.Null((await Statistics.GetAsync()).OwnPlayerId);

        await _app.GetRequiredService<ProfileService>().SaveAsync(new ProfileInput("Sami"));

        var history = await Statistics.GetAsync();
        Assert.Equal("Sami", history.Find(history.OwnPlayerId!.Value)!.Name);
    }
}
