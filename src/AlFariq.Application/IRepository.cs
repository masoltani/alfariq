using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>
/// Accès aux entités d'un type. Le reste de l'application ne connaît que cette interface :
/// l'implémentation locale (IndexedDB) pourra être complétée par une version synchronisée (US-022).
/// </summary>
/// <remarks>
/// Les suppressions sont logiques (<see cref="Entity.DeletedAt"/>). Par défaut, les entités supprimées
/// sont invisibles ; <c>includeDeleted</c> permet de les retrouver (historique, synchronisation).
/// </remarks>
public interface IRepository<T> where T : Entity
{
    /// <summary>Ajoute une entité. Renseigne <c>CreatedAt</c> et <c>UpdatedAt</c> et l'efface de toute suppression.</summary>
    /// <exception cref="InvalidOperationException">Une entité avec le même identifiant existe déjà (même supprimée).</exception>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task<T?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>Liste les entités par date de création croissante.</summary>
    Task<IReadOnlyList<T>> ListAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>Remplace une entité existante et active. Conserve <c>CreatedAt</c>, met à jour <c>UpdatedAt</c>.</summary>
    /// <exception cref="KeyNotFoundException">L'entité n'existe pas ou est supprimée.</exception>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Supprime logiquement une entité. Renvoie false si elle n'existe pas ou est déjà supprimée.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
