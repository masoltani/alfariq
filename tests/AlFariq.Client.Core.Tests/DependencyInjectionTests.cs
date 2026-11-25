using AlFariq.Application;
using AlFariq.Client.Core.Storage;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public class DependencyInjectionTests
{
    private sealed class Dummy : Entity;

    [Fact]
    public void AddLocalRepository_fails_fast_when_the_store_is_not_in_the_schema()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddLocalRepository<Dummy>("inconnu"));
    }

    [Fact]
    public void AddLocalRepository_resolves_a_repository_for_a_declared_store()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(new InMemoryDocumentStore());
        services.AddSingleton<TimeProvider>(new FakeClock());
        services.AddLocalRepository<Dummy>(LocalSchema.MetaStore);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<LocalRepository<Dummy>>(provider.GetRequiredService<IRepository<Dummy>>());
    }
}
