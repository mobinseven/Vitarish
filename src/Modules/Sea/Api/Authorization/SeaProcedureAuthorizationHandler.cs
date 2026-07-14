using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Api.Authorization;

internal sealed class SeaProcedureAuthorizationHandler(
    IEventsService eventsService,
    SeaEventAuthorizationHandler eventAuthorizationHandler,
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

        if (SeaPrincipalAccess.CanManage(context.User, requestContext.TenantId))
        {
            context.Succeed(requirement);
            return;
        }

        IReadOnlyList<EventReadDto> events = (await eventsService.GetListAsync(
            new EventDescriptorDto { ProcedureId = resource.ProcedureId },
            httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None)).Value ?? [];
        await eventAuthorizationHandler.PrimeEventRolesAsync(
            events.Select(@event => @event.Id),
            httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);
        foreach (EventReadDto @event in events)
        {
            if (await eventAuthorizationHandler.IsAuthorizedAsync(
                context.User,
                SeaAuthorizationOperations.Read,
                new SeaEventResource(@event.Id)))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
