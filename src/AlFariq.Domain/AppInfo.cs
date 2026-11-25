using System.Reflection;

namespace AlFariq.Domain;

/// <summary>Identité de l'application, partagée par toutes les couches.</summary>
public static class AppInfo
{
    public const string Name = "AlFariq";

    /// <summary>Version affichée à l'utilisateur, sans suffixe de métadonnées de build.</summary>
    public static string Version { get; } = StripBuildMetadata(
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>Version sémantique comparable ; 0.0.0 si la version de l'assembly n'est pas lisible.</summary>
    public static AppVersion SemanticVersion { get; } = AppVersion.TryParse(Version, out var parsed) ? parsed : default;

    /// <summary>Retire le suffixe de build : « 1.2.3+abc123 » devient « 1.2.3 ».</summary>
    public static string StripBuildMetadata(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.0.0";
        }

        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
