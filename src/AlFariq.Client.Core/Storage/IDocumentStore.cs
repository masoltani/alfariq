namespace AlFariq.Client.Core.Storage;

/// <summary>
/// Stockage clé-valeur de bas niveau : des documents JSON rangés par magasin (un magasin par type d'entité).
/// Implémentation du navigateur : <see cref="IndexedDbDocumentStore"/>. Les tests utilisent une version en mémoire.
/// </summary>
public interface IDocumentStore
{
    /// <summary>Renvoie le document JSON, ou null s'il n'existe pas.</summary>
    Task<string?> GetAsync(string store, string id);

    Task<IReadOnlyList<string>> GetAllAsync(string store);

    /// <summary>Crée ou remplace le document.</summary>
    Task PutAsync(string store, string id, string json);
}
