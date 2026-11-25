using AlFariq.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public class UpcomingMatchQueryTests
{
    private sealed class OneMatch : IUpcomingMatchQuery
    {
        public Task<UpcomingMatch?> GetNextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<UpcomingMatch?>(new UpcomingMatch(Guid.CreateVersion7(), DateTimeOffset.UtcNow, null, 5));
    }

    [Fact]
    public async Task By_default_there_is_no_upcoming_match()
    {
        using var provider = new ServiceCollection().AddAlFariqApplication().BuildServiceProvider();

        Assert.Null(await provider.GetRequiredService<IUpcomingMatchQuery>().GetNextAsync());
    }

    [Fact]
    public void A_later_registration_replaces_the_default_query()
    {
        var services = new ServiceCollection().AddAlFariqApplication();
        services.AddSingleton<IUpcomingMatchQuery, OneMatch>();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<OneMatch>(provider.GetRequiredService<IUpcomingMatchQuery>());
    }
}
