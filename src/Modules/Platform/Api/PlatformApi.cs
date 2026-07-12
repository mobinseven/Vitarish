using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Platform.Api.Endpoints;
using Vitarish.Platform.Contracts.Routes;

namespace Vitarish.Platform.Api;

public static class PlatformApi
{
    public static IEndpointRouteBuilder MapPlatformApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(PlatformApiRoutes.Base);
        group.RequireAuthorization(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        group.MapLocaleNotificationEndpoints();
        return endpoints;
    }
}
