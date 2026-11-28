using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Saisie d'un match. La date, l'heure et le format sont obligatoires.</summary>
public sealed record MatchInput(
    DateOnly Date,
    TimeOnly Time,
    int PlayersPerSide,
    string SportCode = SportCatalog.FootballCode,
    int DurationMinutes = Match.DefaultDurationMinutes,
    string? Location = null,
    string? Notes = null);

/// <summary>Création, modification, duplication, suppression (US-006) et annulation (US-012) des matchs.</summary>
public sealed class MatchService
{
    private readonly IRepository<Match> _matches;
    private readonly IRepository<MatchParticipant> _participants;
    private readonly IRepository<Player> _players;
    private readonly TimeProvider _clock;

    public MatchService(
        IRepository<Match> matches,
        IRepository<MatchParticipant> participants,
        IRepository<Player> players,
        TimeProvider clock)
    {
        _matches = matches;
        _participants = participants;
        _players = players;
        _clock = clock;
    }

    /// <summary>Tous les matchs non supprimés.</summary>
    public Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default) =>
        _matches.ListAsync(cancellationToken: cancellationToken);

    public Task<Match?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        _matches.GetAsync(id, cancellationToken: cancellationToken);

    /// <summary>La saisie correspondant à un match existant, pour le modifier ; null s'il est introuvable.</summary>
    public async Task<MatchInput?> GetInputAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await FindAsync(id, cancellationToken);
        return match is null ? null : ToInput(match);
    }

    /// <summary>
    /// La saisie préremplie pour dupliquer un match : mêmes sport, format, durée, heure et lieu, sept jours plus tard.
    /// Les notes ne sont pas reprises. Rien n'est enregistré ; null si le match est introuvable.
    /// </summary>
    public async Task<MatchInput?> GetDuplicateInputAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var input = await GetInputAsync(sourceId, cancellationToken);
        return input is null ? null : input with { Date = input.Date.AddDays(7), Notes = null };
    }

    /// <summary>Crée un match à l'état « Planifié ».</summary>
    /// <param name="input">La saisie.</param>
    /// <param name="duplicatedFrom">Match d'origine d'une duplication : ses joueurs non archivés sont repris.</param>
    /// <param name="cancellationToken">Annulation.</param>
    /// <exception cref="ArgumentException">Saisie invalide.</exception>
    public async Task<Match> CreateAsync(
        MatchInput input, Guid? duplicatedFrom = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var match = new Match();
        Apply(match, input);
        match = await _matches.AddAsync(match, cancellationToken);

        if (duplicatedFrom is { } sourceId)
        {
            await CopyPlayersAsync(sourceId, match.Id, cancellationToken);
        }

        return match;
    }

    /// <exception cref="ArgumentException">Saisie invalide.</exception>
    /// <exception cref="KeyNotFoundException">Le match n'existe pas ou est supprimé.</exception>
    /// <exception cref="InvalidOperationException">Le sport ou le format d'un match terminé a changé.</exception>
    public async Task<Match> UpdateAsync(Guid id, MatchInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var match = await RequireAsync(id, cancellationToken);
        if (match.Status == MatchStatus.Finished
            && (input.SportCode != match.SportCode || input.PlayersPerSide != match.PlayersPerSide))
        {
            // RG-MAT-02.
            throw new InvalidOperationException("Le sport et le format d'un match terminé ne changent plus.");
        }

        Apply(match, input);
        return await _matches.UpdateAsync(match, cancellationToken);
    }

    /// <summary>Annule un match planifié, avec un motif facultatif. Il reste dans les listes.</summary>
    /// <exception cref="ArgumentException">Motif trop long.</exception>
    /// <exception cref="KeyNotFoundException">Le match n'existe pas ou est supprimé.</exception>
    /// <exception cref="InvalidOperationException">Le match est déjà annulé ou terminé.</exception>
    public async Task<Match> CancelAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var cleaned = Optional(reason, Match.MaxCancellationReasonLength, "Le motif");
        var match = await RequireAsync(id, cancellationToken);
        if (match.Status != MatchStatus.Planned)
        {
            throw new InvalidOperationException("Seul un match planifié peut être annulé.");
        }

        match.Status = MatchStatus.Cancelled;
        match.CancellationReason = cleaned;
        return await _matches.UpdateAsync(match, cancellationToken);
    }

    /// <summary>Rétablit un match annulé : il redevient « Planifié ».</summary>
    /// <exception cref="KeyNotFoundException">Le match n'existe pas ou est supprimé.</exception>
    /// <exception cref="InvalidOperationException">Le match n'est pas annulé.</exception>
    public async Task<Match> ReinstateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await RequireAsync(id, cancellationToken);
        if (match.Status != MatchStatus.Cancelled)
        {
            throw new InvalidOperationException("Ce match n'est pas annulé.");
        }

        match.Status = MatchStatus.Planned;
        match.CancellationReason = null;
        return await _matches.UpdateAsync(match, cancellationToken);
    }

    /// <summary>Suppression logique. Renvoie false si le match n'existe pas ou est déjà supprimé.</summary>
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _matches.DeleteAsync(id, cancellationToken);

    private async Task<Match> RequireAsync(Guid id, CancellationToken cancellationToken) =>
        await FindAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Aucun match actif avec l'identifiant {id}.");

    private MatchInput ToInput(Match match)
    {
        var local = MatchSchedule.ToLocal(match.StartsAt, _clock.LocalTimeZone);
        return new MatchInput(
            DateOnly.FromDateTime(local),
            TimeOnly.FromDateTime(local),
            match.PlayersPerSide,
            match.SportCode,
            match.DurationMinutes,
            match.Location,
            match.Notes);
    }

    private async Task CopyPlayersAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken)
    {
        var available = (await _players.ListAsync(cancellationToken: cancellationToken))
            .Where(player => !player.IsArchived)
            .Select(player => player.Id)
            .ToHashSet();
        var participants = await _participants.ListAsync(cancellationToken: cancellationToken);

        foreach (var playerId in participants
            .Where(participant => participant.MatchId == sourceId && available.Contains(participant.PlayerId))
            .Select(participant => participant.PlayerId)
            .Distinct())
        {
            await _participants.AddAsync(
                new MatchParticipant { MatchId = targetId, PlayerId = playerId }, cancellationToken);
        }
    }

    private void Apply(Match match, MatchInput input)
    {
        if (input.Date.Year is < Match.MinYear or > Match.MaxYear)
        {
            throw new ArgumentException("La date est hors des limites.", nameof(input));
        }

        var sport = SportCatalog.Find(input.SportCode)
            ?? throw new ArgumentException($"Sport inconnu : « {input.SportCode} ».", nameof(input));

        if (input.PlayersPerSide is < Match.MinPlayersPerSide or > Match.MaxPlayersPerSide)
        {
            throw new ArgumentException(
                $"Le format va de {Match.MinPlayersPerSide} à {Match.MaxPlayersPerSide} joueurs par équipe.",
                nameof(input));
        }

        if (input.DurationMinutes is < Match.MinDurationMinutes or > Match.MaxDurationMinutes)
        {
            throw new ArgumentException(
                $"La durée va de {Match.MinDurationMinutes} à {Match.MaxDurationMinutes} minutes.", nameof(input));
        }

        var location = Optional(input.Location, Match.MaxLocationLength, "Le lieu");
        var notes = Optional(input.Notes, Match.MaxNotesLength, "La note");

        match.StartsAt = MatchSchedule.At(input.Date, input.Time, _clock.LocalTimeZone);
        match.SportCode = sport.Code;
        match.PlayersPerSide = input.PlayersPerSide;
        match.DurationMinutes = input.DurationMinutes;
        match.Location = location;
        match.Notes = notes;
    }

    /// <summary>Texte facultatif : null s'il est vide.</summary>
    private static string? Optional(string? text, int maxLength, string label)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length <= maxLength
            ? trimmed
            : throw new ArgumentException($"{label} dépasse {maxLength} caractères.", nameof(text));
    }
}
