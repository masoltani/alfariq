using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

/// <summary>Historique (US-013) : les joueurs de chaque match et la composition présentée sur sa fiche.</summary>
public sealed class MatchHistoryTests : IDisposable
{
    private static readonly MatchInput Evening = new(new DateOnly(2026, 12, 2), new TimeOnly(20, 0), 5);

    private readonly FakeClock _clock = new();
    private readonly ServiceProvider _app;

    public MatchHistoryTests() => _app = MatchTestHost.Create(_clock);

    public void Dispose() => _app.Dispose();

    private MatchHistoryService History => _app.GetRequiredService<MatchHistoryService>();

    private MatchService Matches => _app.GetRequiredService<MatchService>();

    private SquadService Squads => _app.GetRequiredService<SquadService>();

    private LineupService Lineups => _app.GetRequiredService<LineupService>();

    private async Task<Guid> NewMatchAsync() => (await Matches.CreateAsync(Evening)).Id;

    private async Task<Guid> JoinAsync(Guid match, string name, TeamSide? side = TeamSide.A)
    {
        var player = (await Squads.AddNewPlayerAsync(match, name)).Id;
        await Lineups.AssignAsync(match, player, side);
        return player;
    }

    private async Task<IEnumerable<string>> PlayersOfAsync(Guid match) =>
        (await History.ListAsync()).Single(record => record.Match.Id == match)
            .Players.Select(player => player.Name).Order();

    [Fact]
    public async Task Each_match_comes_with_its_own_players()
    {
        var first = await NewMatchAsync();
        var second = await NewMatchAsync();
        var empty = await NewMatchAsync();
        await JoinAsync(first, "Sami");
        await JoinAsync(first, "Alice", side: null);
        await JoinAsync(second, "Karim");

        Assert.Equal(new[] { "Alice", "Sami" }, await PlayersOfAsync(first));
        Assert.Equal(new[] { "Karim" }, await PlayersOfAsync(second));
        Assert.Empty(await PlayersOfAsync(empty));
    }

    [Fact]
    public async Task Archived_and_deleted_players_stay_in_the_history()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");
        var alice = await JoinAsync(match, "Alice");

        await _app.GetRequiredService<PlayerService>().SetArchivedAsync(sami, archived: true);
        await _app.GetRequiredService<IRepository<Player>>().DeleteAsync(alice);

        Assert.Equal(new[] { "Alice", "Sami" }, await PlayersOfAsync(match));
    }

    [Fact]
    public async Task A_player_removed_from_a_match_is_no_longer_found_there()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami");

        await Squads.SetSelectedAsync(match, sami, selected: false);

        Assert.Empty(await PlayersOfAsync(match));
    }

    [Fact]
    public async Task A_deleted_match_leaves_the_history()
    {
        var match = await NewMatchAsync();

        await Matches.DeleteAsync(match);

        Assert.Empty(await History.ListAsync());
    }

    [Fact]
    public async Task Without_a_player_on_the_pitch_the_whole_team_is_listed_without_formation()
    {
        var match = await NewMatchAsync();
        await JoinAsync(match, "Sami");
        await Lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");

        var team = (await Lineups.GetAsync(match))!.TeamA;

        Assert.Null(team.FilledFormation);
        Assert.Equal(new[] { "Sami" }, team.Starters.Select(player => player.Name));
        Assert.Empty(team.Others);
        Assert.False(team.IsPitchFull);
    }

    [Fact]
    public async Task With_a_formation_the_pitch_comes_first_from_the_goalkeeper_then_the_others()
    {
        var match = await NewMatchAsync();
        var alice = await JoinAsync(match, "Alice");
        var karim = await JoinAsync(match, "Karim");
        await JoinAsync(match, "Zoé");
        await Lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");
        await Lineups.PlaceAsync(match, TeamSide.A, 4, alice);
        await Lineups.PlaceAsync(match, TeamSide.A, 0, karim);

        var team = (await Lineups.GetAsync(match))!.TeamA;

        Assert.Equal("2-1-1", team.FilledFormation!.Code);
        Assert.Equal(new[] { "Karim", "Alice" }, team.Starters.Select(player => player.Name));
        Assert.Equal(new[] { "Zoé" }, team.Others.Select(player => player.Name));
        Assert.False(team.IsPitchFull);
    }

    [Fact]
    public async Task The_pitch_is_full_when_every_spot_is_taken()
    {
        var match = await NewMatchAsync();
        await Lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");
        for (var slot = 0; slot < 5; slot++)
        {
            var player = await JoinAsync(match, $"Joueur {slot}");
            await Lineups.PlaceAsync(match, TeamSide.A, slot, player);
        }

        await JoinAsync(match, "Remplaçant");

        var team = (await Lineups.GetAsync(match))!.TeamA;

        Assert.True(team.IsPitchFull);
        Assert.Equal(new[] { "Remplaçant" }, team.Others.Select(player => player.Name));
    }
}
