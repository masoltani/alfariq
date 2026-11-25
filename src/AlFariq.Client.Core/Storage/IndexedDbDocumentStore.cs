using Microsoft.JSInterop;

namespace AlFariq.Client.Core.Storage;

/// <summary>
/// <see cref="IDocumentStore"/> adossé à IndexedDB. La base est ouverte (et migrée, voir <see cref="LocalSchema"/>)
/// à la première opération ; un échec d'ouverture est signalé à l'appelant et retenté à l'opération suivante.
/// </summary>
public sealed class IndexedDbDocumentStore : IDocumentStore
{
    private readonly LocalDbModule _module;
    private Task? _opening;

    public IndexedDbDocumentStore(IJSRuntime js) => _module = new LocalDbModule(js);

    public async Task<string?> GetAsync(string store, string id)
    {
        await EnsureOpenAsync();
        return await _module.InvokeAsync<string?>("get", store, id);
    }

    public async Task<IReadOnlyList<string>> GetAllAsync(string store)
    {
        await EnsureOpenAsync();
        return await _module.InvokeAsync<string[]>("getAll", store);
    }

    public async Task PutAsync(string store, string id, string json)
    {
        await EnsureOpenAsync();
        await _module.InvokeVoidAsync("put", store, id, json);
    }

    private async Task EnsureOpenAsync()
    {
        _opening ??= OpenAsync();
        try
        {
            await _opening;
        }
        catch
        {
            _opening = null;
            throw;
        }
    }

    private async Task OpenAsync()
    {
        LocalSchema.Validate(LocalSchema.Migrations);
        await _module.InvokeVoidAsync(
            "open",
            LocalSchema.DatabaseName,
            LocalSchema.CurrentVersion,
            LocalSchema.ToJson(LocalSchema.Migrations));
    }
}
