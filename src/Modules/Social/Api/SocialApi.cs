using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Aggregator.Core.Endpoints;
using Vitarish.Social.Application.Abstractions;
using Vitarish.Social.Contracts;
using DomainConnection = Vitarish.Social.Domain.Entities.SocialConnection;
using DomainProvider = Vitarish.Social.Domain.Enums.SocialProvider;
using DomainConnectionStatus = Vitarish.Social.Domain.Enums.SocialConnectionStatus;
using DomainRawWebhookEvent = Vitarish.Social.Domain.Entities.RawWebhookEvent;
using DomainWebhookProcessingStatus = Vitarish.Social.Domain.Enums.WebhookProcessingStatus;
using DomainWebhookVerificationStatus = Vitarish.Social.Domain.Enums.WebhookVerificationStatus;

namespace Vitarish.Social.Api;

public static class SocialApi
{
    public static IEndpointRouteBuilder MapSocialApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(SocialApiRoutes.Base);
        _ = group.MapModuleGet(SocialApiRoutes.Connections, ListConnectionsAsync, "SocialListConnections", typeof(SocialConnectionResponse[]));
        _ = group.MapModuleGet(SocialApiRoutes.LatestWebhookForConnection, GetLatestWebhookAsync, "SocialGetLatestWebhook", typeof(SocialWebhookEventResponse));
        _ = group.MapModulePost(SocialApiRoutes.ReplayWebhook, ReplayWebhookAsync, "SocialReplayWebhook", typeof(SocialReplayWebhookResponse));
        return endpoints;
    }

    private static async Task<IResult> ListConnectionsAsync(ISocialConnectionsService service, CancellationToken cancellationToken)
    {
        IReadOnlyList<DomainConnection> connections = await service.GetConnectionsAsync(cancellationToken);
        return Results.Ok(connections.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> GetLatestWebhookAsync(Guid connectionId, ISocialConnectionsService service, CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return Results.BadRequest(new SocialErrorResponse(SocialErrorKind.Validation, "Connection id is required.", StatusCodes.Status400BadRequest));
        }

        DomainRawWebhookEvent? raw = await service.GetLastWebhookAsync(connectionId, cancellationToken);
        return raw is null ? Results.NotFound(new SocialErrorResponse(SocialErrorKind.NotFound, "No webhook was found for the connection.", StatusCodes.Status404NotFound)) : Results.Ok(ToResponse(raw));
    }

    private static async Task<IResult> ReplayWebhookAsync(Guid id, ISocialWebhookGatewayService service, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.BadRequest(SocialReplayWebhookResponse.Failed(SocialReplayWebhookStatus.ValidationFailure, new SocialErrorResponse(SocialErrorKind.Validation, "Webhook event id is required.", StatusCodes.Status400BadRequest)));
        }

        SocialWebhookReplayResult result = await service.ReplayAsync(id, cancellationToken);
        return result.Status switch
        {
            SocialWebhookReplayStatus.Queued => Results.Ok(SocialReplayWebhookResponse.Queued()),
            SocialWebhookReplayStatus.NotFound => Results.NotFound(SocialReplayWebhookResponse.Failed(SocialReplayWebhookStatus.NotFound, new SocialErrorResponse(SocialErrorKind.NotFound, "Webhook event was not found.", StatusCodes.Status404NotFound))),
            SocialWebhookReplayStatus.InvalidState => Results.Conflict(SocialReplayWebhookResponse.Failed(SocialReplayWebhookStatus.Conflict, new SocialErrorResponse(SocialErrorKind.Conflict, "Webhook event is not eligible for replay.", StatusCodes.Status409Conflict))),
            _ => Results.Problem("Unexpected replay result.")
        };
    }

    private static SocialConnectionResponse ToResponse(DomainConnection connection) => new(
        connection.Id,
        ToProvider(connection.Provider),
        connection.ExternalAccountId,
        ToStatus(connection.Status));

    private static SocialWebhookEventResponse ToResponse(DomainRawWebhookEvent raw) => new(
        raw.Id,
        raw.ConnectionId,
        ToProvider(raw.Provider),
        new DateTimeOffset(DateTime.SpecifyKind(raw.ReceivedAt, DateTimeKind.Utc)),
        ToVerificationStatus(raw.VerificationStatus),
        ToProcessingStatus(raw.ProcessingStatus),
        raw.ProcessingError);

    private static SocialProviderResponse ToProvider(DomainProvider provider) => provider switch
    {
        DomainProvider.Telegram => SocialProviderResponse.Telegram,
        DomainProvider.Instagram => SocialProviderResponse.Instagram,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported social provider."),
    };

    private static SocialConnectionStatusResponse ToStatus(DomainConnectionStatus status) => status switch
    {
        DomainConnectionStatus.Active => SocialConnectionStatusResponse.Active,
        DomainConnectionStatus.Disconnected => SocialConnectionStatusResponse.Disconnected,
        DomainConnectionStatus.TokenExpired => SocialConnectionStatusResponse.TokenExpired,
        DomainConnectionStatus.ReconnectRequired => SocialConnectionStatusResponse.ReconnectRequired,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported social connection status."),
    };

    private static SocialWebhookProcessingStatusResponse ToProcessingStatus(DomainWebhookProcessingStatus status) => status switch
    {
        DomainWebhookProcessingStatus.Pending => SocialWebhookProcessingStatusResponse.Pending,
        DomainWebhookProcessingStatus.Processing => SocialWebhookProcessingStatusResponse.Processing,
        DomainWebhookProcessingStatus.Processed => SocialWebhookProcessingStatusResponse.Processed,
        DomainWebhookProcessingStatus.Failed => SocialWebhookProcessingStatusResponse.Failed,
        DomainWebhookProcessingStatus.DeadLetter => SocialWebhookProcessingStatusResponse.DeadLetter,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported webhook processing status."),
    };

    private static SocialWebhookVerificationStatusResponse ToVerificationStatus(DomainWebhookVerificationStatus status) => status switch
    {
        DomainWebhookVerificationStatus.Pending => SocialWebhookVerificationStatusResponse.Pending,
        DomainWebhookVerificationStatus.Verified => SocialWebhookVerificationStatusResponse.Verified,
        DomainWebhookVerificationStatus.Failed => SocialWebhookVerificationStatusResponse.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported webhook verification status."),
    };
}
