using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Api.Authorization;

internal sealed class SeaProcedureAuthorizationHandler(
    ISeaManagementAccessEvaluator access,
    IRequestContext requestContext,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OperationAuthorizationRequirement, SeaProcedureResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        SeaProcedureResource resource)
    {
        if (requirement.Name != SeaAuthorizationOperations.Read.Name)
        {
            return;
        }

        AuthenticatedUserIdResolution user = AuthenticatedUserIdResolver.Resolve(context.User);
        if (user is not AuthenticatedUserIdResolution.Success success || string.IsNullOrWhiteSpace(requestContext.TenantId)) return;
        if (await access.CanReadProcedureAsync(new(requestContext.TenantId, success.UserId.Value), resource.ProcedureId, httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None)) context.Succeed(requirement);
    }
}
