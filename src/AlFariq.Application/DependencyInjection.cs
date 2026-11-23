using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Application;

public static class DependencyInjection
{
    /// <summary>Enregistre les cas d'usage de l'application (les premiers arrivent avec US-004).</summary>
    public static IServiceCollection AddAlFariqApplication(this IServiceCollection services)
    {
        return services;
    }
}
