using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public sealed class LineupServiceTests : IDisposable
{
    private static readonly MatchInput Evening = new(new DateOnly(2026, 12, 2), new TimeOnly(20, 0), 5);

    private readonly ServiceProvider _app;
    private readonly LineupService _lineups;

    public LineupServiceTests()
    {
        _app = MatchTestHost.Create(new FakeClock());
        _lineups = _app.GetRequiredService<LineupService>();
    }

    public void Dispose() => _app.Dispose();

    private MatchService Matches => _app.GetRequiredService<MatchService>();

    private SquadService Squads => _app.GetRequiredService<SquadService>();

    private async Task<Guid> NewMatchAsync() => (await Matches.CreateAsync(Evening)).Id;

    private async Task<Guid> JoinAsync(Guid match, string name) => (await Squads.AddNewPlayerAsync(match, name)).Id;

    private static IEnumerable<string> Names(IEnumerable<Player> players) => players.Select(player => player.Name);

    [Fact]
    public async Task Selected_players_start_unassigned_in_two_default_teams()
    {
        var match = await NewMatchAsync();
        await JoinAsync(match, "Sami");
        await JoinAsync(match, "Alice");

        var lineup = (await _lineups.GetAsync(match))!;

        Assert.Equal(new[] { "Alice", "Sami" }, Names(lineup.Unassigned));
        Assert.Empty(lineup.TeamA.Players);
        Assert.Empty(lineup.TeamB.Players);
        Assert.Null(lineup.TeamA.Name);
        Assert.Equal("blue", lineup.TeamA.ColorCode);
        Assert.Equal("red", lineup.TeamB.ColorCode);
    }

    [Fact]
    public async Task A_player_moves_between_teams_and_is_never_in_two()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");

        await _lineups.AssignAsync(match, sami, TeamSide.A);

        Assert.Equal(new[] { "Sami" }, Names((await _lineups.GetAsync(match))!.TeamA.Players));

        await _lineups.AssignAsync(match, sami, TeamSide.B);
        var moved = (await _lineups.GetAsync(match))!;

        Assert.Empty(moved.TeamA.Players);
        Assert.Empty(moved.Unassigned);
        Assert.Equal(new[] { "Sami" }, Names(moved.TeamB.Players));
        Assert.Equal(1, moved.PlayerCount);

        await _lineups.AssignAsync(match, sami, side: null);

        Assert.Equal(new[] { "Sami" }, Names((await _lineups.GetAsync(match))!.Unassigned));
    }

    [Fact]
    public async Task Only_a_player_of_an_existing_match_can_be_assigned()
    {
        var match = await NewMatchAsync();
        var outsider = (await _app.GetRequiredService<PlayerService>().AddAsync(new PlayerInput("Zoé"))).Id;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _lineups.AssignAsync(match, outsider, TeamSide.A));
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _lineups.AssignAsync(Guid.CreateVersion7(), outsider, TeamSide.A));
        Assert.Null(await _lineups.GetAsync(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task A_player_removed_from_the_match_loses_his_team()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");
        await _lineups.AssignAsync(match, sami, TeamSide.A);

        await Squads.SetSelectedAsync(match, sami, selected: false);

        Assert.Equal(0, (await _lineups.GetAsync(match))!.PlayerCount);

        await Squads.SetSelectedAsync(match, sami, selected: true);

        Assert.Equal(new[] { "Sami" }, Names((await _lineups.GetAsync(match))!.Unassigned));
    }

    [Fact]
    public async Task A_team_can_be_named_and_coloured_and_is_stored_once()
    {
        var match = await NewMatchAsync();

        await _lineups.SetTeamAsync(match, TeamSide.A, "  Les   Bleus ", "green");
        await _lineups.SetTeamAsync(match, TeamSide.A, "Les Verts", "yellow");

        var lineup = (await _lineups.GetAsync(match))!;
        Assert.Equal("Les Verts", lineup.TeamA.Name);
        Assert.Equal("yellow", lineup.TeamA.ColorCode);
        Assert.Null(lineup.TeamB.Name);
        Assert.Single(await _app.GetRequiredService<IRepository<MatchSide>>().ListAsync());

        await _lineups.SetTeamAsync(match, TeamSide.A, "   ", "yellow");

        Assert.Null((await _lineups.GetAsync(match))!.TeamA.Name);
    }

    [Theory]
    [InlineData("1234567890123456789012345678901", "blue")]
    [InlineData("Équipe", "fuchsia")]
    public async Task Invalid_team_settings_are_rejected(string name, string color)
    {
        var match = await NewMatchAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => _lineups.SetTeamAsync(match, TeamSide.B, name, color));

        Assert.Empty(await _app.GetRequiredService<IRepository<MatchSide>>().ListAsync());
    }

    [Fact]
    public async Task A_validated_proposal_replaces_the_teams()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");
        var alice = await JoinAsync(match, "Alice");
        await _lineups.AssignAsync(match, sami, TeamSide.A);

        await _lineups.ApplyAsync(
            match, new Dictionary<Guid, TeamSide> { [sami] = TeamSide.B, [alice] = TeamSide.A });

        var lineup = (await _lineups.GetAsync(match))!;
        Assert.Equal(new[] { "Alice" }, Names(lineup.TeamA.Players));
        Assert.Equal(new[] { "Sami" }, Names(lineup.TeamB.Players));
        Assert.Empty(lineup.Unassigned);
    }

    [Fact]
    public async Task A_proposal_computed_for_other_players_changes_nothing()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");
        var proposal = new Dictionary<Guid, TeamSide> { [sami] = TeamSide.A };
        await JoinAsync(match, "Alice");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _lineups.ApplyAsync(match, proposal));
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _lineups.ApplyAsync(Guid.CreateVersion7(), proposal));

        Assert.Equal(2, (await _lineups.GetAsync(match))!.Unassigned.Count);
    }

    [Fact]
    public async Task A_duplicated_match_keeps_the_players_but_not_the_teams()
    {
        var source = await NewMatchAsync();
        var sami = await JoinAsync(source, "Sami");
        await _lineups.AssignAsync(source, sami, TeamSide.A);

        var copy = await Matches.CreateAsync((await Matches.GetDuplicateInputAsync(source))!, source);

        Assert.Equal(new[] { "Sami" }, Names((await _lineups.GetAsync(copy.Id))!.Unassigned));
        Assert.Equal(new[] { "Sami" }, Names((await _lineups.GetAsync(source))!.TeamA.Players));
    }
}
