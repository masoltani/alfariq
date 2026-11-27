namespace AlFariq.UI.Resources;

/// <summary>Clés de traduction des sports et des postes du catalogue.</summary>
public static class SportLabels
{
    public static string SportKey(string sportCode) => $"Sport_{sportCode}";

    public static string PositionKey(string positionCode) => $"Position_{positionCode.Replace('.', '_')}";

    /// <summary>Clé de l'abréviation affichée sur le terrain (postes du football).</summary>
    public static string PositionShortKey(string positionCode) => $"PositionShort_{positionCode.Replace('.', '_')}";
}
