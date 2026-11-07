using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PasswordLoginOtpEndpoints
{
    public static RouteGroupBuilder MapPasswordLoginOtpEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost(AuthenticationRoutes.PasswordLoginOtpCompletePath, async (
            CompletePasswordLoginOtpRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] CompletePasswordLoginOtpHandler handler,
            CancellationToken cancellationToken) =>
        {
            AuthenticationResult<LoginResult> result = await handler.Handle(
                new CompletePasswordLoginOtpCommand(request.ChallengeId, request.Code, request.ReturnUrl),
                cancellationToken);
            return result is AuthenticationResult<LoginResult>.Success success
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(success.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)result).Error);
        }, AuthenticationPasswordLoginOtpOperationIds.Complete, typeof(AuthenticationRedirectResultDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AuthenticationAnonymousEndpointJustifications.PasswordLoginOtp)
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Completes password sign-in with a one-time code.");

        group.MapModulePost(AuthenticationRoutes.PasswordLoginOtpResendPath, async (
            ResendPasswordLoginOtpRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] ResendPasswordLoginOtpHandler handler,
            CancellationToken cancellationToken) =>
        {
            AuthenticationResult<ChallengeResult> result = await handler.Handle(
                new ResendPasswordLoginOtpCommand(request.ChallengeId),
                cancellationToken);
            return result is AuthenticationResult<ChallengeResult>.Success success
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(success.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)result).Error);
        }, AuthenticationPasswordLoginOtpOperationIds.Resend, typeof(AuthenticationChallengeDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AuthenticationAnonymousEndpointJustifications.PasswordLoginOtpResend)
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Resends the password sign-in second-factor code.");

        return group;
    }
}
