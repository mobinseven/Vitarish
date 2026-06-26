using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Endpoints;
using Vitarish.Sea.Contracts.Results;
using Vitarish.Sea.Contracts.Routes;

namespace Vitarish.Sea.Api;

public static class SeaApiEndpointMapping
{
    public static IEndpointRouteBuilder MapSeaApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(SeaApiRoutes.Base);
        _ = group.MapGet(SeaApiRoutes.Health, (CancellationToken cancellationToken) => Results.Ok(new SeaCommandResult(true)));
        return endpoints;
    }
}
