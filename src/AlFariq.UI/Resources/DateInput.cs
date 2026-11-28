using System.Globalization;

namespace AlFariq.UI.Resources;

/// <summary>La valeur d'un champ <c>type="date"</c>, toujours au format année-mois-jour.</summary>
public static class DateInput
{
    private const string Format = "yyyy-MM-dd";

    public static string Value(DateOnly? date) => date?.ToString(Format, CultureInfo.InvariantCulture) ?? "";

    /// <summary>La date saisie ; null si le champ est vide ou incomplet.</summary>
    public static DateOnly? Parse(object? value) =>
        DateOnly.TryParseExact(
            value as string, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
