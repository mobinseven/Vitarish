using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PhoneLoginEndpoints
{
    public static RouteGroupBuilder MapPhoneLoginEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost("/login/phone/begin", async (BeginPhoneLoginRequest request, [Microsoft.AspNetCore.Mvc.FromServices] BeginPhoneLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new BeginPhoneLoginCommand(request.PhoneNumber, request.ReturnUrl), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        }, "Authentication_Login_Phone_Begin", typeof(AuthenticationChallengeDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.phone")
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Begins phone login.");

        group.MapModulePost("/login/phone/complete", async (CompletePhoneLoginRequest request, [Microsoft.AspNetCore.Mvc.FromServices] CompletePhoneLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new CompletePhoneLoginCommand(request.ChallengeId, request.Code, request.ReturnUrl), ct);
            return r is AuthenticationResult<LoginResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error);
        }, "Authentication_Login_Phone_Complete", typeof(AuthenticationRedirectResultDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.phone")
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Completes phone login.");

        return group;
    }
}
