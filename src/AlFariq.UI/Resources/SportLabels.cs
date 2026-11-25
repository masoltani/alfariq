namespace AlFariq.UI.Resources;

/// <summary>Clés de traduction des sports et des postes du catalogue.</summary>
public static class SportLabels
{
    public static string SportKey(string sportCode) => $"Sport_{sportCode}";

    public static string PositionKey(string positionCode) => $"Position_{positionCode.Replace('.', '_')}";
}
