namespace AlFariq.Domain;

public enum ChangeType
{
    /// <summary>Nouveauté.</summary>
    Added,

    /// <summary>Amélioration d'une fonctionnalité existante.</summary>
    Changed,

    /// <summary>Correction d'un défaut.</summary>
    Fixed,
}

/// <summary>Langues du journal. Le français est obligatoire et sert de repli ; l'anglais et l'arabe sont facultatifs.</summary>
public static class ChangelogLanguages
{
    public const string Default = "fr";

    public static IReadOnlyList<string> Supported { get; } = ["fr", "en", "ar"];

    public static bool IsSupported(string language) => Supported.Contains(language, StringComparer.Ordinal);

    /// <summary>Les langues écrites de droite à gauche (arabe pour l'instant).</summary>
    public static bool IsRightToLeft(string language) => language == "ar";

    /// <summary>Langue de base d'un nom de culture : « fr-FR » devient « fr ». Vide ou inconnu : français.</summary>
    public static string FromCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return Default;
        }

        var dash = cultureName.IndexOf('-');
        var language = (dash >= 0 ? cultureName[..dash] : cultureName).Trim().ToLowerInvariant();
        return IsSupported(language) ? language : Default;
    }
}

/// <summary>Texte d'un changement et la langue dans laquelle il est effectivement fourni.</summary>
/// <param name="Text">Le texte à afficher.</param>
/// <param name="Language">Sa langue (« fr », « en » ou « ar ») : à reporter dans l'attribut <c>lang</c>.</param>
public readonly record struct ResolvedText(string Text, string Language)
{
    /// <summary>« rtl » ou « ltr » : à reporter dans l'attribut <c>dir</c>.</summary>
    public string Direction => ChangelogLanguages.IsRightToLeft(Language) ? "rtl" : "ltr";
}

/// <summary>Un même texte dans plusieurs langues. Le français est toujours présent.</summary>
public sealed class LocalizedText
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public LocalizedText(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (!values.ContainsKey(ChangelogLanguages.Default))
        {
            throw new ArgumentException("Le texte français est obligatoire.", nameof(values));
        }

        _values = values;
    }

    public IReadOnlyDictionary<string, string> Values => _values;

    /// <summary>Texte dans la langue de la culture ; à défaut, texte français (avec sa propre langue indiquée).</summary>
    public ResolvedText Resolve(string? cultureName)
    {
        var language = ChangelogLanguages.FromCulture(cultureName);
        return _values.TryGetValue(language, out var text)
            ? new ResolvedText(text, language)
            : new ResolvedText(_values[ChangelogLanguages.Default], ChangelogLanguages.Default);
    }
}

public sealed record ChangelogChange(ChangeType Type, LocalizedText Text);

public sealed record ChangelogEntry(AppVersion Version, DateOnly Date, IReadOnlyList<ChangelogChange> Changes);
