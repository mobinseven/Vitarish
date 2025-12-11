using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
