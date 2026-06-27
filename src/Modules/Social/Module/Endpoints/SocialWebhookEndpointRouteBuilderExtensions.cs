using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Vitarish.Modules.Base.Endpoints;
using Vitarish.Social.Application.Abstractions;
using Vitarish.Social.Domain.Enums;

namespace Vitarish.Social.Module.Endpoints;

public static class SocialWebhookEndpointRouteBuilderExtensions
{
    private const int MaxWebhookBodyBytes = 1024 * 1024;

    public static IEndpointRouteBuilder MapSocialWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder webhooks = endpoints.MapAnonymousModuleGroup("/webhooks/social", "External social providers cannot send internal module API access tokens; webhook authenticity is handled by the Social application gateway.");

        _ = webhooks.MapModulePost("/{provider}", HandleWebhookAsync, "SocialIngestWebhook", profile: ModuleEndpointProfile.AnonymousWebhook, anonymousJustification: "External social providers cannot send internal module API access tokens; webhook authenticity is handled by the Social application gateway.")
            .WithName("social-webhook-ingest")
            .WithMetadata(new WebhookEndpointMetadata("social", "application gateway signature verification"));

        _ = webhooks.MapModuleGet("/{provider}", HandleWebhookChallengeAsync, "SocialVerifyWebhook", profile: ModuleEndpointProfile.AnonymousWebhook, anonymousJustification: "External social providers cannot send internal module API access tokens; webhook authenticity is handled by the Social application gateway.")
            .WithName("social-webhook-challenge")
            .WithMetadata(new WebhookEndpointMetadata("social", "application gateway signature verification"));

        return endpoints;
    }

    private static async Task<IResult> HandleWebhookAsync(
        string provider,
        HttpContext httpContext,
        ISocialWebhookGatewayService gateway,
        CancellationToken cancellationToken)
    {
        if (!TryParseProvider(provider, out SocialProvider parsedProvider))
        {
            return Results.BadRequest("Unsupported provider.");
        }

        Guid? connectionId = null;
        if (httpContext.Request.Query.TryGetValue("connectionId", out StringValues queryValue)
            && Guid.TryParse(queryValue.ToString(), out Guid parsedConnectionId))
        {
            connectionId = parsedConnectionId;
        }

        string body;
        try
        {
            body = await ReadRequestBodyAsync(httpContext, cancellationToken);
        }
        catch (InvalidDataException)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, Microsoft.Extensions.Primitives.StringValues value) in httpContext.Request.Headers)
        {
            headers[key] = value.ToString();
        }

        Guid rawEventId = await gateway.IngestAsync(parsedProvider, connectionId, headers, body, cancellationToken);

        return Results.Ok(new { accepted = true, rawEventId });
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArrayBufferWriter<byte> buffer = new();

        while (true)
        {
            ReadResult result = await httpContext.Request.BodyReader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> segment = result.Buffer;

            foreach (ReadOnlyMemory<byte> memory in segment)
            {
                if (buffer.WrittenCount + memory.Length > MaxWebhookBodyBytes)
                {
                    httpContext.Request.BodyReader.AdvanceTo(segment.End);
                    throw new InvalidDataException("Webhook request body is too large.");
                }

                buffer.Write(memory.Span);
            }

            httpContext.Request.BodyReader.AdvanceTo(segment.End);
            if (result.IsCompleted)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static IResult HandleWebhookChallengeAsync(string provider, HttpContext httpContext) => !TryParseProvider(provider, out SocialProvider parsedProvider)
            ? Results.BadRequest("Unsupported provider.")
            : parsedProvider == SocialProvider.Instagram
            && httpContext.Request.Query.TryGetValue("hub.challenge", out StringValues challenge)
            ? Results.Text(challenge.ToString())
            : Results.Ok();

    private static bool TryParseProvider(string provider, out SocialProvider parsedProvider)
        => Enum.TryParse(provider, ignoreCase: true, out parsedProvider);
}
