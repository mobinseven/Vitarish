using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using Vorn.Aaas.Client.Api.Abstractions;

namespace Vorn.Aaas.Client.Api.Internal;

internal interface IRequestIdentityForwarder
{
    ValueTask ForwardAsync(RequestIdentityEnvelope envelope, CancellationToken cancellationToken = default);
}

internal sealed class RequestIdentityForwarder(
    IServiceProvider services,
    ILogger<RequestIdentityForwarder> logger) : IRequestIdentityForwarder
{
    private IAaasClientFactory? factory;

    private IAaasClientFactory ClientFactory => factory ??= services.GetRequiredService<IAaasClientFactory>();

    public async ValueTask ForwardAsync(RequestIdentityEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = await ClientFactory.CreateClient("/api/request-identities");
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                string.Empty,
                envelope,
                cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
            {
                return;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Failed to forward request identity {RequestId}: {StatusCode} {Body}",
                envelope.RequestId,
                response.StatusCode,
                body);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Error forwarding request identity {RequestId}", envelope.RequestId);
        }
    }
}

internal sealed record RequestIdentityEnvelope(
    string RequestId,
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo,
    DateTimeOffset CreatedAt);
