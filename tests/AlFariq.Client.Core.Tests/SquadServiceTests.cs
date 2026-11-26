using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public sealed class SquadServiceTests : IDisposable
{
    private static readonly MatchInput Evening = new(new DateOnly(2026, 12, 2), new TimeOnly(20, 0), 5);

    private readonly ServiceProvider _app;
    private readonly SquadService _squads;

    public SquadServiceTests()
    {
        _app = MatchTestHost.Create(new FakeClock());
        _squads = _app.GetRequiredService<SquadService>();
    }

    public void Dispose() => _app.Dispose();

    private MatchService Matches => _app.GetRequiredService<MatchService>();

    private PlayerService Players => _app.GetRequiredService<PlayerService>();

    private IRepository<MatchParticipant> Participants =>
        _app.GetRequiredService<IRepository<MatchParticipant>>();

    private async Task<Guid> NewMatchAsync() => (await Matches.CreateAsync(Evening)).Id;

    private async Task<Guid> NewPlayerAsync(string name) => (await Players.AddAsync(new PlayerInput(name))).Id;

    private static IEnumerable<string> Names(IEnumerable<Player> players) => players.Select(player => player.Name);

    [Fact]
    public async Task A_new_match_proposes_the_active_players_by_name_and_has_none_selected()
    {
        var match = await NewMatchAsync();
        await NewPlayerAsync("Sami");
        await NewPlayerAsync("Alice");
        await Players.SetArchivedAsync(await NewPlayerAsync("Zoé"), archived: true);

        var squad = (await _squads.GetAsync(match))!;

        Assert.Equal(new[] { "Alice", "Sami" }, Names(squad.Candidates));
        Assert.Empty(squad.Selected);
        Assert.Equal(3, squad.AllPlayers.Count);
        Assert.Equal(10, squad.Match.ExpectedPlayers);
    }

    [Fact]
    public async Task A_player_can_be_selected_removed_and_selected_again()
    {
        var match = await NewMatchAsync();
        var sami = await NewPlayerAsync("Sami");

        await _squads.SetSelectedAsync(match, sami, selected: true);
        await _squads.SetSelectedAsync(match, sami, selected: true);

        Assert.Equal(new[] { "Sami" }, Names((await _squads.GetAsync(match))!.Selected));
        Assert.Single(await Participants.ListAsync());

        await _squads.SetSelectedAsync(match, sami, selected: false);

        Assert.Empty((await _squads.GetAsync(match))!.Selected);
        Assert.True(Assert.Single(await Participants.ListAsync(includeDeleted: true)).IsDeleted);

        await _squads.SetSelectedAsync(match, sami, selected: true);

        Assert.Single((await _squads.GetAsync(match))!.Selected);
    }

    [Fact]
    public async Task Quick_add_puts_the_player_in_the_directory_and_in_the_match()
    {
        var match = await NewMatchAsync();

        var added = await _squads.AddNewPlayerAsync(match, "  Karim ");

        Assert.Equal("Karim", added.Name);
        Assert.Equal(new[] { "Karim" }, Names(await Players.ListAsync()));
        Assert.Equal(new[] { "Karim" }, Names((await _squads.GetAsync(match))!.Selected));
    }

    [Fact]
    public async Task Quick_add_writes_nothing_when_the_name_or_the_match_is_invalid()
    {
        var match = await NewMatchAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => _squads.AddNewPlayerAsync(match, "   "));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _squads.AddNewPlayerAsync(Guid.CreateVersion7(), "Karim"));

        Assert.Empty(await Players.ListAsync());
        Assert.Empty(await Participants.ListAsync());
    }

    [Fact]
    public async Task An_archived_player_stays_on_his_match_but_joins_no_other()
    {
        var match = await NewMatchAsync();
        var other = await NewMatchAsync();
        var sami = await NewPlayerAsync("Sami");
        await _squads.SetSelectedAsync(match, sami, selected: true);

        await Players.SetArchivedAsync(sami, archived: true);

        Assert.Equal(new[] { "Sami" }, Names((await _squads.GetAsync(match))!.Selected));
        Assert.Empty((await _squads.GetAsync(other))!.Candidates);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _squads.SetSelectedAsync(other, sami, selected: true));

        // Décoché par erreur, il peut être remis sur le match où il était.
        await _squads.SetSelectedAsync(match, sami, selected: false);
        await _squads.SetSelectedAsync(match, sami, selected: true);

        Assert.Single((await _squads.GetAsync(match))!.Selected);
    }

    [Fact]
    public async Task A_deleted_player_remains_visible_on_his_match()
    {
        var match = await NewMatchAsync();
        var sami = await NewPlayerAsync("Sami");
        await _squads.SetSelectedAsync(match, sami, selected: true);

        await _app.GetRequiredService<IRepository<Player>>().DeleteAsync(sami);

        var squad = (await _squads.GetAsync(match))!;
        Assert.Equal(new[] { "Sami" }, Names(squad.Selected));
        Assert.Empty(squad.AllPlayers);
    }

    [Fact]
    public async Task An_unknown_player_or_match_is_rejected()
    {
        var match = await NewMatchAsync();
        var sami = await NewPlayerAsync("Sami");

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _squads.SetSelectedAsync(match, Guid.CreateVersion7(), selected: true));
        await Matches.DeleteAsync(match);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _squads.SetSelectedAsync(match, sami, selected: true));
        Assert.Null(await _squads.GetAsync(match));
        Assert.Empty(await Participants.ListAsync());
    }

    [Fact]
    public async Task A_duplicated_match_starts_with_the_same_players()
    {
        var source = await NewMatchAsync();
        await _squads.AddNewPlayerAsync(source, "Sami");
        await _squads.AddNewPlayerAsync(source, "Alice");

        var copy = await Matches.CreateAsync((await Matches.GetDuplicateInputAsync(source))!, source);

        Assert.Equal(new[] { "Alice", "Sami" }, Names((await _squads.GetAsync(copy.Id))!.Selected));
    }
}
