using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Un joueur cité sur la carte.</summary>
/// <param name="Name">Son nom.</param>
/// <param name="Position">Son poste pour ce match ; null s'il n'en a pas.</param>
public sealed record CardPlayer(string Name, string? Position);

/// <summary>Un emplacement dessiné sur le terrain.</summary>
/// <param name="Position">Abréviation du poste.</param>
/// <param name="Name">Nom du joueur ; null si l'emplacement est libre.</param>
public sealed record CardSpot(string Position, string? Name);

/// <summary>Une équipe de la carte.</summary>
/// <param name="Name">Nom affiché.</param>
/// <param name="ColorCode">Code de <see cref="TeamColors"/>.</param>
/// <param name="Formation">Code de la formation dessinée ; null sans terrain.</param>
/// <param name="Lines">Les lignes du terrain, de l'arrière vers l'avant ; vide sans terrain.</param>
/// <param name="Players">Les joueurs listés : ceux du terrain (gardien d'abord), sinon toute l'équipe.</param>
/// <param name="Others">Avec un terrain, les joueurs de l'équipe sans emplacement.</param>
/// <param name="OthersTitle">Titre de ces autres joueurs ; null s'il n'y en a pas.</param>
public sealed record CardTeam(
    string Name,
    string ColorCode,
    string? Formation,
    IReadOnlyList<IReadOnlyList<CardSpot>> Lines,
    IReadOnlyList<CardPlayer> Players,
    IReadOnlyList<CardPlayer> Others,
    string? OthersTitle);

/// <summary>
/// Ce qui est partagé d'un match (US-011) : la même carte donne le texte et l'image. Elle ne contient ni niveaux
/// (visibles du seul organisateur, RG-JOU-06) ni notes.
/// </summary>
/// <param name="SportCode">Code de <see cref="SportCatalog"/>.</param>
/// <param name="Title">Titre avec la date.</param>
/// <param name="Details">Heure et format.</param>
/// <param name="Location">Lieu ; null s'il n'est pas renseigné.</param>
/// <param name="Teams">Les deux équipes ; vide tant que personne n'est assigné.</param>
/// <param name="Unassigned">Les joueurs sans équipe.</param>
/// <param name="UnassignedTitle">Titre de cette dernière liste.</param>
/// <param name="EmptyTeam">Texte d'une équipe sans joueur.</param>
public sealed record LineupCard(
    string SportCode,
    string Title,
    string Details,
    string? Location,
    IReadOnlyList<CardTeam> Teams,
    IReadOnlyList<string> Unassigned,
    string UnassignedTitle,
    string EmptyTeam);

/// <summary>Les textes traduits d'une carte, fournis par l'interface.</summary>
/// <param name="Title">Titre avec la date.</param>
/// <param name="Details">Heure et format.</param>
/// <param name="TeamName">Nom affiché d'une équipe, à partir de son côté et du nom choisi.</param>
/// <param name="Position">Nom d'un poste.</param>
/// <param name="ShortPosition">Abréviation d'un poste, pour le terrain.</param>
/// <param name="Substitutes">Titre des joueurs hors du terrain quand il est complet.</param>
/// <param name="OtherPlayers">Titre des joueurs hors du terrain quand il reste des places.</param>
/// <param name="Unassigned">Titre des joueurs sans équipe.</param>
/// <param name="Players">Titre de la liste des joueurs quand les équipes ne sont pas composées.</param>
/// <param name="EmptyTeam">Texte d'une équipe sans joueur.</param>
public sealed record LineupCardLabels(
    string Title,
    string Details,
    Func<TeamSide, string?, string> TeamName,
    Func<string, string> Position,
    Func<string, string> ShortPosition,
    string Substitutes,
    string OtherPlayers,
    string Unassigned,
    string Players,
    string EmptyTeam);

/// <summary>Prépare la carte à partager à partir de la composition d'un match.</summary>
public static class LineupCardBuilder
{
    public static LineupCard Build(MatchLineup lineup, LineupCardLabels labels)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(labels);

        var composed = lineup.TeamA.Players.Count + lineup.TeamB.Players.Count > 0;
        IReadOnlyList<CardTeam> teams = composed
            ? [Describe(lineup.TeamA, labels), Describe(lineup.TeamB, labels)]
            : [];

        return new LineupCard(
            lineup.Match.SportCode,
            labels.Title,
            labels.Details,
            lineup.Match.Location,
            teams,
            lineup.Unassigned.Select(player => player.Name).ToList(),
            composed ? labels.Unassigned : labels.Players,
            labels.EmptyTeam);
    }

    private static CardTeam Describe(TeamLineup team, LineupCardLabels labels)
    {
        var name = labels.TeamName(team.Side, team.Name);

        CardPlayer Listed(Player player) => new(
            player.Name,
            team.Positions.TryGetValue(player.Id, out var code) ? labels.Position(code) : null);

        // Un terrain vide n'apprend rien : sans joueur placé, l'équipe est simplement listée.
        if (team.FilledFormation is not { } formation)
        {
            return new CardTeam(name, team.ColorCode, null, [], team.Starters.Select(Listed).ToList(), [], null);
        }

        var lines = formation.Lines
            .Select(line => (IReadOnlyList<CardSpot>)line
                .Select(slot => new CardSpot(
                    labels.ShortPosition(slot.PositionCode),
                    team.OnPitch.GetValueOrDefault(slot.Index)?.Name))
                .ToList())
            .ToList();
        var placed = team.Starters.Select(Listed).ToList();
        var others = team.Others.Select(Listed).ToList();
        var othersTitle = others.Count == 0 ? null
            : team.IsPitchFull ? labels.Substitutes
            : labels.OtherPlayers;

        return new CardTeam(name, team.ColorCode, formation.Code, lines, placed, others, othersTitle);
    }
}
