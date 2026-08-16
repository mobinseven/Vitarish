using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class LogoutEndpoints
{
    public static RouteGroupBuilder MapLogoutEndpoints(this RouteGroupBuilder group)
    {
        group.MapModuleGet("/logout/context", async (string? logoutId, [Microsoft.AspNetCore.Mvc.FromServices] GetLogoutContextHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new GetLogoutContextQuery(logoutId), ct);
            return r is AuthenticationResult<LogoutContext?>.Success s && s.Value is not null
                ? Results.Ok(new LogoutContextDto(s.Value.LogoutId, s.Value.ClientName, s.Value.PostLogoutRedirectUri, s.Value.ShowLogoutPrompt))
                : Results.NotFound();
        }, "Authentication_Logout_Context", typeof(LogoutContextDto))
        .Produces<LogoutContextDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Gets logout context.");

        group.MapModulePost("/logout", async (LogoutRequest request, [Microsoft.AspNetCore.Mvc.FromServices] LogoutHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new LogoutCommand(request.LogoutId, request.ReturnUrl), ct);
            return r is AuthenticationResult<LogoutResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LogoutResult>.Failure)r).Error);
        }, "Authentication_Logout_Post", typeof(LogoutResultDto))
        .Produces<LogoutResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Signs out the current authentication session.");

        return group;
    }
}
