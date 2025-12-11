namespace Vitarish.Clients.Gateway.Abstractions;

public interface IIdentityClientFactory
{
    Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null);
}
