using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.DependencyInjection;

public static class AuthenticationApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationApi(this IEndpointRouteBuilder endpoints, AuthenticationProfilePolicy profilePolicy)
    {
        RouteGroupBuilder anonymous = endpoints.MapAnonymousModuleGroup(AuthenticationRoutes.BasePath, "Authentication login, challenge, and antiforgery endpoints must be reachable before a user has an authenticated module session.").WithTags(AuthenticationEndpointTags.Authentication);
        RouteGroupBuilder protectedGroup = endpoints.MapProtectedModuleGroup(AuthenticationRoutes.BasePath, requireTenantAccess: false).WithTags(AuthenticationEndpointTags.Authentication);
        _ = anonymous.MapAntiforgeryEndpoints();
        _ = anonymous.MapAuthenticationProfileEndpoint();
        _ = anonymous.MapPasswordLoginEndpoints();
        _ = anonymous.MapPasswordRecoveryEndpoints();
        if (profilePolicy.AllowPasswordlessPhoneLogin)
            _ = anonymous.MapPhoneLoginEndpoints();
        if (profilePolicy.AllowPasswordlessEmailLogin)
            _ = anonymous.MapEmailLoginEndpoints();
        if (profilePolicy.AllowPasskeyLogin)
            _ = anonymous.MapPasskeyLoginEndpoints();
        if (profilePolicy.UsePhoneOtpForTwoFactorAccounts)
            _ = anonymous.MapPasswordLoginOtpEndpoints();
        _ = protectedGroup.MapCurrentSessionEndpoints();
        _ = protectedGroup.MapAuthenticationSecurityEndpoints();
        _ = protectedGroup.MapLogoutEndpoints();
        _ = protectedGroup.MapAccountPhoneChangeEndpoints();
        _ = protectedGroup.MapAccountEmailChangeEndpoints();
        _ = protectedGroup.MapAccountPasswordChangeEndpoints();
        return endpoints;
    }
}
