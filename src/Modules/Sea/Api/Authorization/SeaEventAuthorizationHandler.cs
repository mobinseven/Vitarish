using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Sea.Api.Authorization;

/// <summary>HTTP adapter over the same SEA access evaluator used by agent execution.</summary>
internal sealed class SeaEventAuthorizationHandler(
    ISeaManagementAccessEvaluator access,
    IRequestContext requestContext,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OperationAuthorizationRequirement, SeaEventResource>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, SeaEventResource resource)
    {
        if (await IsAuthorizedAsync(context.User, requirement, resource)) context.Succeed(requirement);
    }

    internal async Task<bool> IsAuthorizedAsync(System.Security.Claims.ClaimsPrincipal principal, OperationAuthorizationRequirement requirement, SeaEventResource resource)
    {
        if (requirement.Name is not (nameof(SeaAuthorizationOperations.Read) or nameof(SeaAuthorizationOperations.Write))) return false;
        AuthenticatedUserIdResolution resolution = AuthenticatedUserIdResolver.Resolve(principal);
        if (resolution is not AuthenticatedUserIdResolution.Success success || string.IsNullOrWhiteSpace(requestContext.TenantId)) return false;
        SeaManagementAgentSubject subject = new(requestContext.TenantId, success.UserId.Value);
        CancellationToken ct = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        return requirement.Name == nameof(SeaAuthorizationOperations.Read)
            ? await access.CanReadEventAsync(subject, resource.EventId, ct)
            : await access.CanWriteEventAsync(subject, resource.EventId, ct);
    }

    internal Task PrimeEventRolesAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken) =>
        access.PrimeEventRolesAsync(eventIds, cancellationToken);
}
