using Microsoft.AspNetCore.Builder;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;

namespace Vitarish.Authentication.Api.DependencyInjection;

public static class AuthenticationApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder anonymous = endpoints.MapAnonymousModuleGroup(AuthenticationRoutes.BasePath, "Authentication login, challenge, and antiforgery endpoints must be reachable before a user has an authenticated module session.").WithTags(AuthenticationEndpointTags.Authentication);
        RouteGroupBuilder protectedGroup = endpoints.MapProtectedModuleGroup(AuthenticationRoutes.BasePath).WithTags(AuthenticationEndpointTags.Authentication);
        anonymous.MapAntiforgeryEndpoints(); anonymous.MapPasswordLoginEndpoints(); anonymous.MapPhoneLoginEndpoints(); anonymous.MapEmailLoginEndpoints(); anonymous.MapPasskeyLoginEndpoints();
        protectedGroup.MapCurrentSessionEndpoints(); protectedGroup.MapLogoutEndpoints();
        return endpoints;
    }
}
