using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlFariq.Application;

public static class DependencyInjection
{
    /// <summary>Enregistre les cas d'usage de l'application (les premiers arrivent avec US-004).</summary>
    public static IServiceCollection AddAlFariqApplication(this IServiceCollection services)
    {
        // US-003 : sans matchs pour l'instant. Un enregistrement ultérieur (US-006) prend le dessus.
        services.TryAddSingleton<IUpcomingMatchQuery, NoUpcomingMatchQuery>();
        return services;
    }
}
