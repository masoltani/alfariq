using System.Text.RegularExpressions;

namespace AlFariq.Domain;

/// <summary>
/// Version sémantique « MAJEUR.MINEUR.CORRECTIF » avec pré-version facultative (« 1.0.0-beta.1 »).
/// Les métadonnées de build (« +abc123 ») sont ignorées. Comparable selon les règles de semver.org.
/// </summary>
public readonly partial record struct AppVersion(int Major, int Minor, int Patch, string? Prerelease = null)
    : IComparable<AppVersion>
{
    [GeneratedRegex(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$")]
    private static partial Regex Pattern();

    public static bool TryParse(string? text, out AppVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        var plus = trimmed.IndexOf('+');
        if (plus >= 0)
        {
            trimmed = trimmed[..plus];
        }

        var match = Pattern().Match(trimmed);
        if (!match.Success
            || !int.TryParse(match.Groups[1].Value, out var major)
            || !int.TryParse(match.Groups[2].Value, out var minor)
            || !int.TryParse(match.Groups[3].Value, out var patch))
        {
            return false;
        }

        var prerelease = match.Groups[4].Success ? match.Groups[4].Value : null;
        version = new AppVersion(major, minor, patch, prerelease);
        return true;
    }

    /// <exception cref="FormatException">Le texte n'est pas une version sémantique.</exception>
    public static AppVersion Parse(string? text) =>
        TryParse(text, out var version)
            ? version
            : throw new FormatException($"Version invalide : « {text} » (attendu : MAJEUR.MINEUR.CORRECTIF).");

    public int CompareTo(AppVersion other)
    {
        var result = Major.CompareTo(other.Major);
        if (result != 0)
        {
            return result;
        }

        result = Minor.CompareTo(other.Minor);
        if (result != 0)
        {
            return result;
        }

        result = Patch.CompareTo(other.Patch);
        return result != 0 ? result : ComparePrerelease(Prerelease, other.Prerelease);
    }

    public static bool operator <(AppVersion left, AppVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(AppVersion left, AppVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(AppVersion left, AppVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(AppVersion left, AppVersion right) => left.CompareTo(right) >= 0;

    public override string ToString() =>
        Prerelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";

    // Une version sans pré-version est plus récente que la même avec pré-version (1.0.0 > 1.0.0-beta).
    private static int ComparePrerelease(string? left, string? right)
    {
        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return 1;
        }

        if (right is null)
        {
            return -1;
        }

        var leftParts = left.Split('.');
        var rightParts = right.Split('.');
        for (var i = 0; i < Math.Min(leftParts.Length, rightParts.Length); i++)
        {
            var result = CompareIdentifier(leftParts[i], rightParts[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return leftParts.Length.CompareTo(rightParts.Length);
    }

    // Identifiants numériques comparés comme des nombres, et plus petits que les identifiants alphanumériques.
    private static int CompareIdentifier(string left, string right)
    {
        var leftIsNumber = left.All(char.IsAsciiDigit);
        var rightIsNumber = right.All(char.IsAsciiDigit);

        if (leftIsNumber && rightIsNumber)
        {
            // Comparaison par longueur puis lexicale : correcte sans limite de taille (pas de dépassement d'entier).
            var lengthResult = left.Length.CompareTo(right.Length);
            return lengthResult != 0 ? lengthResult : string.CompareOrdinal(left, right);
        }

        if (leftIsNumber)
        {
            return -1;
        }

        if (rightIsNumber)
        {
            return 1;
        }

        return Math.Sign(string.CompareOrdinal(left, right));
    }
}
