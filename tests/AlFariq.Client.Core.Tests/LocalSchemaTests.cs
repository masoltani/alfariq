using System.Text.Json;
using AlFariq.Client.Core.Storage;

namespace AlFariq.Client.Core.Tests;

public class LocalSchemaTests
{
    [Fact]
    public void Shipped_migrations_are_valid()
    {
        LocalSchema.Validate(LocalSchema.Migrations);
    }

    [Fact]
    public void Current_version_is_the_last_migration_version()
    {
        Assert.Equal(LocalSchema.Migrations.Max(m => m.Version), LocalSchema.CurrentVersion);
        Assert.True(LocalSchema.CurrentVersion >= 1);
    }

    [Fact]
    public void Validate_rejects_a_gap_in_versions()
    {
        var migrations = new[]
        {
            new SchemaMigration(1, [new StoreDefinition("a")]),
            new SchemaMigration(3, [new StoreDefinition("b")]),
        };

        Assert.Throws<InvalidOperationException>(() => LocalSchema.Validate(migrations));
    }

    [Fact]
    public void Validate_rejects_a_store_declared_twice()
    {
        var migrations = new[]
        {
            new SchemaMigration(1, [new StoreDefinition("a")]),
            new SchemaMigration(2, [new StoreDefinition("a")]),
        };

        Assert.Throws<InvalidOperationException>(() => LocalSchema.Validate(migrations));
    }

    [Fact]
    public void Validate_rejects_an_empty_list()
    {
        Assert.Throws<InvalidOperationException>(() => LocalSchema.Validate([]));
    }

    [Fact]
    public void ToJson_uses_the_camelCase_shape_expected_by_the_javascript_module()
    {
        var migrations = new[]
        {
            new SchemaMigration(1, [new StoreDefinition("matches", [new IndexDefinition("byDate", "startsAt")])]),
        };

        using var document = JsonDocument.Parse(LocalSchema.ToJson(migrations));
        var migration = document.RootElement[0];
        var store = migration.GetProperty("createStores")[0];
        var index = store.GetProperty("indexes")[0];

        Assert.Equal(1, migration.GetProperty("version").GetInt32());
        Assert.Equal("matches", store.GetProperty("name").GetString());
        Assert.Equal("byDate", index.GetProperty("name").GetString());
        Assert.Equal("startsAt", index.GetProperty("keyPath").GetString());
        Assert.False(index.GetProperty("unique").GetBoolean());
    }

    [Fact]
    public void ContainsStore_finds_declared_stores_only()
    {
        Assert.True(LocalSchema.ContainsStore(LocalSchema.MetaStore));
        Assert.False(LocalSchema.ContainsStore("inconnu"));
    }
}
