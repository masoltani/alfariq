using AlFariq.Domain;
using Microsoft.Extensions.Localization;

namespace AlFariq.UI.Resources;

/// <summary>Noms affichés des équipes et clés de traduction des couleurs.</summary>
public static class TeamLabels
{
    public static string ColorKey(string colorCode) => $"Color_{colorCode}";

    /// <summary>Le nom choisi, sinon « Équipe A » ou « Équipe B ».</summary>
    public static string Name(IStringLocalizer localizer, TeamSide side, string? customName)
    {
        if (!string.IsNullOrWhiteSpace(customName))
        {
            return customName;
        }

        return localizer[side == TeamSide.A ? "Teams_DefaultA" : "Teams_DefaultB"].Value;
    }
}
