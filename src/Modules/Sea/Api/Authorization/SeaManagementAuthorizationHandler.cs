using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Api.Authorization;

internal sealed class SeaManagementAuthorizationHandler(IRequestContext requestContext)
    : AuthorizationHandler<OperationAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement)
    {
        if (requirement.Name == SeaAuthorizationOperations.Manage.Name &&
            SeaPrincipalAccess.CanManage(context.User, requestContext.TenantId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
