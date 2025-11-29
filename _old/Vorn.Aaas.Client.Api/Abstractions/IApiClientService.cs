using Vorn.Aaas.Client.Api.Models;

namespace Vorn.Aaas.Client.Api.Abstractions;

/// <summary>
/// Factory/service for creating HTTP clients and retrieving API client credentials.
/// </summary>
public interface IApiClientService
{
    /// <summary>
    /// Creates a configured <see cref="HttpClient"/> for the specified endpoint.
    /// </summary>
    Task<HttpClient> CreateClient(string targetClientId, string targetResourceId, string? clientId = null);

    /// <summary>
    /// Retrieves an access token for the specified <see cref="ApiClientDto"/>.
    /// </summary>
    /// <returns>The access token if successful; otherwise, null.</returns>
    Task<string?> GetAccessTokenAsync(ApiClientDto apiClient);

    /// <summary>
    /// Looks up an API client by id.
    /// </summary>
    /// <returns>The <see cref="ApiClientDto"/> if found; otherwise, null.</returns>
    Task<ApiClientDto?> GetApiClient(string clientId, CancellationToken cancellationToken = default);
}
