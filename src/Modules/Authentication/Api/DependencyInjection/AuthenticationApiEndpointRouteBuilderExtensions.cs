using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.DependencyInjection;

public static class AuthenticationApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationApi(this IEndpointRouteBuilder endpoints, AuthenticationProfilePolicy profilePolicy)
    {
        RouteGroupBuilder anonymous = endpoints.MapAnonymousModuleGroup(AuthenticationRoutes.BasePath, AuthenticationAnonymousEndpointJustifications.EntryGroup).WithTags(AuthenticationEndpointTags.Authentication);
        RouteGroupBuilder protectedGroup = endpoints.MapProtectedModuleGroup(AuthenticationRoutes.BasePath, requireTenantAccess: false).WithTags(AuthenticationEndpointTags.Authentication);
        RouteGroupBuilder centralAnonymous = endpoints.MapAnonymousModuleGroup(AuthenticationRoutes.BasePath, AuthenticationAnonymousEndpointJustifications.EntryGroup).WithTags(AuthenticationEndpointTags.Authentication).AddEndpointFilter<CentralAuthenticationEndpointFilter>();
        RouteGroupBuilder centralProtected = endpoints.MapProtectedModuleGroup(AuthenticationRoutes.BasePath, requireTenantAccess: false).WithTags(AuthenticationEndpointTags.Authentication).AddEndpointFilter<CentralAuthenticationEndpointFilter>();
        _ = anonymous.MapAntiforgeryEndpoints();
        _ = anonymous.MapAuthenticationContinuationBegin();
        _ = centralAnonymous.MapPendingAuthenticationChallenge();
        _ = protectedGroup.MapAuthenticationContinuationComplete();
        _ = centralProtected.MapPersonalBrowserSessions();
        _ = anonymous.MapAuthenticationProfileEndpoint();
        _ = centralAnonymous.MapPasswordLoginEndpoints();
        _ = centralAnonymous.MapPasswordRecoveryEndpoints();
        if (profilePolicy.AllowPasswordlessPhoneLogin)
            _ = centralAnonymous.MapPhoneLoginEndpoints();
        if (profilePolicy.AllowPasswordlessEmailLogin)
            _ = centralAnonymous.MapEmailLoginEndpoints();
        if (profilePolicy.AllowPasskeyLogin)
            _ = centralAnonymous.MapPasskeyLoginEndpoints();
        if (profilePolicy.UsePhoneOtpForTwoFactorAccounts)
            _ = centralAnonymous.MapPasswordLoginOtpEndpoints();
        if (profilePolicy.AllowTenantHandoff)
            _ = anonymous.MapTenantHandoffEndpoints();
        _ = protectedGroup.MapCurrentSessionEndpoints();
        _ = protectedGroup.MapAuthenticationSecurityEndpoints();
        _ = protectedGroup.MapLogoutEndpoints();
        _ = centralProtected.MapAccountPhoneChangeEndpoints();
        _ = centralProtected.MapAccountEmailChangeEndpoints();
        _ = centralProtected.MapAccountPasswordChangeEndpoints();
        _ = centralProtected.MapAccountSignInSecurityEndpoints();
        return endpoints;
    }
}
