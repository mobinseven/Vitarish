namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IPersonalClientFactory
{
    Task<HttpClient> CreateAuthenticatedClient(string? endpoint = null);
}
