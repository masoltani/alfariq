using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlFariq.Application;

public static class DependencyInjection
{
    /// <summary>Enregistre les cas d'usage de l'application.</summary>
    /// <remarks>Les dépôts qu'ils utilisent sont enregistrés par l'hôte.</remarks>
    public static IServiceCollection AddAlFariqApplication(this IServiceCollection services)
    {
        services.TryAddSingleton<IUpcomingMatchQuery, UpcomingMatchQuery>();
        services.TryAddSingleton<MatchService>();
        services.TryAddSingleton<ProfileService>();
        services.TryAddSingleton<PlayerService>();
        return services;
    }
}
