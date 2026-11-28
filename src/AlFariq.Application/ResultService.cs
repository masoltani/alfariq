using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Les buts et les passes décisives d'un joueur dans un match.</summary>
public sealed record PlayerTally(Guid PlayerId, int Goals = 0, int Assists = 0)
{
    public bool IsEmpty => Goals == 0 && Assists == 0;
}

/// <summary>Un résultat saisi.</summary>
/// <param name="ScoreA">Score de l'équipe A.</param>
/// <param name="ScoreB">Score de l'équipe B.</param>
/// <param name="Players">Les buteurs et les passeurs, facultatifs ; un joueur des équipes absent n'en a aucun.</param>
public sealed record ResultInput(int ScoreA, int ScoreB, IReadOnlyList<PlayerTally>? Players = null);

/// <summary>Un match, sa composition et son résultat.</summary>
/// <param name="Lineup">La composition.</param>
/// <param name="Score">Le score ; null tant que le match n'est pas terminé.</param>
/// <param name="Players">Les joueurs d'une équipe qui ont marqué ou fait une passe, par identifiant.</param>
public sealed record MatchSheet(
    MatchLineup Lineup, MatchScore? Score, IReadOnlyDictionary<Guid, PlayerTally> Players)
{
    public Match Match => Lineup.Match;

    /// <summary>Le total des buts attribués aux joueurs d'une équipe.</summary>
    public int PlayerGoals(TeamSide side) =>
        Lineup.Team(side).Players.Sum(player => Players.TryGetValue(player.Id, out var tally) ? tally.Goals : 0);
}

/// <summary>
/// Résultat d'un match (US-012) : le score de chaque équipe et, si on le souhaite, les buteurs et les passeurs.
/// Valider un score rend le match « Terminé » (RG-RES-05) ; il reste modifiable (RG-MAT-05).
/// </summary>
public sealed class ResultService
{
    private readonly IRepository<Match> _matches;
    private readonly IRepository<MatchSide> _sides;
    private readonly IRepository<MatchEvent> _events;
    private readonly LineupService _lineups;

    // Un résultat s'écrit en plusieurs documents : un seul enregistrement à la fois.
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public ResultService(
        IRepository<Match> matches,
        IRepository<MatchSide> sides,
        IRepository<MatchEvent> events,
        LineupService lineups)
    {
        _matches = matches;
        _sides = sides;
        _events = events;
        _lineups = lineups;
    }

    /// <summary>La feuille d'un match ; null si le match est introuvable.</summary>
    public async Task<MatchSheet?> GetAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var lineup = await _lineups.GetAsync(matchId, cancellationToken);
        if (lineup is null)
        {
            return null;
        }

        if (lineup.Match.Status != MatchStatus.Finished)
        {
            return new MatchSheet(lineup, null, new Dictionary<Guid, PlayerTally>());
        }

        var sides = (await _sides.ListAsync(cancellationToken: cancellationToken))
            .Where(side => side.MatchId == matchId)
            .ToList();
        int ScoreOf(TeamSide side) => sides.FirstOrDefault(candidate => candidate.Side == side)?.Score ?? 0;

        // Les buts d'un joueur retiré des équipes ne comptent plus.
        var onTeam = TeamPlayers(lineup);
        var tallies = (await EventsAsync(matchId, cancellationToken))
            .Where(item => onTeam.Contains(item.PlayerId))
            .GroupBy(item => item.PlayerId)
            .ToDictionary(
                group => group.Key,
                group => new PlayerTally(
                    group.Key,
                    group.Count(item => item.Type == MatchEventType.Goal),
                    group.Count(item => item.Type == MatchEventType.Assist)));

