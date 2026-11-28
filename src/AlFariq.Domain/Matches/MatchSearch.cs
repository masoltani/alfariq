namespace AlFariq.Domain;

/// <summary>Un match et les joueurs qui y sont inscrits.</summary>
public sealed record MatchRecord(Match Match, IReadOnlyList<Player> Players);

/// <summary>Ce que l'on cherche dans l'historique ; un critère vide ne filtre rien.</summary>
/// <param name="PlayerName">Tout ou partie du nom d'un joueur inscrit.</param>
/// <param name="Date">Le jour du match, dans le fuseau d'affichage.</param>
public sealed record MatchCriteria(string? PlayerName = null, DateOnly? Date = null)
{
    public bool IsEmpty => PlayerDirectory.CleanName(PlayerName).Length == 0 && Date is null;
}

/// <summary>Un match trouvé.</summary>
/// <param name="Match">Le match.</param>
/// <param name="NamedPlayers">Les joueurs dont le nom correspond, triés par nom ; vide sans nom cherché.</param>
public sealed record FoundMatch(Match Match, IReadOnlyList<Player> NamedPlayers);

/// <summary>Retrouver un match (US-013) : par date, par nom de joueur, puis par statut.</summary>
public static class MatchSearch
{
    /// <summary>Les matchs qui répondent à tous les critères, dans l'ordre reçu.</summary>
    /// <param name="records">Les matchs et leurs joueurs.</param>
    /// <param name="criteria">Ce que l'on cherche.</param>
    /// <param name="zone">Le fuseau dans lequel les dates sont affichées.</param>
    public static IReadOnlyList<FoundMatch> Find(
        IEnumerable<MatchRecord> records, MatchCriteria criteria, TimeZoneInfo zone)
    {
        var name = PlayerDirectory.CleanName(criteria.PlayerName);
        var found = new List<FoundMatch>();
        foreach (var record in records)
        {
            if (criteria.Date is { } date
                && DateOnly.FromDateTime(MatchSchedule.ToLocal(record.Match.StartsAt, zone)) != date)
            {
                continue;
            }

            if (name.Length == 0)
            {
                found.Add(new FoundMatch(record.Match, []));
                continue;
            }

            var named = PlayerDirectory.Search(record.Players, name);
            if (named.Count > 0)
            {
                found.Add(new FoundMatch(record.Match, named));
            }
        }

        return found;
    }

    /// <summary>Les statuts présents dans une liste : eux seuls sont proposés en filtre.</summary>
    public static IReadOnlyList<MatchStatus> Statuses(IEnumerable<Match> matches) =>
        matches.Select(match => match.Status).Distinct().Order().ToList();

    /// <summary>Les matchs d'un statut (tous si aucun statut n'est choisi), dans l'ordre reçu.</summary>
    public static IReadOnlyList<Match> WithStatus(IEnumerable<Match> matches, MatchStatus? status) =>
        matches.Where(match => status is null || match.Status == status).ToList();
}
