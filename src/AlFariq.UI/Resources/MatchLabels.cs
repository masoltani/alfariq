using System.Globalization;
using AlFariq.Domain;

namespace AlFariq.UI.Resources;

/// <summary>Mise en forme des dates de match dans la langue d'affichage, et clés de traduction des statuts.</summary>
public static class MatchLabels
{
    public static string LongDate(DateTime local) => local.ToString("dddd d MMMM yyyy", CultureInfo.CurrentUICulture);

    public static string StatusKey(MatchStatus status) => $"Match_Status{status}";

    public static string Time(DateTime local) => local.ToString("HH:mm", CultureInfo.CurrentUICulture);
}
