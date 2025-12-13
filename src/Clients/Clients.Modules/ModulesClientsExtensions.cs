using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clients.Modules.Communications.DependencyInjection;
using Vitarish.Clients.Modules.Files.DependencyInjection;
using Vitarish.Clients.Modules.Intelligence.DependencyInjection;

namespace Vitarish.Clients.Modules;

/// <summary>
/// Provides extension methods for configuring module clients in the dependency injection container.
/// </summary>
public static class ModulesClientsExtensions
{
    /// <summary>
    /// Adds the module clients to the service collection, including communications, files, and intelligence modules,
    /// along with necessary singletons and hosted services for hub connections and self-testing.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the modules to.</param>
    /// <returns>The <see cref="IServiceCollection"/> with the modules added.</returns>
    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        _ = services.AddCommunicationsModuleClient();
        _ = services.AddFilesModuleClient();
        _ = services.AddIntelligenceModuleClient();

        services.TryAddSingleton<IHubConnectionStarter, HubConnectionStarter>();
        services.TryAddSingleton<ModulesClientsSelfTest>();
        services.TryAddSingleton<IModulesClientsSelfTest>(sp => sp.GetRequiredService<ModulesClientsSelfTest>());
        _ = services.AddHostedService(sp => sp.GetRequiredService<ModulesClientsSelfTest>());

        return services;
    }
}
