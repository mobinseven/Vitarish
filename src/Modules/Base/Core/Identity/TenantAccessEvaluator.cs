using System.Security.Claims;

namespace Vitarish.Modules.Base.Core.Identity;

public static class TenantAccessEvaluator
{
    public static TenantAccess Evaluate(ClaimsPrincipal principal, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        bool canAccessTenantOwner = HasRoleOrAccess(principal, BuildRequirement(tenantId, IdentityDefinitions.TenantOwner));
        bool canAccessTenantUser = HasRoleOrAccess(principal, BuildRequirement(tenantId, IdentityDefinitions.TenantUser));

        return new TenantAccess(canAccessTenantOwner, canAccessTenantUser);
    }

    private static string BuildRequirement(string? tenantId, string role) =>
        string.IsNullOrWhiteSpace(tenantId) ? role : $"{tenantId}:{role}";

    private static bool HasRoleOrAccess(ClaimsPrincipal principal, string requirement) =>
        principal.IsInRole(requirement) ||
        principal.Claims.Any(c => c.Type == IdentityDefinitions.AccessClaim && c.Value == requirement);
}
