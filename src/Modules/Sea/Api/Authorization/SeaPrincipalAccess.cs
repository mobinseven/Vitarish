using System.Security.Claims;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Sea.Api.Authorization;

internal static class SeaPrincipalAccess
{
    public static bool CanManage(ClaimsPrincipal principal, string? tenantId) =>
        principal.IsInRole(IdentityDefinitions.PlatformAdmin) ||
        principal.HasClaim(IdentityDefinitions.AccessClaim, IdentityDefinitions.PlatformAdmin) ||
        TenantAccessEvaluator.Evaluate(principal, tenantId).CanAccessTenantOwner;
}
