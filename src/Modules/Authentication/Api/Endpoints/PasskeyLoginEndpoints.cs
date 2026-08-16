using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PasskeyLoginEndpoints
{
    public static RouteGroupBuilder MapPasskeyLoginEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost("/login/passkey/begin", async (BeginPasskeyAuthenticationRequest request, [Microsoft.AspNetCore.Mvc.FromServices] BeginPasskeyLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new BeginPasskeyLoginCommand(request.UserNameOrEmail, request.ReturnUrl), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        }, "Authentication_Login_Passkey_Begin", typeof(AuthenticationChallengeDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.passkey")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.Passkey)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Begins passkey login.");

        group.MapModulePost("/login/passkey/complete", async (CompletePasskeyAuthenticationRequest request, [Microsoft.AspNetCore.Mvc.FromServices] CompletePasskeyLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new CompletePasskeyLoginCommand(request.ChallengeId, request.CredentialJson, request.ReturnUrl), ct);
            return r is AuthenticationResult<LoginResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error);
        }, "Authentication_Login_Passkey_Complete", typeof(AuthenticationRedirectResultDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.passkey")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.Passkey)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Completes passkey login.");

        return group;
    }
}
