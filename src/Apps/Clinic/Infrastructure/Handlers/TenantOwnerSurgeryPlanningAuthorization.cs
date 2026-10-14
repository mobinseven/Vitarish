using System.Security.Claims;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Clinic.Infrastructure.Handlers;

/// <summary>
/// Only a platform administrator, or a principal holding the current tenant's TenantOwner role or access claim,
/// may plan and manage surgeries.
/// </summary>
internal sealed class TenantOwnerSurgeryPlanningAuthorization(IRequestContext requestContext) : ISurgeryPlanningAuthorization
{
    public bool CanPlan()
    {
        ClaimsPrincipal? user = requestContext.User;
        if (user is null || !Guid.TryParse(requestContext.TenantId, out Guid tenantId) || tenantId == Guid.Empty)
        {
            return false;
        }

        string ownerRole = TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner);
        return user.IsInRole(IdentityDefinitions.PlatformAdmin)
            || user.IsInRole(ownerRole)
            || user.HasClaim(IdentityDefinitions.AccessClaim, ownerRole);
    }
}
