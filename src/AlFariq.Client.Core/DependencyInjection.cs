using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core;

public static class DependencyInjection
{
    /// <summary>Enregistre les services du client (stockage local avec US-002).</summary>
    public static IServiceCollection AddAlFariqClientCore(this IServiceCollection services)
    {
        return services;
    }
}
