using AlFariq.Client.Core.Storage;
using AlFariq.Domain;

namespace AlFariq.Client.Core.Tests;

public class LocalRepositoryTests
{
    private sealed class Player : Entity
    {
        public string Name { get; set; } = "";
    }

    private readonly FakeClock _clock = new();
    private readonly LocalRepository<Player> _players;

    public LocalRepositoryTests()
    {
        _players = new LocalRepository<Player>(new InMemoryDocumentStore(), _clock, "players");
    }

    // ---------- Création ----------

    [Fact]
    public async Task Add_sets_both_dates_to_now_and_keeps_the_client_generated_id()
    {
        var player = new Player { Name = "Sami" };
        var id = player.Id;

        var added = await _players.AddAsync(player);

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(id, added.Id);
        Assert.Equal(_clock.GetUtcNow(), added.CreatedAt);
        Assert.Equal(_clock.GetUtcNow(), added.UpdatedAt);
        Assert.Null(added.DeletedAt);
    }

    [Fact]
    public async Task Add_rejects_an_existing_id_even_if_the_entity_was_deleted()
    {
        var player = await _players.AddAsync(new Player { Name = "Sami" });
        await _players.DeleteAsync(player.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _players.AddAsync(player));
    }

    // ---------- Lecture ----------

    [Fact]
    public async Task Get_returns_the_stored_entity()
    {
        var added = await _players.AddAsync(new Player { Name = "Sami" });

        var read = await _players.GetAsync(added.Id);

        Assert.NotNull(read);
        Assert.Equal("Sami", read.Name);
        Assert.Equal(added.CreatedAt, read.CreatedAt);
    }

    [Fact]
    public async Task Get_returns_null_for_an_unknown_id()
    {
        Assert.Null(await _players.GetAsync(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task List_returns_entities_in_creation_order()
    {
        var first = await _players.AddAsync(new Player { Name = "A" });
        _clock.Advance(TimeSpan.FromMinutes(1));
        var second = await _players.AddAsync(new Player { Name = "B" });

        var all = await _players.ListAsync();

        Assert.Equal(new[] { first.Id, second.Id }, all.Select(p => p.Id));
    }

    // ---------- Modification ----------

    [Fact]
    public async Task Update_changes_the_data_and_UpdatedAt_but_not_CreatedAt()
    {
        var added = await _players.AddAsync(new Player { Name = "Sami" });
        var createdAt = added.CreatedAt;
        _clock.Advance(TimeSpan.FromHours(2));

        added.Name = "Samy";
        await _players.UpdateAsync(added);

        var read = await _players.GetAsync(added.Id);
        Assert.NotNull(read);
        Assert.Equal("Samy", read.Name);
        Assert.Equal(createdAt, read.CreatedAt);
        Assert.Equal(_clock.GetUtcNow(), read.UpdatedAt);
        Assert.True(read.UpdatedAt > read.CreatedAt);
    }

    [Fact]
    public async Task Update_ignores_dates_set_by_the_caller()
    {
        var added = await _players.AddAsync(new Player { Name = "Sami" });
        var createdAt = added.CreatedAt;

        added.CreatedAt = DateTimeOffset.MinValue;
        added.DeletedAt = DateTimeOffset.MaxValue;
        await _players.UpdateAsync(added);

        var read = await _players.GetAsync(added.Id);
        Assert.NotNull(read);
        Assert.Equal(createdAt, read.CreatedAt);
        Assert.Null(read.DeletedAt);
    }

    [Fact]
    public async Task Update_fails_for_an_unknown_entity()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _players.UpdateAsync(new Player { Name = "Fantôme" }));
    }

    [Fact]
    public async Task Update_fails_for_a_deleted_entity()
    {
        var added = await _players.AddAsync(new Player { Name = "Sami" });
        await _players.DeleteAsync(added.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _players.UpdateAsync(added));
    }

    // ---------- Suppression logique ----------

    [Fact]
    public async Task Delete_is_logical_the_entity_stays_stored_with_DeletedAt()
    {
        var added = await _players.AddAsync(new Player { Name = "Sami" });
        _clock.Advance(TimeSpan.FromMinutes(5));

        var deleted = await _players.DeleteAsync(added.Id);

        Assert.True(deleted);
        var kept = await _players.GetAsync(added.Id, includeDeleted: true);
        Assert.NotNull(kept);
        Assert.Equal(_clock.GetUtcNow(), kept.DeletedAt);
        Assert.Equal(_clock.GetUtcNow(), kept.UpdatedAt);
        Assert.True(kept.IsDeleted);
    }

    [Fact]
    public async Task Deleted_entities_are_hidden_unless_requested()
    {
        var kept = await _players.AddAsync(new Player { Name = "A" });
        var removed = await _players.AddAsync(new Player { Name = "B" });
        await _players.DeleteAsync(removed.Id);

        Assert.Null(await _players.GetAsync(removed.Id));
        Assert.Equal(new[] { kept.Id }, (await _players.ListAsync()).Select(p => p.Id));
        Assert.Equal(2, (await _players.ListAsync(includeDeleted: true)).Count);
    }

    [Fact]
    public async Task Delete_returns_false_for_an_unknown_or_already_deleted_entity()
    {
        var added = await _players.AddAsync(new Player { Name = "Sami" });

        Assert.False(await _players.DeleteAsync(Guid.CreateVersion7()));
        Assert.True(await _players.DeleteAsync(added.Id));
        Assert.False(await _players.DeleteAsync(added.Id));
    }

    // ---------- Isolation ----------

    [Fact]
    public async Task Stores_are_separate_per_entity_type()
    {
        var store = new InMemoryDocumentStore();
        var players = new LocalRepository<Player>(store, _clock, "players");
        var venues = new LocalRepository<Player>(store, _clock, "venues");

        await players.AddAsync(new Player { Name = "Sami" });

        Assert.Empty(await venues.ListAsync());
    }

    [Fact]
    public async Task Concurrent_updates_do_not_lose_entities()
    {
        var tasks = Enumerable.Range(0, 20).Select(i => _players.AddAsync(new Player { Name = $"P{i}" }));
        await Task.WhenAll(tasks);

        Assert.Equal(20, (await _players.ListAsync()).Count);
    }
}
