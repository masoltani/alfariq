using Microsoft.JSInterop;

namespace AlFariq.UI.Services;

/// <summary>État de connexion de l'appareil, mis à jour par les événements « online » et « offline » du navigateur.</summary>
public sealed class ConnectivityService : IDisposable
{
    private readonly ShellInterop _interop;
    private DotNetObjectReference<ConnectivityService>? _reference;

    public ConnectivityService(ShellInterop interop) => _interop = interop;

    public bool IsOnline { get; private set; } = true;

    public event Action? Changed;

    /// <summary>À appeler une fois après le premier rendu (l'interopérabilité JavaScript n'existe pas avant).</summary>
    public async Task InitializeAsync()
    {
        if (_reference is not null)
        {
            return;
        }

        _reference = DotNetObjectReference.Create(this);
        IsOnline = await _interop.InvokeAsync<bool>("initConnectivity", _reference);
        Changed?.Invoke();
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool online)
    {
        if (IsOnline == online)
        {
            return;
        }

        IsOnline = online;
        Changed?.Invoke();
    }

    public void Dispose() => _reference?.Dispose();
}
