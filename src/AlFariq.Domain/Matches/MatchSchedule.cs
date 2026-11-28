namespace AlFariq.Domain;

/// <summary>Règles sur les dates des matchs.</summary>
public static class MatchSchedule
{
    /// <summary>L'instant correspondant à une date et une heure saisies dans un fuseau horaire.</summary>
    public static DateTimeOffset At(DateOnly date, TimeOnly time, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(time);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    /// <summary>La date et l'heure d'un instant, vues d'un fuseau horaire.</summary>
    public static DateTime ToLocal(DateTimeOffset instant, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(instant, zone).DateTime;

    /// <summary>Les matchs en cours ou à venir, le plus proche d'abord.</summary>
    public static IReadOnlyList<Match> Upcoming(IEnumerable<Match> matches, DateTimeOffset now) =>
        matches.Where(match => !match.IsPast(now))
            .OrderBy(match => match.StartsAt)
            .ThenBy(match => match.CreatedAt)
            .ToList();

    /// <summary>Les matchs passés, le plus récent d'abord.</summary>
    public static IReadOnlyList<Match> Past(IEnumerable<Match> matches, DateTimeOffset now) =>
        matches.Where(match => match.IsPast(now))
            .OrderByDescending(match => match.StartsAt)
            .ThenByDescending(match => match.CreatedAt)
            .ToList();
}
