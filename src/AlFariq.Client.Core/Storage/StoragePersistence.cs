using AlFariq.Application;
using Microsoft.JSInterop;

namespace AlFariq.Client.Core.Storage;

/// <summary>Demande le stockage persistant au navigateur (<c>navigator.storage.persist()</c>).</summary>
public sealed class StoragePersistence : IStoragePersistence
{
    private readonly LocalDbModule _module;

    public StoragePersistence(IJSRuntime js) => _module = new LocalDbModule(js);

    public async Task<StoragePersistenceStatus> RequestAsync()
    {
        try
        {
            var result = await _module.InvokeAsync<string>("requestPersistence");
            return result switch
            {
                "granted" => StoragePersistenceStatus.Persisted,
                "denied" => StoragePersistenceStatus.NotPersisted,
                _ => StoragePersistenceStatus.Unsupported,
            };
        }
        catch (Exception)
        {
            // La persistance est un confort : son absence ne doit jamais empêcher l'appli de démarrer.
            return StoragePersistenceStatus.Unsupported;
        }
    }
}
