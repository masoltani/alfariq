using Microsoft.JSInterop;

namespace AlFariq.Client.Core.Storage;

/// <summary>Accès au module JavaScript <c>local-db.js</c> (chargé à la première utilisation, rechargé si le chargement a échoué).</summary>
internal sealed class LocalDbModule
{
    private const string ModulePath = "./_content/AlFariq.Client.Core/js/local-db.js";

    private readonly IJSRuntime _js;
    private Task<IJSObjectReference>? _module;

    public LocalDbModule(IJSRuntime js) => _js = js;

    public async ValueTask InvokeVoidAsync(string identifier, params object?[] args)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(identifier, args);
    }

    public async ValueTask<T> InvokeAsync<T>(string identifier, params object?[] args)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<T>(identifier, args);
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        try
        {
            return await _module;
        }
        catch
        {
            _module = null;
            throw;
        }
    }
}
