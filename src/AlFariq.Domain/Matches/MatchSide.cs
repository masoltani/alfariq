namespace AlFariq.Domain;

/// <summary>
/// Réglages d'une équipe pour un match (US-008, US-010). Ils ne sont enregistrés qu'une fois personnalisés :
/// sans eux, le nom et la couleur par défaut s'appliquent, sans formation.
/// </summary>
public sealed class MatchSide : Entity
{
    public const int MaxNameLength = 30;

    public Guid MatchId { get; set; }

    public TeamSide Side { get; set; }

    /// <summary>Nom choisi ; null pour le nom par défaut.</summary>
    public string? Name { get; set; }

    /// <summary>Code de <see cref="TeamColors"/>.</summary>
    public string ColorCode { get; set; } = "";

    /// <summary>Code d'une formation de <see cref="FormationCatalog"/> ; null sans formation (US-010).</summary>
    public string? FormationCode { get; set; }
}