        return new MatchSheet(lineup, new MatchScore(ScoreOf(TeamSide.A), ScoreOf(TeamSide.B)), tallies);
    }

    /// <summary>Enregistre ou corrige un résultat ; le match devient « Terminé ».</summary>
    /// <exception cref="ArgumentException">
    /// Score ou nombre hors limites, joueur cité deux fois, ou buteurs pour un sport sans buts.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    /// <exception cref="InvalidOperationException">
    /// Le match est annulé, ou un buteur ou un passeur ne joue dans aucune équipe ; rien n'est alors enregistré.
    /// </exception>
    public async Task SaveAsync(Guid matchId, ResultInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequireCount(input.ScoreA);
        RequireCount(input.ScoreB);
        var wanted = (input.Players ?? []).ToDictionary(tally => tally.PlayerId);
        foreach (var tally in wanted.Values)
        {
            RequireCount(tally.Goals);
            RequireCount(tally.Assists);
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var lineup = await _lineups.GetAsync(matchId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {matchId}.");
            if (lineup.Match.Status == MatchStatus.Cancelled)
            {
                throw new InvalidOperationException("Un match annulé n'a pas de résultat.");
            }

            var onTeam = TeamPlayers(lineup);
            var cited = wanted.Values.Where(tally => !tally.IsEmpty).ToList();
            if (cited.Count > 0 && !SportCatalog.CountsGoals(lineup.Match.SportCode))
            {
                throw new ArgumentException("Ce sport ne compte pas de buteurs.", nameof(input));
            }

            if (cited.Any(tally => !onTeam.Contains(tally.PlayerId)))
            {
                throw new InvalidOperationException("Un buteur ou un passeur doit jouer dans une des équipes.");
            }

            foreach (var player in await EventsByPlayerAsync(matchId, onTeam, cancellationToken))
            {
                var tally = wanted.GetValueOrDefault(player.PlayerId) ?? new PlayerTally(player.PlayerId);
                await ResizeAsync(matchId, player, MatchEventType.Goal, tally.Goals, cancellationToken);
                await ResizeAsync(matchId, player, MatchEventType.Assist, tally.Assists, cancellationToken);
            }

            await _lineups.SetScoresAsync(matchId, input.ScoreA, input.ScoreB, cancellationToken);

            // Le statut change en dernier : un match n'est jamais « Terminé » sans son score. Le match est réécrit à
            // chaque correction, ce qui date la dernière modification (RG-MAT-05).
            lineup.Match.Status = MatchStatus.Finished;
            await _matches.UpdateAsync(lineup.Match, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>Efface un résultat saisi par erreur : le match redevient « Planifié ».</summary>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    /// <exception cref="InvalidOperationException">Le match n'est pas terminé.</exception>
    public async Task ClearAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var match = await _matches.GetAsync(matchId, cancellationToken: cancellationToken)
                ?? throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {matchId}.");
            if (match.Status != MatchStatus.Finished)
            {
                throw new InvalidOperationException("Ce match n'a pas de résultat.");
            }

            match.Status = MatchStatus.Planned;
            await _matches.UpdateAsync(match, cancellationToken);
            await _lineups.SetScoresAsync(matchId, null, null, cancellationToken);

            foreach (var item in await EventsAsync(matchId, cancellationToken))
            {
                await _events.DeleteAsync(item.Id, cancellationToken);
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static HashSet<Guid> TeamPlayers(MatchLineup lineup) =>
        lineup.TeamA.Players.Concat(lineup.TeamB.Players).Select(player => player.Id).ToHashSet();

    private static void RequireCount(int value)
    {
        if (value is < 0 or > MatchSide.MaxScore)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"De 0 à {MatchSide.MaxScore}.");
        }
    }

    private async Task<List<MatchEvent>> EventsAsync(Guid matchId, CancellationToken cancellationToken) =>
        (await _events.ListAsync(cancellationToken: cancellationToken))
            .Where(item => item.MatchId == matchId)
            .ToList();

    /// <summary>Les événements enregistrés de chaque joueur des équipes.</summary>
    private async Task<List<PlayerEvents>> EventsByPlayerAsync(
        Guid matchId, HashSet<Guid> players, CancellationToken cancellationToken)
    {
        var events = await EventsAsync(matchId, cancellationToken);
        return players
            .Select(id => new PlayerEvents(id, events.Where(item => item.PlayerId == id).ToList()))
            .ToList();
    }

    /// <summary>Ajoute ou supprime des événements d'un type jusqu'au nombre voulu.</summary>
    private async Task ResizeAsync(
        Guid matchId, PlayerEvents player, MatchEventType type, int count, CancellationToken cancellationToken)
    {
        var existing = player.Events.Where(item => item.Type == type).ToList();
        foreach (var extra in existing.Skip(count))
        {
            await _events.DeleteAsync(extra.Id, cancellationToken);
        }

        for (var i = existing.Count; i < count; i++)
        {
            await _events.AddAsync(
                new MatchEvent { MatchId = matchId, PlayerId = player.PlayerId, Type = type }, cancellationToken);
        }
    }

    private sealed record PlayerEvents(Guid PlayerId, IReadOnlyList<MatchEvent> Events);
}
