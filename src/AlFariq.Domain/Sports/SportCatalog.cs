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
        Create(FootballCode, [5, 7, 11], "goalkeeper", "defender", "midfielder", "forward"),
        Create("futsal", [5], "goalkeeper", "defender", "winger", "pivot"),
        Create("basketball", [5, 3], "guard", "forward", "center"),
        Create("handball", [7], "goalkeeper", "winger", "back", "pivot"),
        Create("volleyball", [6, 4, 2], "setter", "outside", "opposite", "middle", "libero"),
    ];

    public static Sport Default => All[0];

    /// <summary>Le sport portant ce code, ou null s'il est inconnu.</summary>
    public static Sport? Find(string? code) => All.FirstOrDefault(sport => sport.Code == code);

    /// <summary>Vrai si ce poste existe dans l'un des sports du catalogue.</summary>
    public static bool IsKnownPosition(string? positionCode) => All.Any(sport => sport.HasPosition(positionCode));

    private static Sport Create(string code, int[] formats, params string[] positions) =>
        new(code, positions.Select(position => $"{code}.{position}").ToArray(), formats);
}
