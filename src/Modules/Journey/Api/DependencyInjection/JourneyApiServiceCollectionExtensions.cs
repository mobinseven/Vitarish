using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Vitarish.Journey.Api.Endpoints;
using Vitarish.Journey.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Journey.Api.DependencyInjection;

public static class JourneyApiServiceCollectionExtensions
{
    public static IEndpointRouteBuilder MapJourneyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(JourneyRoutes.BasePath);
        group.MapCurrentJourneyEndpoints();
        return endpoints;
    }
}
