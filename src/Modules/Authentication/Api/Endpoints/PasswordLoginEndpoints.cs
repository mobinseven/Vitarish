using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PasswordLoginEndpoints
{
    public static RouteGroupBuilder MapPasswordLoginEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost("/login/password", async (PasswordLoginRequest request, [Microsoft.AspNetCore.Mvc.FromServices] LoginWithPasswordHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new LoginWithPasswordCommand(request.UserNameOrEmail, request.Password, request.ReturnUrl, request.RememberMe), ct);
            return r is AuthenticationResult<LoginResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error);
        }, "Authentication_Login_Password", typeof(AuthenticationRedirectResultDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.password")
        .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.PasswordLogin)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status423Locked)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Signs in with a password.");

        return group;
    }
}
