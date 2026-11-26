namespace AlFariq.Domain;

/// <summary>
/// Réglages d'une équipe pour un match (US-008). Elle n'est enregistrée qu'une fois personnalisée :
/// sans elle, le nom et la couleur par défaut s'appliquent.
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
}
