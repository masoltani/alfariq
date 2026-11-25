using System.Globalization;

namespace AlFariq.Domain;

/// <summary>
/// Règles du répertoire sur les noms : recherche, tri et repérage des doublons.
/// La casse et les accents sont ignorés : « elodie » trouve « Élodie ».
/// </summary>
public static class PlayerDirectory
{
    private const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    private static readonly CompareInfo Names = CultureInfo.InvariantCulture.CompareInfo;

    private static readonly Comparer<string> NameOrder =
        Comparer<string>.Create((left, right) => Names.Compare(left, right, Loose));

    /// <summary>Retire les espaces au début et à la fin, et réduit les suites d'espaces à un seul.</summary>
    public static string CleanName(string? name) =>
        string.Join(' ', (name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Les joueurs dont le nom contient le texte cherché (tous si le texte est vide), triés par nom.</summary>
    public static IReadOnlyList<Player> Search(IEnumerable<Player> players, string? query)
    {
        var text = CleanName(query);
        return players
            .Where(player => text.Length == 0 || Names.IndexOf(player.Name, text, Loose) >= 0)
            .OrderBy(player => player.Name, NameOrder)
            .ThenBy(player => player.CreatedAt)
            .ToList();
    }

    /// <summary>Vrai si un autre joueur (archivé ou non) porte déjà ce nom.</summary>
    public static bool HasSameName(IEnumerable<Player> players, string? name, Guid? exceptId = null)
    {
        var wanted = CleanName(name);
        return wanted.Length > 0 && players.Any(player =>
            player.Id != exceptId && Names.Compare(CleanName(player.Name), wanted, Loose) == 0);
    }
}
