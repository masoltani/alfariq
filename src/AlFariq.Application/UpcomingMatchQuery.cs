using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Lit le prochain match (en cours ou à venir) dans le dépôt des matchs.</summary>
public sealed class UpcomingMatchQuery : IUpcomingMatchQuery
{
    private readonly IRepository<Match> _matches;
    private readonly TimeProvider _clock;

    public UpcomingMatchQuery(IRepository<Match> matches, TimeProvider clock)
    {
        _matches = matches;
        _clock = clock;
    }

    public async Task<UpcomingMatch?> GetNextAsync(CancellationToken cancellationToken = default)
    {
        var matches = await _matches.ListAsync(cancellationToken: cancellationToken);
        var upcoming = MatchSchedule.Upcoming(matches, _clock.GetUtcNow());
        if (upcoming.Count == 0)
        {
            return null;
        }

        var next = upcoming[0];
        return new UpcomingMatch(next.Id, next.StartsAt, next.Location, next.PlayersPerSide);
    }
}
