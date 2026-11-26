using System.Globalization;

namespace AlFariq.UI.Resources;

/// <summary>Mise en forme des dates de match dans la langue d'affichage.</summary>
public static class MatchLabels
{
    public static string LongDate(DateTime local) => local.ToString("dddd d MMMM yyyy", CultureInfo.CurrentUICulture);

    public static string Time(DateTime local) => local.ToString("HH:mm", CultureInfo.CurrentUICulture);
}
