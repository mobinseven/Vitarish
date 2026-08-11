using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Api.Authorization;

internal sealed class SeaAttachmentAuthorizationHandler(
    IAttachmentsService attachmentsService,
    ISeaManagementAccessEvaluator access,
    IRequestContext requestContext,
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

        AuthenticatedUserIdResolution user = AuthenticatedUserIdResolver.Resolve(context.User);
        if (user is not AuthenticatedUserIdResolution.Success success || string.IsNullOrWhiteSpace(requestContext.TenantId)) return;
        bool authorized = requirement.Name == SeaAuthorizationOperations.Read.Name
            ? await access.CanReadEventAsync(new(requestContext.TenantId, success.UserId.Value), attachment.EventId, httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None)
            : await access.CanWriteEventAsync(new(requestContext.TenantId, success.UserId.Value), attachment.EventId, httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);
        if (authorized) context.Succeed(requirement);
    }
}
