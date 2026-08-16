using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class EmailLoginEndpoints
{
    public static RouteGroupBuilder MapEmailLoginEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost("/login/email/begin", async (BeginEmailLoginRequest request, [Microsoft.AspNetCore.Mvc.FromServices] BeginEmailLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new BeginEmailLoginCommand(request.Email, request.ReturnUrl), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        }, "Authentication_Login_Email_Begin", typeof(AuthenticationChallengeDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.email")
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Begins email login.");

        group.MapModulePost("/login/email/complete", async (CompleteEmailLoginRequest request, [Microsoft.AspNetCore.Mvc.FromServices] CompleteEmailLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new CompleteEmailLoginCommand(request.ChallengeId, request.Code, request.ReturnUrl), ct);
            return r is AuthenticationResult<LoginResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error);
        }, "Authentication_Login_Email_Complete", typeof(AuthenticationRedirectResultDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.email")
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Completes email login.");

        return group;
    }
}
