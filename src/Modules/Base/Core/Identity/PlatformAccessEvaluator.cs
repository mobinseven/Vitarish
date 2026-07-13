using System.Security.Claims;

namespace Vitarish.Modules.Base.Core.Identity;

public static class PlatformAccessEvaluator
{
    public static PlatformAccess Evaluate(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        bool canAccessPlatformAdmin =
            principal.IsInRole(IdentityDefinitions.PlatformAdmin) ||
            principal.HasClaim(IdentityDefinitions.AccessClaim, IdentityDefinitions.PlatformAdmin);

        return new PlatformAccess(canAccessPlatformAdmin);
    }
}
