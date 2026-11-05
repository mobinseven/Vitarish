using Vitarish.Entities.Contracts.Models;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Application;

internal static class ContentRequestAccess
{
    public static EntityError? RequireTenantOwner(IRequestContext requestContext, out string tenantId)
    {
        tenantId = requestContext.TenantId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return new EntityError(EntityErrorCode.Unauthorized, "A tenant must be resolved for content authoring.");
        }

        System.Security.Claims.ClaimsPrincipal? user = requestContext.User;
        if (user is null || !TenantAccessEvaluator.Evaluate(user, tenantId).CanAccessTenantOwner)
        {
            return new EntityError(EntityErrorCode.Unauthorized, "The tenant owner role is required for content authoring.");
        }

        if (string.IsNullOrWhiteSpace(requestContext.UserId))
        {
            return new EntityError(EntityErrorCode.Unauthorized, "A requesting user must be resolved for content authoring.");
        }

        return null;
    }

    public static bool TryGetTenant(IRequestContext requestContext, out string tenantId)
    {
        tenantId = requestContext.TenantId ?? string.Empty;
        return !string.IsNullOrWhiteSpace(tenantId);
    }
}
