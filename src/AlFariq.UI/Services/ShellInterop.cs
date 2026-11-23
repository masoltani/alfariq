using Microsoft.JSInterop;

namespace AlFariq.UI.Services;

/// <summary>Accès au module JavaScript de l'interface (chargé une seule fois, à la première utilisation).</summary>
public sealed class ShellInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/AlFariq.UI/js/alfariq.js";

    private readonly Lazy<Task<IJSObjectReference>> _module;

    public ShellInterop(IJSRuntime js)
    {
        _module = new Lazy<Task<IJSObjectReference>>(
            () => js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    public async ValueTask InvokeVoidAsync(string identifier, params object?[] args)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync(identifier, args);
    }

    public async ValueTask<T> InvokeAsync<T>(string identifier, params object?[] args)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<T>(identifier, args);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_module.IsValueCreated)
        {
            return;
        }

        try
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Le circuit est déjà fermé : rien à libérer.
        }
    }
}
