using Microsoft.AspNetCore.Routing;
using Vitarish.Journey.Api.DependencyInjection;

namespace Vitarish.Journey.Module.DependencyInjection;

public static class JourneyModuleEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapJourneyModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapJourneyEndpoints();
    }
}
