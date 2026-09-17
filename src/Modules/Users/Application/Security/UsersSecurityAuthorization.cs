using System.Security.Claims;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Users.Application.Security;

public static class UsersSecurityAuthorization
{
    public static bool CanManageAccounts(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanReadSecurityState(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanAssignGlobalRole(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanReadTenantRoles(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanReplaceTenantRole(ClaimsPrincipal? principal) => IsPlatformAdministrator(principal);

    public static bool CanManageTenantMembers(ClaimsPrincipal? principal, Guid tenantId)
    {
        if (tenantId == Guid.Empty || principal is null)
            return false;

        return IsPlatformAdministrator(principal) ||
               principal.IsInRole(TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner)) ||
               principal.HasClaim(IdentityDefinitions.AccessClaim, TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner));
    }

    public static bool CanManageAnyTenantMembers(ClaimsPrincipal? principal) =>
        IsPlatformAdministrator(principal) || principal?.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == IdentityDefinitions.AccessClaim) && claim.Value.EndsWith(
                $"{TenantRoleDefinitions.RoleSeparator}{IdentityDefinitions.TenantOwner}", StringComparison.Ordinal)) is true;

    private static bool IsPlatformAdministrator(ClaimsPrincipal? principal) =>
        principal?.IsInRole(IdentityDefinitions.PlatformAdmin) is true;
}
