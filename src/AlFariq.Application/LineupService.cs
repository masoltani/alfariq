using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Une équipe d'un match.</summary>
/// <param name="Side">Côté A ou B.</param>
/// <param name="Name">Nom choisi ; null pour le nom par défaut.</param>
/// <param name="ColorCode">Code de <see cref="TeamColors"/>.</param>
/// <param name="Players">Ses joueurs, triés par nom.</param>
public sealed record TeamLineup(TeamSide Side, string? Name, string ColorCode, IReadOnlyList<Player> Players);

/// <summary>La composition d'un match : chaque joueur inscrit figure dans une seule des trois listes.</summary>
public sealed record MatchLineup(Match Match, TeamLineup TeamA, TeamLineup TeamB, IReadOnlyList<Player> Unassigned)
{
    public int PlayerCount => TeamA.Players.Count + TeamB.Players.Count + Unassigned.Count;
}

/// <summary>Composition des équipes : à la main (US-008) ou d'après une proposition validée (US-009).</summary>
public sealed class LineupService
{
    private readonly IRepository<Match> _matches;
    private readonly IRepository<MatchParticipant> _participants;
    private readonly IRepository<MatchSide> _sides;
    private readonly SquadService _squads;

    // Les écritures sont lues puis réécrites : une à la fois, pour ne jamais créer deux fois les réglages d'une équipe.
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public LineupService(
        IRepository<Match> matches,
        IRepository<MatchParticipant> participants,
        IRepository<MatchSide> sides,
        SquadService squads)
    {
        _matches = matches;
        _participants = participants;
        _sides = sides;
        _squads = squads;
    }

    /// <summary>La composition d'un match ; null si le match est introuvable.</summary>
    public async Task<MatchLineup?> GetAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var squad = await _squads.GetAsync(matchId, cancellationToken);
        if (squad is null)
        {
            return null;
        }

        // Si un joueur avait plusieurs inscriptions (synchronisation future), la plus récemment modifiée l'emporte.
        var assignments = (await _participants.ListAsync(cancellationToken: cancellationToken))
            .Where(participant => participant.MatchId == matchId)
            .GroupBy(participant => participant.PlayerId)
            .ToDictionary(group => group.Key, group => group.MaxBy(participant => participant.UpdatedAt)!.Side);
        var settings = (await _sides.ListAsync(cancellationToken: cancellationToken))
            .Where(side => side.MatchId == matchId)
            .ToList();

        List<Player> PlayersOn(TeamSide? side) =>
            squad.Selected.Where(player => assignments.GetValueOrDefault(player.Id) == side).ToList();

        TeamLineup TeamOn(TeamSide side)
        {
            var custom = settings.FirstOrDefault(candidate => candidate.Side == side);
            var color = TeamColors.IsKnown(custom?.ColorCode) ? custom!.ColorCode : TeamColors.DefaultFor(side);
            return new TeamLineup(side, custom?.Name, color, PlayersOn(side));
        }

        return new MatchLineup(squad.Match, TeamOn(TeamSide.A), TeamOn(TeamSide.B), PlayersOn(null));
    }

    /// <summary>Place un joueur dans une équipe, ou le rend « non assigné » (<paramref name="side"/> null).</summary>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    /// <exception cref="InvalidOperationException">Le joueur n'est pas inscrit à ce match.</exception>
    public async Task AssignAsync(
        Guid matchId, Guid playerId, TeamSide? side, CancellationToken cancellationToken = default)
    {
        if (side is { } value && !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(side));
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await RequireMatchAsync(matchId, cancellationToken);

            var registrations = (await _participants.ListAsync(cancellationToken: cancellationToken))
                .Where(participant => participant.MatchId == matchId && participant.PlayerId == playerId)
                .ToList();
            if (registrations.Count == 0)
            {
                throw new InvalidOperationException("Ce joueur n'est pas inscrit à ce match.");
            }

            foreach (var participant in registrations.Where(participant => participant.Side != side))
            {
                participant.Side = side;
                await _participants.UpdateAsync(participant, cancellationToken);
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>Enregistre une répartition proposée, une fois validée par l'organisateur (US-009).</summary>
    /// <param name="matchId">Le match.</param>
    /// <param name="sides">Le côté de chacun des joueurs inscrits.</param>
    /// <param name="cancellationToken">Annulation.</param>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    /// <exception cref="InvalidOperationException">
    /// Les joueurs du match ne sont plus ceux de la proposition ; rien n'est alors enregistré.
    /// </exception>
    public async Task ApplyAsync(
        Guid matchId, IReadOnlyDictionary<Guid, TeamSide> sides, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sides);
        if (sides.Values.Any(side => !Enum.IsDefined(side)))
        {
            throw new ArgumentOutOfRangeException(nameof(sides));
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var squad = await _squads.GetAsync(matchId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {matchId}.");
            if (squad.Selected.Count != sides.Count || !squad.Selected.All(player => sides.ContainsKey(player.Id)))
            {
                throw new InvalidOperationException("Les joueurs du match ont changé depuis la proposition.");
            }

            var registrations = (await _participants.ListAsync(cancellationToken: cancellationToken))
                .Where(participant => participant.MatchId == matchId)
                .ToList();
            foreach (var participant in registrations)
            {
                if (sides.TryGetValue(participant.PlayerId, out var side) && participant.Side != side)
                {
                    participant.Side = side;
                    await _participants.UpdateAsync(participant, cancellationToken);
                }
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>Enregistre le nom et la couleur d'une équipe. Un nom vide revient au nom par défaut.</summary>
    /// <exception cref="ArgumentException">Nom trop long ou couleur inconnue.</exception>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    public async Task SetTeamAsync(
        Guid matchId, TeamSide side, string? name, string colorCode, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side));
        }

        var cleaned = PlayerDirectory.CleanName(name);
        if (cleaned.Length > MatchSide.MaxNameLength)
        {
            throw new ArgumentException(
                $"Le nom d'équipe dépasse {MatchSide.MaxNameLength} caractères.", nameof(name));
        }

        if (!TeamColors.IsKnown(colorCode))
        {
            throw new ArgumentException($"Couleur inconnue : « {colorCode} ».", nameof(colorCode));
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await RequireMatchAsync(matchId, cancellationToken);

            var existing = (await _sides.ListAsync(cancellationToken: cancellationToken))
                .FirstOrDefault(candidate => candidate.MatchId == matchId && candidate.Side == side);
            var team = existing ?? new MatchSide { MatchId = matchId, Side = side };
            team.Name = cleaned.Length == 0 ? null : cleaned;
            team.ColorCode = colorCode;

            if (existing is null)
            {
                await _sides.AddAsync(team, cancellationToken);
            }
            else
            {
                await _sides.UpdateAsync(team, cancellationToken);
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task RequireMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        if (await _matches.GetAsync(matchId, cancellationToken: cancellationToken) is null)
        {
            throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {matchId}.");
        }
    }
}
