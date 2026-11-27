using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

/// <summary>La carte et le texte partagés (US-011).</summary>
public sealed class LineupShareTests : IDisposable
{
    private static readonly MatchInput Evening = new(
        new DateOnly(2026, 12, 2), new TimeOnly(20, 0), 5, Location: "Stade municipal", Notes: "Code du portail 1234");

    private static readonly LineupCardLabels Labels = new(
        "Match du mercredi 2 décembre 2026",
        "20:00 · 5 contre 5",
        (side, name) => name ?? $"Équipe {side}",
        code => $"poste:{code}",
        code => "P",
        "Remplaçants :",
        "Aussi dans l'équipe :",
        "Non assignés :",
        "Joueurs",
        "Aucun joueur.");

    private readonly ServiceProvider _app = MatchTestHost.Create(new FakeClock());

    public void Dispose() => _app.Dispose();

    private LineupService Lineups => _app.GetRequiredService<LineupService>();

    private async Task<Guid> NewMatchAsync(MatchInput? input = null) =>
        (await _app.GetRequiredService<MatchService>().CreateAsync(input ?? Evening)).Id;

    private async Task<Guid> JoinAsync(Guid match, string name, TeamSide? side = null)
    {
        var player = (await _app.GetRequiredService<SquadService>().AddNewPlayerAsync(match, name)).Id;
        await Lineups.AssignAsync(match, player, side);
        return player;
    }

    private async Task<LineupCard> CardAsync(Guid match) =>
        LineupCardBuilder.Build((await Lineups.GetAsync(match))!, Labels);

    [Fact]
    public async Task The_text_gives_date_time_place_teams_and_players()
    {
        var match = await NewMatchAsync();
        await JoinAsync(match, "Sami", TeamSide.A);
        await JoinAsync(match, "Alice", TeamSide.B);
        await JoinAsync(match, "Tom");
        await Lineups.SetTeamAsync(match, TeamSide.A, "Les Verts", "green");

        var text = LineupText.Compose(await CardAsync(match));

        Assert.Equal(
            string.Join('\n',
                "⚽ *Match du mercredi 2 décembre 2026*",
                "🕒 20:00 · 5 contre 5",
                "📍 Stade municipal",
                "",
                "🟢 *Les Verts*",
                "• Sami",
                "",
                "🔴 *Équipe B*",
                "• Alice",
                "",
                "Non assignés : Tom"),
            text);
    }

    [Fact]
    public async Task Without_teams_the_players_are_listed()
    {
        var match = await NewMatchAsync(Evening with { Location = null });
        await JoinAsync(match, "Sami");
        await JoinAsync(match, "Alice");

        var card = await CardAsync(match);
        var text = LineupText.Compose(card);

        Assert.Empty(card.Teams);
        Assert.DoesNotContain("📍", text);
        Assert.EndsWith("👥 *Joueurs*\n• Alice\n• Sami", text);
    }

    [Fact]
    public async Task Levels_and_notes_are_never_shared()
    {
        var match = await NewMatchAsync();
        var player = await _app.GetRequiredService<PlayerService>().AddAsync(new PlayerInput("Sami", Level: 5));
        await _app.GetRequiredService<SquadService>().SetSelectedAsync(match, player.Id, selected: true);
        await Lineups.AssignAsync(match, player.Id, TeamSide.A);

        var text = LineupText.Compose(await CardAsync(match));

        Assert.Contains("• Sami\n", text);
        Assert.DoesNotContain("1234", text);
    }

    [Fact]
    public async Task An_empty_team_is_said_empty_and_asterisks_do_not_break_the_bold()
    {
        var match = await NewMatchAsync();
        await JoinAsync(match, "Sami", TeamSide.A);
        await Lineups.SetTeamAsync(match, TeamSide.B, "*Stars*", "white");

        var text = LineupText.Compose(await CardAsync(match));

        Assert.Contains("⚪ *Stars*\nAucun joueur.", text);
    }

    [Fact]
    public async Task A_formation_with_players_gives_a_pitch_with_the_goalkeeper_first()
    {
        var match = await NewMatchAsync();
        var sami = await JoinAsync(match, "Sami", TeamSide.A);
        var alice = await JoinAsync(match, "Alice", TeamSide.A);
        await JoinAsync(match, "Tom", TeamSide.A);
        await Lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");
        await Lineups.PlaceAsync(match, TeamSide.A, 4, alice);
        await Lineups.PlaceAsync(match, TeamSide.A, 0, sami);

        var card = await CardAsync(match);
        var team = card.Teams[0];

        Assert.Equal("2-1-1", team.Formation);
        Assert.Equal(new[] { 1, 2, 1, 1 }, team.Lines.Select(line => line.Count));
        Assert.Equal("Sami", team.Lines[0][0].Name);
        Assert.Null(team.Lines[1][0].Name);
        Assert.Equal(new[] { "Sami", "Alice" }, team.Players.Select(player => player.Name));
        Assert.Contains(
            "(2-1-1)\n• Sami (poste:football.goalkeeper)\n• Alice (poste:football.forward)\nAussi dans l'équipe : Tom",
            LineupText.Compose(card));
    }

    [Fact]
    public async Task Players_off_a_full_pitch_are_substitutes()
    {
        var match = await NewMatchAsync();
        await Lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");
        for (var slot = 0; slot < 5; slot++)
        {
            var player = await JoinAsync(match, $"Joueur {slot}", TeamSide.A);
            await Lineups.PlaceAsync(match, TeamSide.A, slot, player);
        }

        await JoinAsync(match, "Tom", TeamSide.A);

        var team = (await CardAsync(match)).Teams[0];

        Assert.Equal("Remplaçants :", team.OthersTitle);
        Assert.Equal(new[] { "Tom" }, team.Others.Select(player => player.Name));
    }

    [Fact]
    public async Task A_formation_without_placed_players_gives_a_plain_list()
    {
        var match = await NewMatchAsync();
        await JoinAsync(match, "Sami", TeamSide.A);
        await Lineups.SetFormationAsync(match, TeamSide.A, "2-1-1");

        var team = (await CardAsync(match)).Teams[0];

        Assert.Null(team.Formation);
        Assert.Empty(team.Lines);
        Assert.Equal(new[] { "Sami" }, team.Players.Select(player => player.Name));
        Assert.Null(team.OthersTitle);
    }
}
