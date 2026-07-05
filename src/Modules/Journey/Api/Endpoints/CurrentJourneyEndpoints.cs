using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Contracts.Responses;
using Vitarish.Journey.Contracts.Routes;
using Vitarish.Journey.Domain;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Journey.Api.Endpoints;

public static class CurrentJourneyEndpoints
{
    public static RouteGroupBuilder MapCurrentJourneyEndpoints(this RouteGroupBuilder group)
    {
        group.MapModuleGet(JourneyRoutes.Current, GetCurrentJourneyAsync, "Journey_GetCurrent", typeof(JourneyDecisionDto));

        return group;
    }

    internal static async Task<IResult> GetCurrentJourneyAsync(
        IJourneyResolver resolver,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken ct)
    {
        AuthenticatedUserIdResolution userIdResolution = AuthenticatedUserIdResolver.Resolve(user);
        if (userIdResolution is not AuthenticatedUserIdResolution.Success resolvedUserId)
        {
            return Results.Problem(
                detail: "Authenticated user subject id is missing.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        string userId = resolvedUserId.UserId.Value;

        string? returnUrl = httpContext.Request.Query["returnUrl"].FirstOrDefault();
        string currentPath = httpContext.Request.Path.Value ?? "/";

        var request = new JourneyRequest(
            UserId: userId,
            RequestedReturnUrl: returnUrl,
            CurrentPath: currentPath,
            AppKey: null,
            WorkspaceId: null,
            IsAfterLogin: false
        );

        JourneyStep step = await resolver.ResolveAsync(request, ct);

        var decision = new JourneyDecisionDto(
            Step: MapToDto(step),
            LegacyReturnUrl: step.DestinationUrl,
            LegacyRequiresProfileCompletion: step.HasBlockingGate && step.BlockingGate?.Kind == JourneyGateKind.ProfileCompletion
        );

        return Results.Ok(decision);
    }

    private static JourneyStepDto MapToDto(JourneyStep step) =>
        new(
            DestinationUrl: step.DestinationUrl,
            DestinationKind: (JourneyDestinationKindDto)(int)step.DestinationKind,
            BlockingGate: step.BlockingGate is not null ? MapGateToDto(step.BlockingGate) : null,
            NonBlockingGates: step.NonBlockingGates?.Select(MapGateToDto).ToList()
        );

    private static JourneyGateDto MapGateToDto(JourneyGate gate) =>
        new(
            Kind: (JourneyGateKindDto)(int)gate.Kind,
            IsBlocking: gate.IsBlocking,
            DestinationUrl: gate.DestinationUrl,
            Message: gate.Message
        );
}
