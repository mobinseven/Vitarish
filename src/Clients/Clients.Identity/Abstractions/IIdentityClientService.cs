namespace Vitarish.Clients.Identity.Abstractions;

public interface IIdentityClientService
{
    Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null);
    Task<string> GetAccessTokenAsync();
}
