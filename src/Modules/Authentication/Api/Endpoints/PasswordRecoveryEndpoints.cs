using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Enums;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PasswordRecoveryEndpoints
{
    public static RouteGroupBuilder MapPasswordRecoveryEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost(AuthenticationRoutes.BeginPasswordRecoveryPath, async (
            BeginPasswordRecoveryRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] BeginPasswordRecoveryHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.IsDefined(request.Channel))
                return Results.BadRequest();

            PasswordRecoveryChannel channel = request.Channel switch
            {
                PasswordRecoveryChannelDto.Email => PasswordRecoveryChannel.Email,
                PasswordRecoveryChannelDto.Sms => PasswordRecoveryChannel.Sms,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Channel))
            };
            AuthenticationResult<ChallengeResult> result = await handler.Handle(
                new BeginPasswordRecoveryCommand(channel, request.Contact), cancellationToken);
            return result is AuthenticationResult<ChallengeResult>.Success success
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(success.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)result).Error);
        }, AuthenticationPasswordRecoveryOperationIds.Begin, typeof(AuthenticationChallengeDto),
            profile: ModuleEndpointProfile.AnonymousJson,
            anonymousJustification: AuthenticationAnonymousEndpointJustifications.PasswordRecovery)
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Begins password recovery.");

        group.MapModulePost(AuthenticationRoutes.CompletePasswordRecoveryPath, async (
            CompletePasswordRecoveryRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] CompletePasswordRecoveryHandler handler,
            CancellationToken cancellationToken) =>
        {
            AuthenticationResult<bool> result = await handler.Handle(
                new CompletePasswordRecoveryCommand(request.ChallengeId, request.Code, request.NewPassword), cancellationToken);
            return result is AuthenticationResult<bool>.Success success && success.Value
                ? Results.Ok(new AuthenticationActionResultDto(true))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)result).Error);
        }, AuthenticationPasswordRecoveryOperationIds.Complete, typeof(AuthenticationActionResultDto),
            profile: ModuleEndpointProfile.AnonymousJson,
            anonymousJustification: AuthenticationAnonymousEndpointJustifications.PasswordRecovery)
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces<AuthenticationActionResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status423Locked)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .WithSummary("Completes password recovery.");

        return group;
    }
}
