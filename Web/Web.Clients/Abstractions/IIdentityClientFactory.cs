namespace Vitarish.Web.Clients.Abstractions;

/// <summary>
/// Factory for creating HTTP clients to communicate with the identity server.
/// </summary>
public interface IIdentityClientFactory
{
    /// <summary>
    /// Creates an HTTP client with authentication token configured.
    /// </summary>
    /// <param name="endpoint">Optional API endpoint path.</param>
    /// <param name="clientId">Optional client ID override.</param>
    /// <returns>Configured HttpClient.</returns>
    Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null);
}
