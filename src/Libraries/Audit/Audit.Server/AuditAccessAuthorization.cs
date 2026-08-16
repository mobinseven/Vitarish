using System.Security.Claims;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

internal static class AuditAccessAuthorization
{
    public static void DemandReviewAccess(IRequestContext requestContext)
    {
        ClaimsPrincipal? user = requestContext.User;
        string? tenantId = requestContext.TenantId;
        bool isPlatformAdmin = user?.IsInRole(IdentityDefinitions.PlatformAdmin) is true;
        bool isTenantOwner = !string.IsNullOrWhiteSpace(tenantId) &&
            user?.IsInRole($"{tenantId}:{IdentityDefinitions.TenantOwner}") is true;

        if (string.IsNullOrWhiteSpace(tenantId) || (!isPlatformAdmin && !isTenantOwner))
        {
            throw new UnauthorizedAccessException("Audit review requires an authorized tenant owner or platform administrator.");
        }
    }
}
