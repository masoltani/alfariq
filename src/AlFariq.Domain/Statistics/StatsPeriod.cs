namespace AlFariq.Domain;

/// <summary>Les périodes proposées pour les statistiques (US-014, RG-STA-05).</summary>
public enum PeriodKind
{
    All,
    Month,
    Season,
    Custom,
}

/// <summary>Une période en jours, bornes comprises ; une borne absente ne limite rien.</summary>
public sealed record StatsPeriod(DateOnly? From = null, DateOnly? To = null)
{
    /// <summary>La saison va du 1er juillet au 30 juin.</summary>
    public const int SeasonFirstMonth = 7;

    /// <summary>Vrai si le début suit la fin : aucun jour ne convient.</summary>
    public bool IsReversed => From > To;

    public bool Contains(DateOnly day) => (From is null || day >= From) && (To is null || day <= To);

    /// <summary>La période d'un choix, vue d'un jour donné.</summary>
    /// <param name="kind">Le choix.</param>
    /// <param name="today">Le jour qui situe le mois et la saison en cours.</param>
    /// <param name="from">Début, pour des dates au choix.</param>
    /// <param name="to">Fin, pour des dates au choix.</param>
    public static StatsPeriod Of(PeriodKind kind, DateOnly today, DateOnly? from = null, DateOnly? to = null) =>
        kind switch
        {
            PeriodKind.All => new StatsPeriod(),
            PeriodKind.Month => MonthOf(today),
            PeriodKind.Season => SeasonOf(today),
            PeriodKind.Custom => new StatsPeriod(from, to),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Période inconnue."),
        };

    private static StatsPeriod MonthOf(DateOnly day)
    {
        var first = new DateOnly(day.Year, day.Month, 1);
        return new StatsPeriod(first, first.AddMonths(1).AddDays(-1));
    }

    private static StatsPeriod SeasonOf(DateOnly day)
    {
        var year = day.Month >= SeasonFirstMonth ? day.Year : day.Year - 1;
        var first = new DateOnly(year, SeasonFirstMonth, 1);
        return new StatsPeriod(first, first.AddYears(1).AddDays(-1));
    }
}
