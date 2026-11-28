using AlFariq.Domain;

namespace AlFariq.UI.Services;

/// <summary>
/// L'onglet et les filtres de la liste des matchs (US-013), gardés tant que l'application est ouverte : revenir de la
/// fiche d'un match retrouve la même recherche. Rien n'est enregistré sur l'appareil.
/// </summary>
public sealed class MatchListState
{
    public bool ShowPast { get; private set; }

    /// <summary>Statut choisi ; null pour tous.</summary>
    public MatchStatus? Status { get; set; }

    public string PlayerName { get; set; } = "";

    public DateOnly? Date { get; set; }

    public MatchCriteria Criteria => new(PlayerName, Date);

    /// <summary>Change d'onglet. Les statuts varient d'un onglet à l'autre : le filtre repart de « Tous ».</summary>
    public void Show(bool past)
    {
        ShowPast = past;
        Status = null;
    }

    public void ClearSearch()
    {
        PlayerName = "";
        Date = null;
    }
}
