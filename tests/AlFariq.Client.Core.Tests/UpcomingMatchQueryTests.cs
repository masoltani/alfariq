using AlFariq.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public sealed class UpcomingMatchQueryTests : IDisposable
{
    // Il est 10 h (UTC) le 25 novembre 2026.
    private static readonly DateOnly Today = new(2026, 11, 25);

    private readonly FakeClock _clock = new();
    private readonly ServiceProvider _app;

    public UpcomingMatchQueryTests() => _app = MatchTestHost.Create(_clock);

    public void Dispose() => _app.Dispose();

    private MatchService Matches => _app.GetRequiredService<MatchService>();

    private Task<UpcomingMatch?> NextAsync() => _app.GetRequiredService<IUpcomingMatchQuery>().GetNextAsync();

    [Fact]
    public async Task Without_any_match_there_is_no_upcoming_match()
    {
        Assert.Null(await NextAsync());
    }

    [Fact]
    public async Task The_closest_match_still_to_be_played_is_returned()
    {
        await Matches.CreateAsync(new MatchInput(Today.AddDays(-1), new TimeOnly(20, 0), 5));
        await Matches.CreateAsync(new MatchInput(Today.AddDays(9), new TimeOnly(20, 0), 11));
        var closest = await Matches.CreateAsync(
            new MatchInput(Today.AddDays(2), new TimeOnly(20, 0), 7, Location: "Stade"));

        var next = await NextAsync();

        Assert.Equal(new UpcomingMatch(closest.Id, closest.StartsAt, "Stade", 7), next);
    }

    [Fact]
    public async Task A_match_stays_upcoming_until_it_is_over()
    {
        await Matches.CreateAsync(new MatchInput(Today, new TimeOnly(9, 30), 5));

        Assert.NotNull(await NextAsync());

        _clock.Advance(TimeSpan.FromMinutes(30));

        Assert.Null(await NextAsync());
    }

    [Fact]
    public async Task A_deleted_match_is_not_upcoming()
    {
        var match = await Matches.CreateAsync(new MatchInput(Today.AddDays(1), new TimeOnly(20, 0), 5));

        await Matches.DeleteAsync(match.Id);

        Assert.Null(await NextAsync());
    }
}
