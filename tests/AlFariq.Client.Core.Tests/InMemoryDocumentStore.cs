using AlFariq.Client.Core.Storage;

namespace AlFariq.Client.Core.Tests;

/// <summary>Stockage en mémoire pour les tests : même contrat que IndexedDB (documents JSON par magasin).</summary>
internal sealed class InMemoryDocumentStore : IDocumentStore
{
    private readonly Dictionary<string, Dictionary<string, string>> _stores = new();

    public Task<string?> GetAsync(string store, string id) =>
        Task.FromResult(_stores.TryGetValue(store, out var documents) && documents.TryGetValue(id, out var json) ? json : null);

    public Task<IReadOnlyList<string>> GetAllAsync(string store) =>
        Task.FromResult<IReadOnlyList<string>>(
            _stores.TryGetValue(store, out var documents) ? documents.Values.ToList() : []);

    public Task PutAsync(string store, string id, string json)
    {
        if (!_stores.TryGetValue(store, out var documents))
        {
            _stores[store] = documents = new Dictionary<string, string>();
        }

        documents[id] = json;
        return Task.CompletedTask;
    }
}

/// <summary>Horloge réglable.</summary>
internal sealed class FakeClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Fuseau horaire « de l'appareil » ; UTC par défaut.</summary>
    public TimeZoneInfo Zone { get; set; } = TimeZoneInfo.Utc;

    public override TimeZoneInfo LocalTimeZone => Zone;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan duration) => _now += duration;
}
