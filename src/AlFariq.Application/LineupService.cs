using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Une équipe d'un match.</summary>
/// <param name="Side">Côté A ou B.</param>
/// <param name="Name">Nom choisi ; null pour le nom par défaut.</param>
/// <param name="ColorCode">Code de <see cref="TeamColors"/>.</param>
/// <param name="Players">Ses joueurs, triés par nom.</param>
/// <param name="Formation">Sa formation ; null sans formation, ou si celle enregistrée ne convient plus au format.</param>
/// <param name="OnPitch">Le joueur de chaque emplacement occupé de la formation.</param>
/// <param name="Positions">Le poste des joueurs qui en ont un pour ce match.</param>
public sealed record TeamLineup(
    TeamSide Side,
    string? Name,
    string ColorCode,
    IReadOnlyList<Player> Players,
    Formation? Formation,
    IReadOnlyDictionary<int, Player> OnPitch,
    IReadOnlyDictionary<Guid, string> Positions)
{
    /// <summary>Les joueurs de l'équipe sans emplacement (RG-COM-05), triés par nom.</summary>
    public IReadOnlyList<Player> OffPitch =>
        Players.Where(player => OnPitch.Values.All(placed => placed.Id != player.Id)).ToList();
}

/// <summary>La composition d'un match : chaque joueur inscrit figure dans une seule des trois listes.</summary>
public sealed record MatchLineup(Match Match, TeamLineup TeamA, TeamLineup TeamB, IReadOnlyList<Player> Unassigned)
{
    public int PlayerCount => TeamA.Players.Count + TeamB.Players.Count + Unassigned.Count;

    public TeamLineup Team(TeamSide side) => side == TeamSide.A ? TeamA : TeamB;
}

