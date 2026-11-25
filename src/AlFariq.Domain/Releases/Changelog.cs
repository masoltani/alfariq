using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AlFariq.Domain;

/// <summary>
/// Journal des changements : versions de la plus récente à la plus ancienne, textes en français (obligatoire),
/// anglais et arabe. Source unique : <c>Releases/changelog.json</c>, embarqué dans l'assembly (lisible hors ligne).
/// </summary>
public sealed class Changelog
{
    private const string ResourceName = "AlFariq.Domain.changelog.json";

    public Changelog(IReadOnlyList<ChangelogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries;
    }

    public IReadOnlyList<ChangelogEntry> Entries { get; }

    /// <summary>Entrée la plus récente, ou null si le journal est vide.</summary>
    public ChangelogEntry? Latest => Entries.Count > 0 ? Entries[0] : null;

    /// <summary>Entrées strictement plus récentes que <paramref name="lastSeen"/>, jusqu'à <paramref name="current"/> incluse.</summary>
    public IReadOnlyList<ChangelogEntry> EntriesSince(AppVersion lastSeen, AppVersion current) =>
        Entries.Where(e => e.Version > lastSeen && e.Version <= current).ToList();

    /// <summary>
    /// Vrai s'il faut annoncer des nouveautés : une version a déjà été vue (pas le premier lancement), l'appli est plus
    /// récente, et le journal contient quelque chose entre les deux. Un numéro mémorisé illisible ne déclenche rien.
    /// </summary>
    public bool HasNewsSince(string? lastSeenVersion, AppVersion current) =>
        AppVersion.TryParse(lastSeenVersion, out var lastSeen) && EntriesSince(lastSeen, current).Count > 0;

    /// <summary>Charge le journal embarqué.</summary>
    /// <exception cref="InvalidOperationException">La ressource est absente de l'assembly.</exception>
    /// <exception cref="FormatException">Le journal est invalide.</exception>
    public static Changelog LoadEmbedded()
    {
        using var stream = typeof(Changelog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Ressource embarquée introuvable : {ResourceName}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Analyse et valide un journal au format JSON (voir <c>changelog.json</c>).</summary>
    /// <exception cref="FormatException">Le contenu est invalide ; le message dit quoi et où.</exception>
    public static Changelog Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new FormatException($"Journal des changements illisible : {exception.Message}", exception);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("entries", out var entriesElement)
                || entriesElement.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("Le journal doit être un objet contenant un tableau « entries ».");
            }

            var entries = new List<ChangelogEntry>();
            foreach (var element in entriesElement.EnumerateArray())
            {
                var entry = ParseEntry(element);
                if (entries.Count > 0 && entry.Version >= entries[^1].Version)
                {
                    throw new FormatException(
                        $"Les versions doivent être uniques et classées de la plus récente à la plus ancienne : {entry.Version} suit {entries[^1].Version}.");
                }

                entries.Add(entry);
            }

            return new Changelog(entries);
        }
    }

    private static ChangelogEntry ParseEntry(JsonElement element)
    {
        var versionText = RequireString(element, "version", "une entrée");
        if (!AppVersion.TryParse(versionText, out var version))
        {
            throw new FormatException($"Version invalide : « {versionText} » (attendu : MAJEUR.MINEUR.CORRECTIF).");
        }

        var where = $"la version {versionText}";
        var dateText = RequireString(element, "date", where);
        if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new FormatException($"Date invalide pour {where} : « {dateText} » (attendu : AAAA-MM-JJ).");
        }

        if (!element.TryGetProperty("changes", out var changesElement)
            || changesElement.ValueKind != JsonValueKind.Array
            || changesElement.GetArrayLength() == 0)
        {
            throw new FormatException($"La version {versionText} doit contenir au moins un changement (« changes »).");
        }

        var changes = new List<ChangelogChange>();
        foreach (var changeElement in changesElement.EnumerateArray())
        {
            changes.Add(ParseChange(changeElement, where));
        }

        return new ChangelogEntry(version, date, changes);
    }

    private static ChangelogChange ParseChange(JsonElement element, string where)
    {
        var typeText = RequireString(element, "type", where);
        var type = typeText switch
        {
            "added" => ChangeType.Added,
            "changed" => ChangeType.Changed,
            "fixed" => ChangeType.Fixed,
            _ => throw new FormatException($"Type de changement inconnu pour {where} : « {typeText} » (attendu : added, changed ou fixed)."),
        };

        if (!element.TryGetProperty("text", out var textElement) || textElement.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"Un changement de {where} n'a pas de « text » par langue.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in textElement.EnumerateObject())
        {
            if (!ChangelogLanguages.IsSupported(property.Name))
            {
                throw new FormatException(
                    $"Langue inconnue pour {where} : « {property.Name} » (langues : {string.Join(", ", ChangelogLanguages.Supported)}).");
            }

            var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException($"Texte vide ou invalide pour {where} en « {property.Name} ».");
            }

            values[property.Name] = value.Trim();
        }

        if (!values.ContainsKey(ChangelogLanguages.Default))
        {
            throw new FormatException($"Le texte français est obligatoire pour chaque changement ({where}).");
        }

        return new ChangelogChange(type, new LocalizedText(values));
    }

    private static string RequireString(JsonElement element, string name, string where)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(property.GetString()))
        {
            return property.GetString()!.Trim();
        }

        throw new FormatException($"Champ « {name} » manquant ou vide pour {where}.");
    }
}
