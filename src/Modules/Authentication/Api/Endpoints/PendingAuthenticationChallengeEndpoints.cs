using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PendingAuthenticationChallengeEndpoints
{
    public static RouteGroupBuilder MapPendingAuthenticationChallenge(this RouteGroupBuilder group)
    {
        group.MapModuleGet(AuthenticationRoutes.PendingChallengePath, async ([Microsoft.AspNetCore.Mvc.FromServices] GetPendingAuthenticationChallengeHandler handler, CancellationToken ct) =>
        {
            PendingAuthenticationChallenge? pending = await handler.Handle(ct);
            return pending is null ? Results.NoContent() : Results.Ok(new PendingAuthenticationChallengeResponse(
                AuthenticationEndpointMapper.ToDto(pending.Challenge), pending.Destination, pending.ReturnUrl,
                pending.Challenge.Purpose == AuthenticationChallengePurpose.EmailLogin,
                pending.Challenge.Purpose == AuthenticationChallengePurpose.PhoneLogin,
                pending.Challenge.Purpose is AuthenticationChallengePurpose.PasswordLoginOtp or AuthenticationChallengePurpose.PasswordLoginEmailOtp,
                pending.Challenge.Purpose is AuthenticationChallengePurpose.EmailPasswordRecovery or AuthenticationChallengePurpose.PhonePasswordRecovery,
                pending.Challenge.Purpose == AuthenticationChallengePurpose.PasswordLoginEmailOtp));
        }, AuthenticationPendingChallengeOperationIds.Get, typeof(PendingAuthenticationChallengeResponse),
            profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AuthenticationAnonymousEndpointJustifications.PendingChallenge)
        .Produces<PendingAuthenticationChallengeResponse>()
        .Produces(StatusCodes.Status204NoContent)
        .WithSummary("Reads the pending challenge bound to this browser without extending or acknowledging it.");
        group.MapModulePost(AuthenticationRoutes.PendingChallengePath, ([Microsoft.AspNetCore.Mvc.FromServices] IPendingAuthenticationChallenge pending) =>
        {
            pending.Clear();
            return Results.Ok(new AuthenticationActionResultDto(true));
        }, AuthenticationPendingChallengeOperationIds.Clear, typeof(AuthenticationActionResultDto),
            profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AuthenticationAnonymousEndpointJustifications.PendingChallenge)
        .Produces<AuthenticationActionResultDto>()
        .WithSummary("Clears this browser's pending challenge reference.");
        return group;
    }
}
