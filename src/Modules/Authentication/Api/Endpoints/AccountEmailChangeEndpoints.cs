using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AccountEmailChangeEndpoints
{
    public static IEndpointRouteBuilder MapAccountEmailChangeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/email/begin", async (BeginChangeEmailRequest request, BeginChangeEmailHandler handler, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            string? userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new BeginChangeEmailCommand(userId, request.Email), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        })
        .RequireAuthorization()
        .WithName("Authentication_Account_Email_Change_Begin")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Begins email change.");

        endpoints.MapPost("/account/email/complete", async (CompleteChangeEmailRequest request, CompleteChangeEmailHandler handler, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            string? userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new CompleteChangeEmailCommand(userId, request.ChallengeId, request.Code), ct);
            return r is AuthenticationResult<bool>.Success s && s.Value
                ? Results.Ok(new { Succeeded = true })
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)r).Error);
        })
        .RequireAuthorization()
        .WithName("Authentication_Account_Email_Change_Complete")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Completes email change.");

        return endpoints;
    }
}
