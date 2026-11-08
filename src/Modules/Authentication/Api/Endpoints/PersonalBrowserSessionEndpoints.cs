using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PersonalBrowserSessionEndpoints
{
    public static RouteGroupBuilder MapPersonalBrowserSessions(this RouteGroupBuilder group)
    {
        group.MapModuleGet(AuthenticationRoutes.BrowserSessionsPath, async ([Microsoft.AspNetCore.Mvc.FromServices] PersonalBrowserSessionHandler handler, CancellationToken ct) =>
            Results.Ok(new BrowserSessionsResponse((await handler.ListAsync(ct)).Select(item => new BrowserSessionDto(item.SessionId, item.CreatedAt, item.LastActivityAt, item.IsCurrent)).ToArray())),
            AuthenticationBrowserSessionOperationIds.Get, typeof(BrowserSessionsResponse))
            .Produces<BrowserSessionsResponse>().WithSummary("Lists the authenticated person's active browser sessions.");
        group.MapModulePost(AuthenticationRoutes.BrowserSessionRevokePath, async (RevokeBrowserSessionRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] PersonalBrowserSessionHandler handler, CancellationToken ct) =>
            await handler.RevokeAsync(request.SessionId, ct) ? Results.Ok(new AuthenticationActionResultDto(true)) : Results.NotFound(),
            AuthenticationBrowserSessionOperationIds.Revoke, typeof(AuthenticationActionResultDto))
            .WithMetadata(new SecurityAuditRequiredMetadata())
            .Produces<AuthenticationActionResultDto>().ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Ends one of the authenticated person's browser sessions and its linked application sessions.");
        return group;
    }
}
