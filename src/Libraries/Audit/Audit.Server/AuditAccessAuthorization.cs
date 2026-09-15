using System.Security.Claims;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

internal static class AuditAccessAuthorization
{
    public static bool HasReviewAccess(IRequestContext requestContext)
    {
        ClaimsPrincipal? user = requestContext.User;
        string? tenantId = requestContext.TenantId;
        return !string.IsNullOrWhiteSpace(tenantId) &&
            (user?.IsInRole(IdentityDefinitions.PlatformAdmin) is true || user?.IsInRole($"{tenantId}:{IdentityDefinitions.TenantOwner}") is true);
    }

    public static bool HasPlatformReviewAccess(IRequestContext requestContext) =>
        requestContext.User?.IsInRole(IdentityDefinitions.PlatformAdmin) is true;

    public static void DemandReviewAccess(IRequestContext requestContext)
    {
        if (!HasReviewAccess(requestContext))
        {
            throw new UnauthorizedAccessException("Audit review requires an authorized tenant owner or platform administrator.");
        }
    }

    public static void DemandPlatformReviewAccess(IRequestContext requestContext)
    {
        if (!HasPlatformReviewAccess(requestContext))
            throw new UnauthorizedAccessException("Global audit review requires an authorized platform administrator.");
    }
}
