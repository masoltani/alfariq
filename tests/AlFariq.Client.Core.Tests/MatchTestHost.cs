using AlFariq.Application;
using AlFariq.Client.Core.Storage;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

/// <summary>L'application branchée sur un stockage en mémoire, avec les dépôts des matchs.</summary>
internal static class MatchTestHost
{
    public static ServiceProvider Create(FakeClock clock)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(new InMemoryDocumentStore());
        services.AddSingleton<TimeProvider>(clock);
        services.AddAlFariqApplication();
        services.AddLocalRepository<Profile>(LocalSchema.ProfilesStore);
        services.AddLocalRepository<Player>(LocalSchema.PlayersStore);
        services.AddLocalRepository<Match>(LocalSchema.MatchesStore);
        services.AddLocalRepository<MatchParticipant>(LocalSchema.MatchParticipantsStore);
        services.AddLocalRepository<MatchSide>(LocalSchema.MatchSidesStore);
        return services.BuildServiceProvider();
    }
}
