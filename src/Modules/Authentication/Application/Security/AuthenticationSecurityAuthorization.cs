using System.Security.Claims;

namespace Vitarish.Authentication.Application.Security;

public static class AuthenticationSecurityAuthorization
{
    public static bool CanReadSessionPolicy(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanChangeSessionPolicy(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanChangeUserInactivityPolicy(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanEndUserSessions(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    private static bool IsPlatformAdministrator(ClaimsPrincipal? principal) =>
        principal?.IsInRole(IdentityDefinitions.PlatformAdmin) is true;
}
