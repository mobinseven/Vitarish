using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Modules;

namespace Vitarish.Clients.Interface;

public static class InterfaceClientsExtensions
{
    public static IServiceCollection AddInterfaceClients(this IServiceCollection services)
    {
        _ = services.AddModulesClients();
        _ = services.AddVitarishClientsInterface();
        return services;
    }
}
