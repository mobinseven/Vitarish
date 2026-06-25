using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Vitarish.Modules.Base.Endpoints;

public static class ProtectedModuleEndpointRouteBuilderExtensions
{
    private const string DefaultScheme = "IdentityServerAccessToken";

    public static RouteGroupBuilder MapProtectedModuleGroup(this IEndpointRouteBuilder endpoints, string prefix, string scheme = DefaultScheme)
    {
        RouteGroupBuilder group = endpoints.MapGroup(prefix);
        group.RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = scheme });
        return group;
    }

    public static RouteGroupBuilder MapAnonymousModuleGroup(this IEndpointRouteBuilder endpoints, string prefix, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new ArgumentException("Anonymous module endpoints require an explicit justification.", nameof(justification));
        }

        RouteGroupBuilder group = endpoints.MapGroup(prefix);
        group.AllowAnonymous();
        return group;
    }
}
