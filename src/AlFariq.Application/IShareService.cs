namespace AlFariq.Application;

public enum ShareOutcome
{
    /// <summary>Le contenu a été remis à l'application choisie.</summary>
    Shared,

    /// <summary>L'utilisateur a refermé le partage.</summary>
    Cancelled,

    /// <summary>Le partage n'a pas pu avoir lieu.</summary>
    Failed,
}

/// <summary>L'image d'une composition, prête à être partagée ou enregistrée. À libérer après usage.</summary>
public interface ILineupImage : IAsyncDisposable
{
    /// <summary>Adresse locale de l'image, pour l'afficher et l'enregistrer.</summary>
    string Url { get; }

    string FileName { get; }

    /// <summary>Vrai si l'appareil sait partager ce fichier.</summary>
    bool CanShare { get; }

    /// <summary>Ouvre le partage de l'appareil. À appeler directement depuis un geste de l'utilisateur.</summary>
    Task<ShareOutcome> ShareAsync();
}

/// <summary>
/// Partage d'un contenu avec les autres applications de l'appareil (US-011). Le navigateur en fournit une
/// implémentation ; une application native aura la sienne. Aucune de ces méthodes ne lève d'exception.
/// </summary>
public interface IShareService
{
    /// <summary>Vrai si l'appareil propose un partage natif.</summary>
    Task<bool> CanShareAsync();

    /// <summary>Ouvre le partage de l'appareil. À appeler directement depuis un geste de l'utilisateur.</summary>
    Task<ShareOutcome> ShareTextAsync(string text);

    /// <summary>Copie le texte dans le presse-papiers ; faux en cas d'échec.</summary>
    Task<bool> CopyTextAsync(string text);

    /// <summary>Dessine l'image d'une composition, sur l'appareil ; null si elle ne peut pas être créée.</summary>
    Task<ILineupImage?> CreateImageAsync(LineupCard card, string fileName);
}
