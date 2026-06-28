using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;

namespace Vitarish.Authentication.Api.Endpoints;

public static class LogoutEndpoints
{
    public static IEndpointRouteBuilder MapLogoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/logout/context", async (string? logoutId, GetLogoutContextHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new GetLogoutContextQuery(logoutId), ct);
            return r is AuthenticationResult<LogoutContext?>.Success s && s.Value is not null
                ? Results.Ok(new LogoutContextDto(s.Value.LogoutId, s.Value.ClientName, s.Value.PostLogoutRedirectUri, s.Value.ShowLogoutPrompt))
                : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("Authentication_Logout_Context")
        .WithOpenApi(o =>
        {
            o.OperationId = "getAuthenticationLogoutContext";
            o.Summary = "Gets logout context.";
            return o;
        });

        endpoints.MapPost("/logout", async (LogoutRequest request, LogoutHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new LogoutCommand(request.LogoutId, request.ReturnUrl), ct);
            return r is AuthenticationResult<LogoutResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LogoutResult>.Failure)r).Error);
        })
        .RequireAuthorization()
        .WithName("Authentication_Logout_Post")
        .WithOpenApi(o =>
        {
            o.OperationId = "logoutAuthenticationSession";
            o.Summary = "Signs out the current authentication session.";
            return o;
        });

        return endpoints;
    }
}
