using AlFariq.Domain;
using Microsoft.JSInterop;

namespace AlFariq.UI.Services;

/// <summary>
/// Journal des changements (embarqué, lisible hors ligne) et annonce des nouveautés après une mise à jour.
/// Le dernier numéro de version vu est gardé sur l'appareil. Premier lancement : on l'enregistre sans rien afficher.
/// </summary>
public sealed class WhatsNewService
{
    private readonly ShellInterop _interop;
    private bool _initialized;
    private bool _markedSeen;

    public WhatsNewService(ShellInterop interop)
    {
        _interop = interop;
        Changelog = LoadChangelog();
    }

    public Changelog Changelog { get; }

    /// <summary>Vrai tant que le bandeau « nouveautés » doit être affiché.</summary>
    public bool IsAnnouncementVisible { get; private set; }

    public event Action? Changed;

    /// <summary>À appeler une fois après le premier rendu (l'interopérabilité JavaScript n'existe pas avant).</summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var current = AppInfo.SemanticVersion;

        string? lastSeen;
        try
        {
            lastSeen = await _interop.InvokeAsync<string?>("getLastSeenVersion");
        }
        catch (JSException)
        {
            return;
        }

        // L'utilisateur a déjà ouvert l'écran des nouveautés pendant la lecture : rien à annoncer.
        if (_markedSeen)
        {
            return;
        }

        if (Changelog.HasNewsSince(lastSeen, current))
        {
            IsAnnouncementVisible = true;
            Changed?.Invoke();
            return;
        }

        // Premier lancement, ou version sans rien de nouveau à dire : on aligne le numéro mémorisé en silence.
        if (lastSeen != current.ToString())
        {
            await RememberAsync(current);
        }
    }

    /// <summary>L'utilisateur a vu (ou écarté) les nouveautés de la version installée.</summary>
    public async Task MarkSeenAsync()
    {
        _markedSeen = true;
        var wasVisible = IsAnnouncementVisible;
        IsAnnouncementVisible = false;

        await RememberAsync(AppInfo.SemanticVersion);

        if (wasVisible)
        {
            Changed?.Invoke();
        }
    }

    private async Task RememberAsync(AppVersion version)
    {
        try
        {
            await _interop.InvokeVoidAsync("setLastSeenVersion", version.ToString());
        }
        catch (JSException)
        {
            // Stockage indisponible : l'annonce ne se répétera simplement pas à la prochaine visite.
        }
    }

    private static Changelog LoadChangelog()
    {
        try
        {
            return Changelog.LoadEmbedded();
        }
        catch (Exception exception) when (exception is FormatException or InvalidOperationException)
        {
            // Un journal défectueux ne doit jamais empêcher l'appli de démarrer (les tests l'empêchent d'être publié).
            return new Changelog([]);
        }
    }
}
