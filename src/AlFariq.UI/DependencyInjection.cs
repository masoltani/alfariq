using AlFariq.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.UI;

public static class DependencyInjection
{
    /// <summary>Enregistre les services de l'interface (traductions, connexion, thème, mises à jour).</summary>
    public static IServiceCollection AddAlFariqUi(this IServiceCollection services)
    {
        services.AddLocalization();
        services.AddScoped<ShellInterop>();
        services.AddScoped<ConnectivityService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<UpdateService>();
        return services;
    }
}
