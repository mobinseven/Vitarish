using Microsoft.AspNetCore.Http;
using Vitarish.Social.Application.Abstractions;
using Vitarish.Social.Domain.Enums;

namespace Vitarish.Social.Hosting.Endpoints;

public static class SocialWebhookEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSocialWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost("/webhooks/social/{provider}", HandleWebhookAsync)
            .WithName("social-webhook-ingest")
            .AllowAnonymous();

        _ = endpoints.MapGet("/webhooks/social/{provider}", HandleWebhookChallengeAsync)
            .WithName("social-webhook-challenge")
            .AllowAnonymous();

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
        if (httpContext.Request.Query.TryGetValue("connectionId", out var queryValue)
            && Guid.TryParse(queryValue.ToString(), out Guid parsedConnectionId))
        {
            connectionId = parsedConnectionId;
        }

        string body;
        using (StreamReader reader = new(httpContext.Request.Body))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, Microsoft.Extensions.Primitives.StringValues value) in httpContext.Request.Headers)
        {
            headers[key] = value.ToString();
        }

        Guid rawEventId = await gateway.IngestAsync(parsedProvider, connectionId, headers, body, cancellationToken);

        return Results.Ok(new { accepted = true, rawEventId });
    }

    private static IResult HandleWebhookChallengeAsync(string provider, HttpContext httpContext)
    {
        if (!TryParseProvider(provider, out SocialProvider parsedProvider))
        {
            return Results.BadRequest("Unsupported provider.");
        }

        if (parsedProvider == SocialProvider.Instagram
            && httpContext.Request.Query.TryGetValue("hub.challenge", out var challenge))
        {
            return Results.Text(challenge.ToString());
        }

        return Results.Ok();
    }

    private static bool TryParseProvider(string provider, out SocialProvider parsedProvider)
        => Enum.TryParse(provider, ignoreCase: true, out parsedProvider);
}
