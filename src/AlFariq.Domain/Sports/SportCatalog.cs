namespace AlFariq.Domain;

/// <summary>
/// Catalogue des sports livré avec l'application. Pour ajouter un sport ou un poste : l'ajouter ici,
/// puis ajouter son libellé dans les traductions (un test signale tout libellé manquant).
/// </summary>
public static class SportCatalog
{
    public const string FootballCode = "football";

    /// <summary>Les sports proposés ; le premier est le sport par défaut.</summary>
    public static IReadOnlyList<Sport> All { get; } =
    [
        Create(FootballCode, "goalkeeper", "defender", "midfielder", "forward"),
        Create("futsal", "goalkeeper", "defender", "winger", "pivot"),
        Create("basketball", "guard", "forward", "center"),
        Create("handball", "goalkeeper", "winger", "back", "pivot"),
        Create("volleyball", "setter", "outside", "opposite", "middle", "libero"),
    ];

    public static Sport Default => All[0];

    /// <summary>Le sport portant ce code, ou null s'il est inconnu.</summary>
    public static Sport? Find(string? code) => All.FirstOrDefault(sport => sport.Code == code);

    private static Sport Create(string code, params string[] positions) =>
        new(code, positions.Select(position => $"{code}.{position}").ToArray());
}
