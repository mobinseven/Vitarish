using Microsoft.Extensions.DependencyInjection;
using Vitarish.Knowledge.Client;
using Vitarish.Modules.Base.DependencyInjection;

namespace Vitarish.Knowledge.Interface.DependencyInjection;

public static class KnowledgeInterfaceExtensions
{
    public static IServiceCollection AddKnowledgeInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<IKnowledgeClient, KnowledgeHttpClient>();
        return services;
    }
}
