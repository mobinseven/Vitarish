using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;

namespace Vitarish.Authentication.Api.Endpoints;

public static class EmailLoginEndpoints
{
    public static IEndpointRouteBuilder MapEmailLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login/email/begin", async (BeginEmailLoginRequest request, BeginEmailLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new BeginEmailLoginCommand(request.Email, request.ReturnUrl), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        })
        .WithName("Authentication_Login_Email_Begin")
        .WithOpenApi(o =>
        {
            o.OperationId = "beginEmailLogin";
            o.Summary = "Begins email login.";
            return o;
        });

        endpoints.MapPost("/login/email/complete", async (CompleteEmailLoginRequest request, CompleteEmailLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new CompleteEmailLoginCommand(request.ChallengeId, request.Code, request.ReturnUrl), ct);
            return r is AuthenticationResult<LoginResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error);
        })
        .WithName("Authentication_Login_Email_Complete")
        .WithOpenApi(o =>
        {
            o.OperationId = "completeEmailLogin";
            o.Summary = "Completes email login.";
            return o;
        });

        return endpoints;
    }
}
