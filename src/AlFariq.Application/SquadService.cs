using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Les joueurs d'un match.</summary>
/// <param name="Match">Le match.</param>
/// <param name="AllPlayers">Tout le répertoire (hors joueurs supprimés), pour repérer les doublons.</param>
/// <param name="Candidates">Les joueurs à cocher : les non archivés, plus ceux déjà inscrits ; triés par nom.</param>
/// <param name="Selected">Les joueurs inscrits, triés par nom.</param>
public sealed record Squad(
    Match Match,
    IReadOnlyList<Player> AllPlayers,
    IReadOnlyList<Player> Candidates,
    IReadOnlyList<Player> Selected);

/// <summary>
/// Choix des joueurs d'un match (US-007). Un joueur archivé ou supprimé reste sur les matchs où il est inscrit,
/// mais ne s'ajoute pas à un nouveau match (RG-JOU-03 et 04).
/// </summary>
public sealed class SquadService
{
    private readonly IRepository<Match> _matches;
    private readonly IRepository<MatchParticipant> _participants;
    private readonly IRepository<Player> _players;
    private readonly PlayerService _directory;

    // Lire puis écrire les inscriptions une demande à la fois : deux taps rapides ne créent pas de doublon.
    private readonly SemaphoreSlim _selectionGate = new(1, 1);

    public SquadService(
        IRepository<Match> matches,
        IRepository<MatchParticipant> participants,
        IRepository<Player> players,
        PlayerService directory)
    {
        _matches = matches;
        _participants = participants;
        _players = players;
        _directory = directory;
    }

    /// <summary>Les joueurs d'un match ; null si le match est introuvable.</summary>
    public async Task<Squad?> GetAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var match = await _matches.GetAsync(matchId, cancellationToken: cancellationToken);
        if (match is null)
        {
            return null;
        }

        var selectedIds = (await _participants.ListAsync(cancellationToken: cancellationToken))
            .Where(participant => participant.MatchId == matchId)
            .Select(participant => participant.PlayerId)
            .ToHashSet();
        var players = await _players.ListAsync(includeDeleted: true, cancellationToken);

        var candidates = PlayerDirectory.Search(
            players.Where(player => selectedIds.Contains(player.Id) || (!player.IsDeleted && !player.IsArchived)),
            query: null);

        return new Squad(
            match,
            players.Where(player => !player.IsDeleted).ToList(),
            candidates,
            candidates.Where(player => selectedIds.Contains(player.Id)).ToList());
    }

    /// <summary>Inscrit un joueur au match, ou l'en retire. Sans effet si c'est déjà le cas.</summary>
    /// <exception cref="KeyNotFoundException">Le match ou le joueur est introuvable.</exception>
    /// <exception cref="InvalidOperationException">Joueur archivé ou supprimé qui n'a jamais été sur ce match.</exception>
    public async Task SetSelectedAsync(
        Guid matchId, Guid playerId, bool selected, CancellationToken cancellationToken = default)
    {
        await _selectionGate.WaitAsync(cancellationToken);
        try
        {
            await RequireMatchAsync(matchId, cancellationToken);

            // Les inscriptions retirées comptent : elles montrent que le joueur a déjà été sur ce match.
            var history = (await _participants.ListAsync(includeDeleted: true, cancellationToken))
                .Where(participant => participant.MatchId == matchId && participant.PlayerId == playerId)
                .ToList();
            var active = history.Where(participant => !participant.IsDeleted).ToList();

            if (!selected)
            {
                foreach (var participant in active)
                {
                    await _participants.DeleteAsync(participant.Id, cancellationToken);
                }

                return;
            }

            if (active.Count > 0)
            {
                return;
            }

            var player = await _players.GetAsync(playerId, includeDeleted: true, cancellationToken)
                ?? throw new KeyNotFoundException($"Aucun joueur avec l'identifiant {playerId}.");
            if ((player.IsArchived || player.IsDeleted) && history.Count == 0)
            {
                throw new InvalidOperationException("Un joueur archivé ou supprimé ne s'ajoute pas à un match.");
            }

            await _participants.AddAsync(
                new MatchParticipant { MatchId = matchId, PlayerId = playerId }, cancellationToken);
        }
        finally
        {
            _selectionGate.Release();
        }
    }

    /// <summary>Ajout rapide : crée le joueur dans le répertoire et l'inscrit au match.</summary>
    /// <exception cref="ArgumentException">Nom vide ou trop long.</exception>
    /// <exception cref="KeyNotFoundException">Le match est introuvable ; aucun joueur n'est alors créé.</exception>
    public async Task<Player> AddNewPlayerAsync(Guid matchId, string name, CancellationToken cancellationToken = default)
    {
        await RequireMatchAsync(matchId, cancellationToken);

        var player = await _directory.AddAsync(new PlayerInput(name), cancellationToken);
        await SetSelectedAsync(matchId, player.Id, selected: true, cancellationToken);
        return player;
    }

    private async Task RequireMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        if (await _matches.GetAsync(matchId, cancellationToken: cancellationToken) is null)
        {
            throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {matchId}.");
        }
    }
}
