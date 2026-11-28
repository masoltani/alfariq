using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>L'historique des matchs (US-013) : chaque match avec ses joueurs, pour le retrouver.</summary>
public sealed class MatchHistoryService
{
    private readonly IRepository<Match> _matches;
    private readonly IRepository<MatchParticipant> _participants;
    private readonly IRepository<Player> _players;

    public MatchHistoryService(
        IRepository<Match> matches,
        IRepository<MatchParticipant> participants,
        IRepository<Player> players)
    {
        _matches = matches;
        _participants = participants;
        _players = players;
    }

    /// <summary>
    /// Tous les matchs non supprimés et leurs joueurs. Un joueur archivé ou supprimé reste dans les matchs auxquels
    /// il a participé (RG-JOU-03 et 04) ; un joueur retiré d'un match n'y figure plus.
    /// </summary>
    public async Task<IReadOnlyList<MatchRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var matches = await _matches.ListAsync(cancellationToken: cancellationToken);
        var players = (await _players.ListAsync(includeDeleted: true, cancellationToken))
            .ToDictionary(player => player.Id);
        var registrations = (await _participants.ListAsync(cancellationToken: cancellationToken))
            .ToLookup(participant => participant.MatchId, participant => participant.PlayerId);

        return matches
            .Select(match => new MatchRecord(
                match,
                registrations[match.Id]
                    .Distinct()
                    .Where(players.ContainsKey)
                    .Select(id => players[id])
                    .ToList()))
            .ToList();
    }
}
