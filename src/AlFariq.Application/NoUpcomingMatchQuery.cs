namespace AlFariq.Application;

/// <summary>
/// Implémentation utilisée tant que les matchs n'existent pas : il n'y a jamais de prochain match.
/// US-006 enregistrera à sa place une implémentation qui lit le dépôt des matchs.
/// </summary>
public sealed class NoUpcomingMatchQuery : IUpcomingMatchQuery
{
    public Task<UpcomingMatch?> GetNextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<UpcomingMatch?>(null);
}
