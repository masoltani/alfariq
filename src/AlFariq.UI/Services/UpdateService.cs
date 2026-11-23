using Microsoft.JSInterop;

namespace AlFariq.UI.Services;

/// <summary>
/// Détecte qu'une nouvelle version de l'application est prête et l'applique sur demande de l'utilisateur
/// (jamais de rechargement automatique : il pourrait être en pleine saisie).
/// </summary>
public sealed class UpdateService : IDisposable
{
    private readonly ShellInterop _interop;
    private DotNetObjectReference<UpdateService>? _reference;

    public UpdateService(ShellInterop interop) => _interop = interop;

    public bool IsUpdateAvailable { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_reference is not null)
        {
            return;
        }

        _reference = DotNetObjectReference.Create(this);
        await _interop.InvokeVoidAsync("initUpdates", _reference);
    }

    [JSInvokable]
    public void OnUpdateAvailable()
    {
        IsUpdateAvailable = true;
        Changed?.Invoke();
    }

    /// <summary>L'utilisateur reporte : le message disparaît jusqu'à la prochaine détection.</summary>
    public void Dismiss()
    {
        IsUpdateAvailable = false;
        Changed?.Invoke();
    }

    /// <summary>Active la nouvelle version puis recharge la page.</summary>
    public Task ApplyAsync() => _interop.InvokeVoidAsync("applyUpdate").AsTask();

    public void Dispose() => _reference?.Dispose();
}
