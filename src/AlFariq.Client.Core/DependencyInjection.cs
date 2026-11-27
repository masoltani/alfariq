using AlFariq.Application;
using AlFariq.Client.Core.Sharing;
using AlFariq.Client.Core.Storage;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlFariq.Client.Core;

public static class DependencyInjection
{
    /// <summary>Enregistre les services du client : horloge, stockage IndexedDB, demande de persistance et partage.</summary>
    public static IServiceCollection AddAlFariqClientCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IDocumentStore, IndexedDbDocumentStore>();
        services.TryAddSingleton<IStoragePersistence, StoragePersistence>();
        services.TryAddSingleton<IShareService, BrowserShareService>();
        return services;
    }

    /// <summary>
    /// Enregistre le dépôt local d'un type d'entité. Le magasin doit être déclaré dans <see cref="LocalSchema"/>
    /// (une migration le crée) ; l'oubli est signalé tout de suite plutôt qu'à la première écriture.
    /// </summary>
    public static IServiceCollection AddLocalRepository<T>(this IServiceCollection services, string storeName)
        where T : Entity
    {
        if (!LocalSchema.ContainsStore(storeName))
        {
            throw new InvalidOperationException(
                $"Le magasin « {storeName} » n'est déclaré dans aucune migration de LocalSchema.");
        }

        services.AddSingleton<IRepository<T>>(sp => new LocalRepository<T>(
            sp.GetRequiredService<IDocumentStore>(),
            sp.GetRequiredService<TimeProvider>(),
            storeName));
        return services;
    }
}
