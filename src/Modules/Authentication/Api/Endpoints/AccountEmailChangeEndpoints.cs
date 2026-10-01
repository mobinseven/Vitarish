using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AccountEmailChangeEndpoints
{
    public static RouteGroupBuilder MapAccountEmailChangeEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost("/account/email/begin", async (BeginChangeEmailRequest request, [Microsoft.AspNetCore.Mvc.FromServices] BeginChangeEmailHandler handler, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            string? userId = AuthenticatedUserIdResolver.Resolve(user) is AuthenticatedUserIdResolution.Success resolvedUserId
                ? resolvedUserId.UserId.Value
                : null;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new BeginChangeEmailCommand(userId, request.Email), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        }, "Authentication_Account_Email_Change_Begin", typeof(AuthenticationChallengeDto))
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Begins email change.");

        group.MapModulePost("/account/email/complete", async (CompleteChangeEmailRequest request, [Microsoft.AspNetCore.Mvc.FromServices] CompleteChangeEmailHandler handler, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            string? userId = AuthenticatedUserIdResolver.Resolve(user) is AuthenticatedUserIdResolution.Success resolvedUserId
                ? resolvedUserId.UserId.Value
                : null;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new CompleteChangeEmailCommand(userId, request.ChallengeId, request.Code), ct);
            return r is AuthenticationResult<bool>.Success s && s.Value
                ? Results.Ok(new AuthenticationActionResultDto(true))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)r).Error);
        }, "Authentication_Account_Email_Change_Complete", typeof(AuthenticationActionResultDto))
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Completes email change.");

        return group;
    }
}