/// <summary>
/// Composition des équipes : à la main (US-008), d'après une proposition validée (US-009), puis placement sur le
/// terrain et postes (US-010).
/// </summary>
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
        var registrations = (await RegistrationsAsync(matchId, cancellationToken))
            .GroupBy(participant => participant.PlayerId)
            .ToDictionary(group => group.Key, group => group.MaxBy(participant => participant.UpdatedAt)!);
        var sport = SportCatalog.Find(squad.Match.SportCode);
        var settings = (await _sides.ListAsync(cancellationToken: cancellationToken))
            .Where(side => side.MatchId == matchId)
            .ToList();

        List<Player> PlayersOn(TeamSide? side) =>
            squad.Selected.Where(player => registrations.GetValueOrDefault(player.Id)?.Side == side).ToList();

        TeamLineup TeamOn(TeamSide side)
        {
            var custom = settings.FirstOrDefault(candidate => candidate.Side == side);
            var color = TeamColors.IsKnown(custom?.ColorCode) ? custom!.ColorCode : TeamColors.DefaultFor(side);
            var players = PlayersOn(side);
            var formation = FormationCatalog.Find(
                squad.Match.SportCode, squad.Match.PlayersPerSide, custom?.FormationCode);

            // Deux joueurs sur le même emplacement (synchronisation future) : le dernier placé l'occupe.
            var onPitch = formation is null
                ? new Dictionary<int, Player>()
                : players
                    .Where(player => registrations[player.Id].Slot is { } slot
                        && slot >= 0 && slot < formation.Slots.Count)
                    .GroupBy(player => registrations[player.Id].Slot!.Value)
                    .ToDictionary(
                        group => group.Key,
                        group => group.MaxBy(player => registrations[player.Id].UpdatedAt)!);

            var positions = new Dictionary<Guid, string>();
            foreach (var player in players)
            {
                if (registrations[player.Id].PositionCode is { } code && sport?.HasPosition(code) == true)
                {
                    positions[player.Id] = code;
                }
            }

            // Sur le terrain, c'est l'emplacement qui donne le poste.
            foreach (var (slot, player) in onPitch)
            {
                positions[player.Id] = formation!.Slots[slot].PositionCode;
            }

            return new TeamLineup(side, custom?.Name, color, players, formation, onPitch, positions);
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

            var registrations = (await RegistrationsAsync(matchId, cancellationToken))
                .Where(participant => participant.PlayerId == playerId)
                .ToList();
            if (registrations.Count == 0)
            {
                throw new InvalidOperationException("Ce joueur n'est pas inscrit à ce match.");
            }

            foreach (var participant in registrations.Where(participant => participant.Side != side))
            {
                participant.Side = side;
                participant.Slot = null;
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

            foreach (var participant in await RegistrationsAsync(matchId, cancellationToken))
            {
                if (sides.TryGetValue(participant.PlayerId, out var side) && participant.Side != side)
                {
                    participant.Side = side;
                    participant.Slot = null;
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

            await SaveSideAsync(
                matchId,
                side,
                team =>
                {
                    team.Name = cleaned.Length == 0 ? null : cleaned;
                    team.ColorCode = colorCode;
                },
                cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>
    /// Choisit la formation d'une équipe, ou l'enlève (<paramref name="formationCode"/> null). Les joueurs sont
    /// replacés au mieux (<see cref="Formation.Distribute"/>) ; sans formation, ils gardent leur poste.
    /// </summary>
    /// <exception cref="ArgumentException">Cette formation n'existe pas pour le format du match.</exception>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    public async Task SetFormationAsync(
        Guid matchId, TeamSide side, string? formationCode, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side));
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var lineup = await RequireLineupAsync(matchId, cancellationToken);
            var formation = FormationCatalog.Find(
                lineup.Match.SportCode, lineup.Match.PlayersPerSide, formationCode);
            if (formationCode is not null && formation is null)
            {
                throw new ArgumentException(
                    $"Formation inconnue pour ce format : « {formationCode} ».", nameof(formationCode));
            }

            var team = lineup.Team(side);
            var previous = team.OnPitch.ToDictionary(entry => entry.Value.Id, entry => entry.Key);
            var candidates = team.Players
                .OrderBy(player => previous.GetValueOrDefault(player.Id, int.MaxValue))
                .Select(player => new SlotCandidate(
                    player.Id, team.Positions.GetValueOrDefault(player.Id), previous.ContainsKey(player.Id)))
                .ToList();
            var placed = formation?.Distribute(candidates) ?? new Dictionary<Guid, int>();
            var members = team.Players.Select(player => player.Id).ToHashSet();

            foreach (var participant in await RegistrationsAsync(matchId, cancellationToken))
            {
                var playerId = participant.PlayerId;
                if (!members.Contains(playerId))
                {
                    continue;
                }

                if (formation is not null && placed.TryGetValue(playerId, out var slot))
                {
                    await MoveAsync(participant, side, slot, formation.Slots[slot].PositionCode, cancellationToken);
                }
                else
                {
                    // Un joueur qui perd sa place dans la nouvelle formation perd aussi le poste qui allait avec.
                    var losesPlace = formation is not null && previous.ContainsKey(playerId);
                    var position = losesPlace ? null : team.Positions.GetValueOrDefault(playerId);
                    await MoveAsync(participant, side, slot: null, position, cancellationToken);
                }
            }

            await SaveSideAsync(matchId, side, settings => settings.FormationCode = formation?.Code, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>
    /// Place un joueur sur un emplacement, ou le libère (<paramref name="playerId"/> null). Un joueur non assigné
    /// rejoint l'équipe. L'occupant précédent sort du terrain ; si le nouveau venu était déjà sur le terrain, les
    /// deux échangent leurs places.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Cet emplacement n'existe pas dans la formation.</exception>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    /// <exception cref="InvalidOperationException">
    /// L'équipe n'a pas de formation, ou le joueur n'est pas inscrit au match ou joue dans l'autre équipe.
    /// </exception>
    public async Task PlaceAsync(
        Guid matchId, TeamSide side, int slot, Guid? playerId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side));
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var lineup = await RequireLineupAsync(matchId, cancellationToken);
            var team = lineup.Team(side);
            var formation = team.Formation
                ?? throw new InvalidOperationException("Cette équipe n'a pas de formation.");
            if (slot < 0 || slot >= formation.Slots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            var registrations = await RegistrationsAsync(matchId, cancellationToken);
            int? origin = null;
            if (playerId is { } incoming)
            {
                if (registrations.All(participant => participant.PlayerId != incoming))
                {
                    throw new InvalidOperationException("Ce joueur n'est pas inscrit à ce match.");
                }

                var opponents = lineup.Team(side == TeamSide.A ? TeamSide.B : TeamSide.A);
                if (opponents.Players.Any(player => player.Id == incoming))
                {
                    throw new InvalidOperationException("Ce joueur est dans l'autre équipe.");
                }

                origin = team.OnPitch
                    .Where(entry => entry.Value.Id == incoming)
                    .Select(entry => (int?)entry.Key)
                    .FirstOrDefault();
                if (origin == slot)
                {
                    return;
                }
            }

            // L'occupant s'en va d'abord, puis le nouveau venu arrive.
            var occupant = team.OnPitch.GetValueOrDefault(slot);
            foreach (var participant in registrations.Where(participant =>
                participant.Side == side && participant.Slot == slot && participant.PlayerId != playerId))
            {
                if (origin is { } free && participant.PlayerId == occupant?.Id)
                {
                    await MoveAsync(participant, side, free, formation.Slots[free].PositionCode, cancellationToken);
                }
                else
                {
                    await MoveAsync(participant, side, slot: null, positionCode: null, cancellationToken);
                }
            }

            foreach (var participant in registrations.Where(participant => participant.PlayerId == playerId))
            {
                await MoveAsync(participant, side, slot, formation.Slots[slot].PositionCode, cancellationToken);
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>
    /// Donne un poste à un joueur dont l'équipe n'a pas de formation, ou le lui retire
    /// (<paramref name="positionCode"/> null).
    /// </summary>
    /// <exception cref="ArgumentException">Ce poste n'existe pas dans le sport du match.</exception>
    /// <exception cref="KeyNotFoundException">Le match est introuvable.</exception>
    /// <exception cref="InvalidOperationException">
    /// Le joueur n'est pas inscrit au match, ou son équipe a une formation : le poste vient alors de l'emplacement.
    /// </exception>
    public async Task SetPositionAsync(
        Guid matchId, Guid playerId, string? positionCode, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var lineup = await RequireLineupAsync(matchId, cancellationToken);
            if (positionCode is not null
                && SportCatalog.Find(lineup.Match.SportCode)?.HasPosition(positionCode) != true)
            {
                throw new ArgumentException(
                    $"Le poste « {positionCode} » n'existe pas dans ce sport.", nameof(positionCode));
            }

            var registrations = (await RegistrationsAsync(matchId, cancellationToken))
                .Where(participant => participant.PlayerId == playerId)
                .ToList();
            if (registrations.Count == 0)
            {
                throw new InvalidOperationException("Ce joueur n'est pas inscrit à ce match.");
            }

            if (new[] { lineup.TeamA, lineup.TeamB }.Any(team =>
                team.Formation is not null && team.Players.Any(player => player.Id == playerId)))
            {
                throw new InvalidOperationException("Avec une formation, le poste vient de l'emplacement.");
            }

            foreach (var participant in registrations)
            {
                await MoveAsync(participant, participant.Side, slot: null, positionCode, cancellationToken);
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>Enregistre, ou efface (null), le score des deux équipes pour <see cref="ResultService"/>.</summary>
    internal async Task SetScoresAsync(
        Guid matchId, int? scoreA, int? scoreB, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await SaveSideAsync(matchId, TeamSide.A, settings => settings.Score = scoreA, cancellationToken);
            await SaveSideAsync(matchId, TeamSide.B, settings => settings.Score = scoreB, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task<List<MatchParticipant>> RegistrationsAsync(Guid matchId, CancellationToken cancellationToken) =>
        (await _participants.ListAsync(cancellationToken: cancellationToken))
            .Where(participant => participant.MatchId == matchId)
            .ToList();

    /// <summary>Enregistre l'équipe, l'emplacement et le poste d'une inscription, seulement s'ils changent.</summary>
    private async Task MoveAsync(
        MatchParticipant participant,
        TeamSide? side,
        int? slot,
        string? positionCode,
        CancellationToken cancellationToken)
    {
        if (participant.Side == side && participant.Slot == slot && participant.PositionCode == positionCode)
        {
            return;
        }

        participant.Side = side;
        participant.Slot = slot;
        participant.PositionCode = positionCode;
        await _participants.UpdateAsync(participant, cancellationToken);
    }

    /// <summary>Modifie les réglages d'une équipe, en les créant au premier enregistrement.</summary>
    private async Task SaveSideAsync(
        Guid matchId, TeamSide side, Action<MatchSide> change, CancellationToken cancellationToken)
    {
        var existing = (await _sides.ListAsync(cancellationToken: cancellationToken))
            .FirstOrDefault(candidate => candidate.MatchId == matchId && candidate.Side == side);
        var settings = existing
            ?? new MatchSide { MatchId = matchId, Side = side, ColorCode = TeamColors.DefaultFor(side) };
        change(settings);

        if (existing is null)
        {
            await _sides.AddAsync(settings, cancellationToken);
        }
        else
        {
            await _sides.UpdateAsync(settings, cancellationToken);
        }
    }

    private async Task<MatchLineup> RequireLineupAsync(Guid matchId, CancellationToken cancellationToken) =>
        await GetAsync(matchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {matchId}.");

    private async Task RequireMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        if (await _matches.GetAsync(matchId, cancellationToken: cancellationToken) is null)
        {
            throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {matchId}.");
        }
    }
}
