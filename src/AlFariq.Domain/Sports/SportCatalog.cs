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
        Create(
            FootballCode,
            [5, 7, 11],
            "goalkeeper",
            "defender", "centre_back", "left_back", "right_back",
            "midfielder", "defensive_midfielder", "central_midfielder", "attacking_midfielder",
            "left_midfielder", "right_midfielder",
            "forward", "striker", "left_winger", "right_winger"),
        Create("futsal", [5], "goalkeeper", "defender", "winger", "pivot"),
        Create("basketball", [5, 3], "guard", "forward", "center"),
        Create("handball", [7], "goalkeeper", "winger", "back", "pivot"),
        Create("volleyball", [6, 4, 2], "setter", "outside", "opposite", "middle", "libero"),
    ];

    // Variante → poste général. Un poste absent de cette table est lui-même un poste général.
    private static readonly Dictionary<string, string> BasePositions = new()
    {
        ["football.centre_back"] = "football.defender",
        ["football.left_back"] = "football.defender",
        ["football.right_back"] = "football.defender",
        ["football.defensive_midfielder"] = "football.midfielder",
        ["football.central_midfielder"] = "football.midfielder",
        ["football.attacking_midfielder"] = "football.midfielder",
        ["football.left_midfielder"] = "football.midfielder",
        ["football.right_midfielder"] = "football.midfielder",
        ["football.striker"] = "football.forward",
        ["football.left_winger"] = "football.forward",
        ["football.right_winger"] = "football.forward",
    };

    public static Sport Default => All[0];

    /// <summary>Le poste général d'une variante (« arrière gauche » → « défenseur ») ; sinon le poste lui-même.</summary>
    public static string BaseOf(string positionCode) => BasePositions.GetValueOrDefault(positionCode, positionCode);

    /// <summary>Vrai si le score se compte en buts : on peut alors désigner les buteurs (US-012).</summary>
    public static bool CountsGoals(string? sportCode) => sportCode is FootballCode or "futsal" or "handball";

    /// <summary>Le sport portant ce code, ou null s'il est inconnu.</summary>
    public static Sport? Find(string? code) => All.FirstOrDefault(sport => sport.Code == code);

    /// <summary>Vrai si ce poste existe dans l'un des sports du catalogue.</summary>
    public static bool IsKnownPosition(string? positionCode) => All.Any(sport => sport.HasPosition(positionCode));

    private static Sport Create(string code, int[] formats, params string[] positions) =>
        new(code, positions.Select(position => $"{code}.{position}").ToArray(), formats);
}
