using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Intelligence.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Intelligence.Api;

internal static class RealtimeTranscriptionEndpointMapping
{
    internal static RouteGroupBuilder MapRealtimeTranscription(this RouteGroupBuilder group)
    {
        _ = group.MapModuleGet(
            IntelligenceApiRoutes.LiveTranscriptionCapabilities,
            GetCapabilities,
            "IntelligenceGetLiveTranscriptionCapabilities",
            typeof(RealtimeTranscriptionCapabilities));

        _ = group.MapModuleGet(
                IntelligenceApiRoutes.LiveTranscription,
                HandleWebSocketAsync,
                "IntelligenceLiveTranscription",
                profile: ModuleEndpointProfile.Streaming,
                contentType: "application/websocket")
            .ExcludeFromOpenApiWithJustification(
                "OpenAPI cannot describe the authenticated bidirectional WebSocket protocol; the adjacent protected capability endpoint remains the generated HTTP discovery contract.");

        return group;
    }

    private static RealtimeTranscriptionCapabilities GetCapabilities(
        LiveTranscriptionCapabilitiesQuery query)
    {
        LiveTranscriptionCapabilities capabilities = query.Execute();
        return new RealtimeTranscriptionCapabilities(
            capabilities.Available,
            capabilities.Languages,
            capabilities.MimeTypes,
            new RealtimeTranscriptionLimits(
                capabilities.MaxFrameBytes,
                capabilities.MaxTotalBytes,
                capabilities.MaxDurationSeconds,
                capabilities.IdleTimeoutSeconds));
    }

    private static async Task HandleWebSocketAsync(
        HttpContext context,
        RealtimeTranscriptionOriginPolicy originPolicy,
        LiveTranscriptionSessionFactory sessionFactory,
        LiveTranscriptionSettings settings,
        ILogger<RealtimeTranscriptionWebSocketSession> logger,
        CancellationToken cancellationToken)
    {
        StringValues origins = context.Request.Headers.Origin;
        if (origins.Count != 1 || !originPolicy.IsTrusted(origins[0]))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest
            || !context.WebSockets.WebSocketRequestedProtocols.Contains(
                RealtimeTranscriptionProtocol.Subprotocol,
                StringComparer.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync(
            RealtimeTranscriptionProtocol.Subprotocol).ConfigureAwait(false);
        RealtimeTranscriptionWebSocketSession protocol = new(
            socket,
            sessionFactory,
            settings,
            logger);
        await protocol.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}
