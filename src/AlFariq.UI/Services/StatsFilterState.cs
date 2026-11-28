using AlFariq.Domain;

namespace AlFariq.UI.Services;

/// <summary>
/// La période et le classement choisis (US-014), communs aux écrans de statistiques et gardés tant que l'application
/// est ouverte. Rien n'est enregistré sur l'appareil.
/// </summary>
public sealed class StatsFilterState
{
    public PeriodKind Period { get; set; } = PeriodKind.All;

    /// <summary>Début des dates au choix.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Fin des dates au choix.</summary>
    public DateOnly? To { get; set; }

    public RankingCriterion Ranking { get; set; } = RankingCriterion.Wins;

    public StatsPeriod PeriodAt(DateOnly today) => StatsPeriod.Of(Period, today, From, To);
}
