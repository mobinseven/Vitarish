using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Social.Contracts;
namespace Vitarish.Social.Client;
public sealed class SocialClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), ISocialClient
{
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);
    public async Task<IReadOnlyList<SocialConnection>> GetConnectionsAsync(CancellationToken cancellationToken) => (await GetJsonAsync<IReadOnlyList<SocialConnection>>(SocialApiRoutes.Base + SocialApiRoutes.Connections, cancellationToken)).Value ?? [];
    public Task<RawWebhookEvent?> GetLastWebhookAsync(Guid connectionId, CancellationToken cancellationToken) => Task.FromResult<RawWebhookEvent?>(null);
    public async Task ReplayAsync(Guid rawWebhookEventId, CancellationToken cancellationToken) => _ = await PostJsonAsync<object, object>(SocialApiRoutes.Base + SocialApiRoutes.ReplayWebhook.Replace("{id:guid}", rawWebhookEventId.ToString()), new { }, cancellationToken);
}
