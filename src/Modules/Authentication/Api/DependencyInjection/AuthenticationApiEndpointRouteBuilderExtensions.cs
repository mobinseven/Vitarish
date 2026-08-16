using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.DependencyInjection;

public static class AuthenticationApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder anonymous = endpoints.MapAnonymousModuleGroup(AuthenticationRoutes.BasePath, "Authentication login, challenge, and antiforgery endpoints must be reachable before a user has an authenticated module session.").WithTags(AuthenticationEndpointTags.Authentication);
        RouteGroupBuilder protectedGroup = endpoints.MapProtectedModuleGroup(AuthenticationRoutes.BasePath, requireTenantAccess: false).WithTags(AuthenticationEndpointTags.Authentication);
        _ = anonymous.MapAntiforgeryEndpoints();
        _ = anonymous.MapPasswordLoginEndpoints();
        _ = anonymous.MapPhoneLoginEndpoints();
        _ = anonymous.MapEmailLoginEndpoints();
        _ = anonymous.MapPasskeyLoginEndpoints();
        _ = protectedGroup.MapCurrentSessionEndpoints();
        _ = protectedGroup.MapLogoutEndpoints();
        _ = protectedGroup.MapAccountPhoneChangeEndpoints();
        _ = protectedGroup.MapAccountEmailChangeEndpoints();
        _ = protectedGroup.MapAccountPasswordChangeEndpoints();
        return endpoints;
    }
}
