using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clients.Modules.Communications.DependencyInjection;
using Vitarish.Clients.Modules.Files.DependencyInjection;
using Vitarish.Clients.Modules.Intelligence.DependencyInjection;

namespace Vitarish.Clients.Modules;

public static class ModulesClientsExtensions
{
    public static IServiceCollection AddModulesClients(this IServiceCollection services)
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
