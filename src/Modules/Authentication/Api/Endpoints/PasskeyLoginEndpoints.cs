using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PasskeyLoginEndpoints
{
    public static IEndpointRouteBuilder MapPasskeyLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login/passkey/begin", async (BeginPasskeyAuthenticationRequest request, BeginPasskeyLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new BeginPasskeyLoginCommand(request.UserNameOrEmail, request.ReturnUrl), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        })
        .WithName("Authentication_Login_Passkey_Begin")
        .WithOpenApi(o =>
        {
            o.OperationId = "beginPasskeyLogin";
            o.Summary = "Begins passkey login.";
            return o;
        });

        endpoints.MapPost("/login/passkey/complete", async (CompletePasskeyAuthenticationRequest request, CompletePasskeyLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new CompletePasskeyLoginCommand(request.ChallengeId, request.CredentialJson, request.ReturnUrl), ct);
            return r is AuthenticationResult<LoginResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error);
        })
        .WithName("Authentication_Login_Passkey_Complete")
        .WithOpenApi(o =>
        {
            o.OperationId = "completePasskeyLogin";
            o.Summary = "Completes passkey login.";
            return o;
        });

        return endpoints;
    }
}
