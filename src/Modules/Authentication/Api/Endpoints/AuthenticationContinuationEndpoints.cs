using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AuthenticationContinuationEndpoints
{
    public static RouteGroupBuilder MapAuthenticationContinuationBegin(this RouteGroupBuilder group)
    {
        group.MapModulePost(AuthenticationRoutes.ContinuationBeginPath, async (BeginAuthenticationContinuationRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] AuthenticationContinuationHandler handler, [Microsoft.AspNetCore.Mvc.FromServices] IAuthenticationHostDirectory hosts, CancellationToken ct) =>
        {
            string? token = await handler.BeginAsync(hosts.CurrentOrigin, request.ReturnPath, ct);
            return token is null ? Results.NotFound() : Results.Ok(new AuthenticationContinuationResponse(token));
        }, AuthenticationContinuationOperationIds.Begin, typeof(AuthenticationContinuationResponse),
            profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AuthenticationAnonymousEndpointJustifications.Continuation)
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.Continuation)
        .Produces<AuthenticationContinuationResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Prepares a browser-correlated return to the requesting registered application.");
        return group;
    }

    public static RouteGroupBuilder MapAuthenticationContinuationComplete(this RouteGroupBuilder group)
    {
        group.MapModulePost(AuthenticationRoutes.ContinuationCompletePath, async (CompleteAuthenticationContinuationRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] AuthenticationContinuationHandler handler, CancellationToken ct) =>
        {
            string? destination = await handler.CompleteAsync(request.Token, ct);
            return destination is null ? Results.NotFound() : Results.Ok(new AuthenticationRedirectResultDto(destination, false));
        }, AuthenticationContinuationOperationIds.Complete, typeof(AuthenticationRedirectResultDto))
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.Continuation)
        .Produces<AuthenticationRedirectResultDto>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Completes central authentication for an authorized application.");
        return group;
    }
}
