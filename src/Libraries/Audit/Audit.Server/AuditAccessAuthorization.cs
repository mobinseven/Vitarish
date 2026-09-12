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

    public static void DemandReviewAccess(IRequestContext requestContext)
    {
        if (!HasReviewAccess(requestContext))
        {
            throw new UnauthorizedAccessException("Audit review requires an authorized tenant owner or platform administrator.");
        }
    }
}
