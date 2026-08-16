using Vitarish.Modules.Base.Core.Identity;
using Microsoft.AspNetCore.Authorization;
using Duende.IdentityServer.Extensions;

namespace Vitarish.Main.Authorization;

public sealed class Authorizer(IUsersService usersService, IRequestContext requestContext) : AuthorizationHandler<AuthorizerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizerRequirement requirement)
    {
        if (!context.User.IsAuthenticated())
        {
            context.Fail();
            return;
        }

        AuthenticatedUserIdResolution subjectIdResolution = AuthenticatedUserIdResolver.Resolve(context.User);

        if (subjectIdResolution is not AuthenticatedUserIdResolution.Success resolvedSubjectId)
        {
            context.Fail();
            return;
        }

        string subjectId = resolvedSubjectId.UserId.Value;

        string requiredRole;
        bool result;
        switch (requirement.Role)
        {
            case IdentityDefinitions.PlatformUser:
                context.Succeed(requirement);
                break;

            case IdentityDefinitions.PlatformAdmin:
                requiredRole = IdentityDefinitions.PlatformAdmin;
                result = await HasCurrentRole(requiredRole, subjectId);
                if (result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case IdentityDefinitions.TenantUser:
                requiredRole = $"{requestContext.TenantId}:{IdentityDefinitions.TenantUser}";
                result = await HasCurrentRole(requiredRole, subjectId);
                if (result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case IdentityDefinitions.TenantOwner:
                requiredRole = $"{requestContext.TenantId}:{IdentityDefinitions.TenantOwner}";
                result = await HasCurrentRole(requiredRole, subjectId);
                if (result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case IdentityDefinitions.TenantAccess:
                if (string.IsNullOrWhiteSpace(requestContext.TenantId))
                {
                    context.Fail();
                    break;
                }

                string tenantUserRole = $"{requestContext.TenantId}:{IdentityDefinitions.TenantUser}";
                string tenantOwnerRole = $"{requestContext.TenantId}:{IdentityDefinitions.TenantOwner}";
                result = await HasCurrentRole(IdentityDefinitions.PlatformAdmin, subjectId) ||
                    await HasCurrentRole(tenantOwnerRole, subjectId) ||
                    await HasCurrentRole(tenantUserRole, subjectId);
                if (result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            default:
                break;
        }
        return;
    }

    private Task<bool> HasCurrentRole(string role, string subjectId) =>
        usersService.CheckUserRoleAsync(subjectId, role);
}
