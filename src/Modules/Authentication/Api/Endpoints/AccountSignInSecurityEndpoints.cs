using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AccountSignInSecurityEndpoints
{
    public static RouteGroupBuilder MapAccountSignInSecurityEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost(AuthenticationRoutes.SetFirstPasswordPath, async (
            SetFirstPasswordRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] SetFirstPasswordHandler handler,
            System.Security.Claims.ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            string? userId = AuthenticatedUserIdResolver.Resolve(user) is AuthenticatedUserIdResolution.Success resolvedUserId
                ? resolvedUserId.UserId.Value
                : null;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            AuthenticationResult<bool> result = await handler.Handle(new SetFirstPasswordCommand(userId, request.NewPassword), cancellationToken);
            return result is AuthenticationResult<bool>.Success success && success.Value
                ? Results.Ok(new AuthenticationActionResultDto(true))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)result).Error);
        }, AuthenticationAccountSecurityOperationIds.FirstPasswordSet, typeof(AuthenticationActionResultDto))
        .RequireRateLimiting(AuthenticationRateLimitPolicies.AccountSecurityMutation)
        .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
        .Produces<AuthenticationActionResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Sets a password when the account does not yet have one.");

        group.MapModulePut(AuthenticationRoutes.TwoStepSignInPath, async (
            ChangeTwoStepSignInRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] ChangeTwoStepSignInHandler handler,
            System.Security.Claims.ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            string? userId = AuthenticatedUserIdResolver.Resolve(user) is AuthenticatedUserIdResolution.Success resolvedUserId
                ? resolvedUserId.UserId.Value
                : null;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            AuthenticationResult<bool> result = await handler.Handle(new ChangeTwoStepSignInCommand(userId, request.Enabled), cancellationToken);
            return result is AuthenticationResult<bool>.Success success && success.Value
                ? Results.Ok(new AuthenticationActionResultDto(true))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)result).Error);
        }, AuthenticationAccountSecurityOperationIds.TwoStepSignInChange, typeof(AuthenticationActionResultDto))
        .RequireRateLimiting(AuthenticationRateLimitPolicies.AccountSecurityMutation)
        .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
        .Produces<AuthenticationActionResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Changes the account's two-step sign-in setting.");

        return group;
    }
}
