using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Vitarish.Sea.Application.Abstractions;

namespace Vitarish.Sea.Api.Authorization;

internal sealed class SeaAttachmentAuthorizationHandler(
    ISeaResourceAccess access,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OperationAuthorizationRequirement, SeaAttachmentResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        SeaAttachmentResource resource)
    {
        CancellationToken cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        bool authorized = requirement.Name == SeaAuthorizationOperations.Read.Name
            ? await access.CanReadAttachmentAsync(resource.AttachmentId, cancellationToken)
            : requirement.Name == SeaAuthorizationOperations.Write.Name
                && await access.CanWriteAttachmentAsync(resource.AttachmentId, cancellationToken);
        if (authorized) context.Succeed(requirement);
    }
}
