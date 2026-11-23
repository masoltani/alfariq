namespace AlFariq.Web.Tests;

/// <summary>Localise les fichiers du dépôt à partir du dossier d'exécution des tests.</summary>
internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string WebRoot => Path.Combine(Root, "src", "AlFariq.Web", "wwwroot");

    public static string UiRoot => Path.Combine(Root, "src", "AlFariq.UI");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AlFariq.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Racine du dépôt introuvable (AlFariq.slnx).");
    }
}
