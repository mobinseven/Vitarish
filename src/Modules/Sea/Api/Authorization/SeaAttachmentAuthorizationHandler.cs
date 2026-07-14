using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Vitarish.Sea.Application.Abstractions;

namespace Vitarish.Sea.Api.Authorization;

internal sealed class SeaAttachmentAuthorizationHandler(
    IAttachmentsService attachmentsService,
    SeaEventAuthorizationHandler eventAuthorizationHandler,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OperationAuthorizationRequirement, SeaAttachmentResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        SeaAttachmentResource resource)
    {
        Domain.Dto.AttachmentReadDto? attachment =
            (await attachmentsService.GetAsync(
                resource.AttachmentId,
                httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None)).Value;
        if (attachment is null)
        {
            return;
        }

        if (await eventAuthorizationHandler.IsAuthorizedAsync(
            context.User, requirement, new SeaEventResource(attachment.EventId)))
        {
            context.Succeed(requirement);
        }
    }
}
