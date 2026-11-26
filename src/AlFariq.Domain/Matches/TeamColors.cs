namespace AlFariq.Domain;

/// <summary>
/// Couleurs proposées pour les équipes. Pour en ajouter une : l'ajouter ici, puis son libellé dans les traductions
/// et sa classe « team-color--… » dans la feuille de style (des tests signalent tout oubli).
/// </summary>
public static class TeamColors
{
    /// <summary>Codes enregistrés avec les données (ne jamais les renommer).</summary>
    public static IReadOnlyList<string> All { get; } =
        ["blue", "red", "green", "yellow", "orange", "purple", "white", "black"];

    public static string DefaultFor(TeamSide side) => side == TeamSide.A ? "blue" : "red";

    public static bool IsKnown(string? code) => code is not null && All.Contains(code);
}
