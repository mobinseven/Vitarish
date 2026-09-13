using System.Security.Claims;

namespace Vitarish.Users.Application.Security;

public static class UsersSecurityAuthorization
{
    public static bool CanManageAccounts(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanReadSecurityState(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanAssignGlobalRole(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanReadTenantRoles(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanReplaceTenantRole(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    private static bool IsPlatformAdministrator(ClaimsPrincipal? principal) =>
        principal?.IsInRole(IdentityDefinitions.PlatformAdmin) is true;
}
