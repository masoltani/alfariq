using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>L'historique dont se calculent les statistiques.</summary>
/// <param name="Matches">Les matchs terminés et les joueurs de leurs équipes.</param>
/// <param name="Players">Tous les joueurs, y compris archivés ou supprimés (RG-JOU-03 et 04).</param>
/// <param name="OwnPlayerId">Le joueur qui est la personne du profil ; null sans profil.</param>
public sealed record StatsHistory(
    IReadOnlyList<FinishedMatch> Matches, IReadOnlyList<Player> Players, Guid? OwnPlayerId)
{
    public Player? Find(Guid playerId) => Players.FirstOrDefault(player => player.Id == playerId);
}

/// <summary>
/// Rassemble l'historique pour les statistiques (US-014). Rien n'est enregistré : tout est relu des matchs, des
/// équipes, des scores et des buteurs.
/// </summary>
public sealed class StatisticsService
{
    private readonly IRepository<Match> _matches;
    private readonly IRepository<MatchParticipant> _participants;
    private readonly IRepository<MatchSide> _sides;
    private readonly IRepository<MatchEvent> _events;
    private readonly IRepository<Player> _players;

    public StatisticsService(
        IRepository<Match> matches,
        IRepository<MatchParticipant> participants,
        IRepository<MatchSide> sides,
        IRepository<MatchEvent> events,
        IRepository<Player> players)
    {
        _matches = matches;
        _participants = participants;
        _sides = sides;
        _events = events;
        _players = players;
    }

    public async Task<StatsHistory> GetAsync(CancellationToken cancellationToken = default)
    {
        var matches = await _matches.ListAsync(cancellationToken: cancellationToken);
        var players = await _players.ListAsync(includeDeleted: true, cancellationToken);
        var known = players.Select(player => player.Id).ToHashSet();
        var registrations = (await _participants.ListAsync(cancellationToken: cancellationToken))
            .ToLookup(participant => participant.MatchId);
        var sides = (await _sides.ListAsync(cancellationToken: cancellationToken))
            .ToLookup(side => side.MatchId);
        var events = (await _events.ListAsync(cancellationToken: cancellationToken))
            .ToLookup(item => (item.MatchId, item.PlayerId));

        FinishedMatch Describe(Match match)
        {
            int ScoreOf(TeamSide side) =>
                sides[match.Id].FirstOrDefault(candidate => candidate.Side == side)?.Score ?? 0;

            // Comme pour la composition : la dernière inscription modifiée l'emporte, et seuls les joueurs d'une
            // équipe ont joué (RG-STA-04).
            var appearances = registrations[match.Id]
                .GroupBy(participant => participant.PlayerId)
                .Select(group => group.MaxBy(participant => participant.UpdatedAt)!)
                .Where(participant => participant.Side is not null && known.Contains(participant.PlayerId))
                .Select(participant =>
                {
                    var own = events[(match.Id, participant.PlayerId)];
                    return new Appearance(
                        participant.PlayerId,
                        participant.Side!.Value,
                        own.Count(item => item.Type == MatchEventType.Goal),
                        own.Count(item => item.Type == MatchEventType.Assist));
                })
                .ToList();

            return new FinishedMatch(
                match, new MatchScore(ScoreOf(TeamSide.A), ScoreOf(TeamSide.B)), appearances);
        }

        return new StatsHistory(
            matches.Where(match => match.Status == MatchStatus.Finished).Select(Describe).ToList(),
            players,
            players.FirstOrDefault(player => player.ProfileId is not null && !player.IsDeleted)?.Id);
    }
}
