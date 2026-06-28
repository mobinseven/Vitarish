using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PhoneLoginEndpoints
{
    public static IEndpointRouteBuilder MapPhoneLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login/phone/begin", async (BeginPhoneLoginRequest request, BeginPhoneLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new BeginPhoneLoginCommand(request.PhoneNumber, request.ReturnUrl), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        })
        .WithName("Authentication_Login_Phone_Begin")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithOpenApi(o =>
        {
            o.OperationId = "beginPhoneLogin";
            o.Summary = "Begins phone login.";
            return o;
        });

        endpoints.MapPost("/login/phone/complete", async (CompletePhoneLoginRequest request, CompletePhoneLoginHandler handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new CompletePhoneLoginCommand(request.ChallengeId, request.Code, request.ReturnUrl), ct);
            return r is AuthenticationResult<LoginResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error);
        })
        .WithName("Authentication_Login_Phone_Complete")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithOpenApi(o =>
        {
            o.OperationId = "completePhoneLogin";
            o.Summary = "Completes phone login.";
            return o;
        });

        return endpoints;
    }
}
