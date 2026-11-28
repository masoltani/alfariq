namespace AlFariq.Domain;

/// <summary>La participation d'un joueur à un match, dans une des équipes.</summary>
public sealed record Appearance(Guid PlayerId, TeamSide Side, int Goals = 0, int Assists = 0);

/// <summary>Un match, son score et les joueurs de ses équipes (RG-STA-04).</summary>
public sealed record FinishedMatch(Match Match, MatchScore Score, IReadOnlyList<Appearance> Appearances);

/// <summary>Les statistiques d'un joueur sur une période (RG-STA-02).</summary>
public sealed record PlayerStats(
    Guid PlayerId,
    int Played = 0,
    int Wins = 0,
    int Draws = 0,
    int Losses = 0,
    int Goals = 0,
    int Assists = 0);

/// <summary>Les statistiques d'une période.</summary>
/// <param name="Matches">Les matchs terminés pris en compte.</param>
/// <param name="MatchesWithoutTeams">Les matchs terminés sans joueur en équipe, non comptés.</param>
/// <param name="Players">Les joueurs ayant joué au moins un de ces matchs, par identifiant.</param>
public sealed record StatsSummary(
    int Matches, int MatchesWithoutTeams, IReadOnlyDictionary<Guid, PlayerStats> Players)
{
    /// <summary>Les statistiques d'un joueur ; tout à zéro s'il n'a pas joué.</summary>
    public PlayerStats For(Guid playerId) => Players.GetValueOrDefault(playerId) ?? new PlayerStats(playerId);

    /// <summary>La part des matchs comptés que le joueur a joués, en pour cent ; null sans aucun match.</summary>
    public int? AttendancePercent(Guid playerId) => Matches == 0
        ? null
        : (int)Math.Round(100.0 * For(playerId).Played / Matches, MidpointRounding.AwayFromZero);
}

/// <summary>Ce qui classe les joueurs.</summary>
public enum RankingCriterion
{
    Wins,
    Goals,
}

/// <summary>Un joueur et sa place au classement ; deux joueurs à égalité partagent la même.</summary>
public sealed record RankedPlayer(int Rank, Player Player, PlayerStats Stats);

/// <summary>
/// Statistiques de base (US-014). Elles sont toujours calculées à partir de l'historique, jamais enregistrées
/// (RG-STA-01), et seuls les matchs « Terminés » comptent.
/// </summary>
public static class StatsCalculator
{
    /// <summary>Les statistiques des matchs terminés d'une période.</summary>
    /// <param name="matches">Les matchs et les joueurs de leurs équipes.</param>
    /// <param name="period">La période.</param>
    /// <param name="zone">Le fuseau dans lequel les dates sont affichées.</param>
    public static StatsSummary Compute(IEnumerable<FinishedMatch> matches, StatsPeriod period, TimeZoneInfo zone)
    {
        var players = new Dictionary<Guid, PlayerStats>();
        var counted = 0;
        var withoutTeams = 0;
        foreach (var (match, score, appearances) in matches)
        {
            if (match.Status != MatchStatus.Finished
                || !period.Contains(DateOnly.FromDateTime(MatchSchedule.ToLocal(match.StartsAt, zone))))
            {
                continue;
            }

            if (appearances.Count == 0)
            {
                withoutTeams++;
                continue;
            }

            counted++;
            foreach (var appearance in appearances)
            {
                var stats = players.GetValueOrDefault(appearance.PlayerId) ?? new PlayerStats(appearance.PlayerId);
                players[appearance.PlayerId] = stats with
                {
                    Played = stats.Played + 1,
                    Wins = stats.Wins + (score.Winner == appearance.Side ? 1 : 0),
                    Draws = stats.Draws + (score.Winner is null ? 1 : 0),
                    Losses = stats.Losses + (score.Winner is { } winner && winner != appearance.Side ? 1 : 0),
                    Goals = stats.Goals + appearance.Goals,
                    Assists = stats.Assists + appearance.Assists,
                };
            }
        }

        return new StatsSummary(counted, withoutTeams, players);
    }

    /// <summary>
    /// Le classement des joueurs ayant joué : par victoires ou par buts, puis à égalité celui qui a eu besoin du moins
    /// de matchs. Les ex æquo partagent leur place et sont présentés par nom.
    /// </summary>
    public static IReadOnlyList<RankedPlayer> Rank(
        StatsSummary summary, IEnumerable<Player> players, RankingCriterion criterion)
    {
        int ValueOf(PlayerStats stats) => criterion == RankingCriterion.Goals ? stats.Goals : stats.Wins;

        // Le tri conserve l'ordre des noms entre ex æquo.
        var ordered = PlayerDirectory.Search(players, query: null)
            .Where(player => summary.Players.ContainsKey(player.Id))
            .OrderByDescending(player => ValueOf(summary.Players[player.Id]))
            .ThenBy(player => summary.Players[player.Id].Played);

        var ranking = new List<RankedPlayer>();
        foreach (var player in ordered)
        {
            var stats = summary.Players[player.Id];
            var previous = ranking.LastOrDefault();
            var tied = previous is not null
                && ValueOf(previous.Stats) == ValueOf(stats)
                && previous.Stats.Played == stats.Played;
            ranking.Add(new RankedPlayer(tied ? previous!.Rank : ranking.Count + 1, player, stats));
        }

        return ranking;
    }
}
