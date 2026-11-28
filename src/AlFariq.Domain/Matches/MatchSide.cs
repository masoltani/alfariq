namespace AlFariq.Domain;

/// <summary>
/// Réglages et score d'une équipe pour un match (US-008, US-010, US-012). Ils ne sont enregistrés qu'une fois
/// personnalisés ou le score saisi : sans eux, le nom et la couleur par défaut s'appliquent, sans formation.
/// </summary>
public sealed class MatchSide : Entity
{
    public const int MaxNameLength = 30;
    public const int MaxScore = 999;

    public Guid MatchId { get; set; }

    public TeamSide Side { get; set; }

    /// <summary>Nom choisi ; null pour le nom par défaut.</summary>
    public string? Name { get; set; }

    /// <summary>Code de <see cref="TeamColors"/>.</summary>
    public string ColorCode { get; set; } = "";

    /// <summary>Code d'une formation de <see cref="FormationCatalog"/> ; null sans formation (US-010).</summary>
    public string? FormationCode { get; set; }

    /// <summary>Score de l'équipe ; null tant qu'aucun résultat n'est saisi.</summary>
    public int? Score { get; set; }
}
