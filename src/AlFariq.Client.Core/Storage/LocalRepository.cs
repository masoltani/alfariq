using System.Text.Json;
using AlFariq.Application;
using AlFariq.Domain;

namespace AlFariq.Client.Core.Storage;

/// <summary>
/// Dépôt local : sérialise les entités en JSON dans un magasin de l'<see cref="IDocumentStore"/>.
/// Gère les dates (UTC, via <see cref="TimeProvider"/>) et la suppression logique.
/// </summary>
public sealed class LocalRepository<T> : IRepository<T> where T : Entity
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDocumentStore _store;
    private readonly TimeProvider _clock;
    private readonly string _storeName;

    // Les écritures sont lues puis réécrites : on les fait une par une pour qu'aucune modification ne soit perdue.
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public LocalRepository(IDocumentStore store, TimeProvider clock, string storeName)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);

        _store = store;
        _clock = clock;
        _storeName = storeName;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RequireId(entity.Id);

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            if (await ReadAsync(entity.Id) is not null)
            {
                throw new InvalidOperationException($"Une entité avec l'identifiant {entity.Id} existe déjà.");
            }

            var now = _clock.GetUtcNow();
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.DeletedAt = null;

            await WriteAsync(entity);
            return entity;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<T?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await ReadAsync(id);
        return entity is not null && (includeDeleted || !entity.IsDeleted) ? entity : null;
    }

    public async Task<IReadOnlyList<T>> ListAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var documents = await _store.GetAllAsync(_storeName);
        return documents
            .Select(Deserialize)
            .Where(entity => includeDeleted || !entity.IsDeleted)
            .OrderBy(entity => entity.CreatedAt)
            .ThenBy(entity => entity.Id)
            .ToList();
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RequireId(entity.Id);

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var existing = await ReadAsync(entity.Id);
            if (existing is null || existing.IsDeleted)
            {
                throw new KeyNotFoundException($"Aucune entité active avec l'identifiant {entity.Id}.");
            }

            entity.CreatedAt = existing.CreatedAt;
            entity.UpdatedAt = _clock.GetUtcNow();
            entity.DeletedAt = null;

            await WriteAsync(entity);
            return entity;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var existing = await ReadAsync(id);
            if (existing is null || existing.IsDeleted)
            {
                return false;
            }

            var now = _clock.GetUtcNow();
            existing.DeletedAt = now;
            existing.UpdatedAt = now;

            await WriteAsync(existing);
            return true;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static void RequireId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("L'identifiant d'une entité ne peut pas être vide.", nameof(id));
        }
    }

    private static string Key(Guid id) => id.ToString("D");

    private static T Deserialize(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException("Document local illisible.");

    private async Task<T?> ReadAsync(Guid id)
    {
        var json = await _store.GetAsync(_storeName, Key(id));
        return json is null ? null : Deserialize(json);
    }

    private Task WriteAsync(T entity) =>
        _store.PutAsync(_storeName, Key(entity.Id), JsonSerializer.Serialize(entity, JsonOptions));
}
