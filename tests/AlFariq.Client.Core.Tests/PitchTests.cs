using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

/// <summary>Placement sur le terrain et postes (US-010).</summary>
public sealed class PitchTests : IDisposable
{
    private static readonly MatchInput Evening = new(new DateOnly(2026, 12, 2), new TimeOnly(20, 0), 5);

    private readonly FakeClock _clock = new();
    private readonly ServiceProvider _app;
    private readonly LineupService _lineups;

    public PitchTests()
    {
        _app = MatchTestHost.Create(_clock);
        _lineups = _app.GetRequiredService<LineupService>();
    }

    public void Dispose() => _app.Dispose();

    private MatchService Matches => _app.GetRequiredService<MatchService>();

    private SquadService Squads => _app.GetRequiredService<SquadService>();

    private async Task<Guid> NewMatchAsync() => (await Matches.CreateAsync(Evening)).Id;

    private async Task<Guid> JoinAsync(Guid match, string name, TeamSide? side = TeamSide.A)
    {
        var player = (await Squads.AddNewPlayerAsync(match, name)).Id;
        await _lineups.AssignAsync(match, player, side);
        return player;
    }

    private async Task<TeamLineup> TeamAAsync(Guid match) => (await _lineups.GetAsync(match))!.TeamA;

    // Chaque écriture a sa propre date, comme sur un appareil.
    private async Task PlaceAsync(Guid match, int slot, Guid? player)
    {
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _lineups.PlaceAsync(match, TeamSide.A, slot, player);
    }

    private async Task<Guid> MatchWithFormationAsync()
    {
        var match = await NewMatchAsync();
        await _lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");
        return match;
    }

    [Fact]
    public async Task A_lineup_without_formation_or_positions_is_valid()
    {
        var match = await NewMatchAsync();
        await JoinAsync(match, "Sami");

        var team = await TeamAAsync(match);

        Assert.Null(team.Formation);
        Assert.Empty(team.OnPitch);
        Assert.Empty(team.Positions);
        Assert.Single(team.OffPitch);
    }

    [Fact]
    public async Task A_placed_player_takes_the_position_of_the_spot()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");

        await PlaceAsync(match, 0, sami);

        var team = await TeamAAsync(match);
        Assert.Equal(sami, team.OnPitch[0].Id);
        Assert.Equal("football.goalkeeper", team.Positions[sami]);
        Assert.Empty(team.OffPitch);
    }

    [Fact]
    public async Task A_player_from_the_bench_replaces_the_occupant()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");
        var alice = await JoinAsync(match, "Alice");
        await PlaceAsync(match, 3, sami);

        await PlaceAsync(match, 3, alice);

        var team = await TeamAAsync(match);
        Assert.Equal(alice, team.OnPitch[3].Id);
        Assert.Equal(new[] { sami }, team.OffPitch.Select(player => player.Id));
        Assert.False(team.Positions.ContainsKey(sami));
    }

    [Fact]
    public async Task Two_players_on_the_pitch_swap_their_spots()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");
        var alice = await JoinAsync(match, "Alice");
        await PlaceAsync(match, 0, sami);
        await PlaceAsync(match, 4, alice);

        await PlaceAsync(match, 0, alice);

        var team = await TeamAAsync(match);
        Assert.Equal(alice, team.OnPitch[0].Id);
        Assert.Equal(sami, team.OnPitch[4].Id);
        Assert.Equal("football.forward", team.Positions[sami]);
    }

    [Fact]
    public async Task A_spot_can_be_emptied()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");
        await PlaceAsync(match, 1, sami);

        await PlaceAsync(match, 1, null);

        var team = await TeamAAsync(match);
        Assert.Empty(team.OnPitch);
        Assert.Empty(team.Positions);
    }

    [Fact]
    public async Task An_unassigned_player_joins_the_team_when_placed()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami", side: null);

        await PlaceAsync(match, 2, sami);

        Assert.Equal(sami, (await TeamAAsync(match)).OnPitch[2].Id);
    }

    [Fact]
    public async Task Invalid_placements_are_rejected()
    {
        var match = await NewMatchAsync();
        var opponent = await JoinAsync(match, "Zoé", TeamSide.B);

        await Assert.ThrowsAsync<InvalidOperationException>(() => PlaceAsync(match, 0, null));

        await _lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => PlaceAsync(match, 0, opponent));
        await Assert.ThrowsAsync<InvalidOperationException>(() => PlaceAsync(match, 0, Guid.CreateVersion7()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => PlaceAsync(match, 5, null));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _lineups.SetFormationAsync(match, TeamSide.A, "4-4-2"));
    }

    [Fact]
    public async Task Changing_team_frees_the_spot()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");
        await PlaceAsync(match, 0, sami);

        await _lineups.AssignAsync(match, sami, TeamSide.B);
        await _lineups.AssignAsync(match, sami, TeamSide.A);

        Assert.Empty((await TeamAAsync(match)).OnPitch);
    }

    [Fact]
    public async Task Changing_formation_keeps_the_players_on_the_pitch()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");
        var alice = await JoinAsync(match, "Alice");
        await PlaceAsync(match, 0, sami);
        await PlaceAsync(match, 4, alice);

        await _lineups.SetFormationAsync(match, TeamSide.A, "1-2-1");

        var team = await TeamAAsync(match);
        Assert.Equal("football.goalkeeper", team.Positions[sami]);
        Assert.Equal("football.forward", team.Positions[alice]);
        Assert.Equal(2, team.OnPitch.Count);
    }

    [Fact]
    public async Task Removing_the_formation_keeps_the_positions()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");
        await PlaceAsync(match, 0, sami);

        await _lineups.SetFormationAsync(match, TeamSide.A, null);

        var team = await TeamAAsync(match);
        Assert.Empty(team.OnPitch);
        Assert.Equal("football.goalkeeper", team.Positions[sami]);
    }

    [Fact]
    public async Task A_position_can_be_given_without_formation_only()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");

        await _lineups.SetPositionAsync(match, sami, "football.striker");

        Assert.Equal("football.striker", (await TeamAAsync(match)).Positions[sami]);
        await Assert.ThrowsAsync<ArgumentException>(
            () => _lineups.SetPositionAsync(match, sami, "handball.pivot"));

        await _lineups.SetPositionAsync(match, sami, null);

        Assert.Empty((await TeamAAsync(match)).Positions);

        await _lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _lineups.SetPositionAsync(match, sami, "football.striker"));
    }

    [Fact]
    public async Task A_formation_keeps_the_team_name_and_colour()
    {
        var match = await NewMatchAsync();
        await _lineups.SetTeamAsync(match, TeamSide.A, "Les Verts", "green");

        await _lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");
        await _lineups.SetTeamAsync(match, TeamSide.A, "Les Verts", "black");

        var team = await TeamAAsync(match);
        Assert.Equal("Les Verts", team.Name);
        Assert.Equal("black", team.ColorCode);
        Assert.Equal("2-1-1", team.Formation?.Code);
    }

    [Fact]
    public async Task A_formation_no_longer_applies_when_the_format_changes()
    {
        var match = await MatchWithFormationAsync();
        var sami = await JoinAsync(match, "Sami");
        await PlaceAsync(match, 0, sami);

        await Matches.UpdateAsync(match, Evening with { PlayersPerSide = 7 });

        var team = await TeamAAsync(match);
        Assert.Null(team.Formation);
        Assert.Empty(team.OnPitch);
        Assert.Equal(new[] { sami }, team.Players.Select(player => player.Id));
    }
}
