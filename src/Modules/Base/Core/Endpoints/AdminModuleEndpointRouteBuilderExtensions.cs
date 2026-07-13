using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Vitarish.Modules.Base.Core.Endpoints;

public static class AdminModuleEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapAdminModuleGroup(this IEndpointRouteBuilder endpoints, string prefix, string scheme = ModuleEndpointAuthenticationSchemes.BrowserOrBearer)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(prefix, scheme);
        group.RequireAuthorization(new AuthorizeAttribute { Policy = IdentityDefinitions.PlatformAdmin });
        return group;
    }
}
