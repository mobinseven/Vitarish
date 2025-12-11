using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using Vitarish.Client.Context;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Contracts;

namespace Vitarish.Clients.Interface.Identity.Services;

public class UserAccessApiClient(IIdentityClientService clientFactory, ILogger<UserAccessApiClient> logger) : IUserAccessApiClient
{
    private const string BaseUrl = "api/users/";
    private Task<HttpClient> CreateHttpClientAsync(string? clientId = null) => clientFactory.CreateClient(BaseUrl, clientId);

    private async Task<T?> GetQuery<T>(string query, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateHttpClientAsync();
        logger.LogInformation("Sending GET query {Query}", query);
        using HttpRequestMessage request = new(HttpMethod.Get, query);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        T? resultT = await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken);
        logger.LogInformation("GET query {Query} finished", query);
        return resultT;
    }

    /// <summary>
    /// Get all claims for a user.
    /// GET /api/users/{userId}/claims
    /// </summary>
    public async Task<IEnumerable<ClaimDto>?> GetUserClaims(string userId, CancellationToken cancellationToken = default) => await GetQuery($"{userId}/claims", UserAccessJsonContext.Default.IEnumerableClaimDto, cancellationToken);

    /// <summary>
    /// Get a user's access claim for a client.
    /// GET /api/users/{userId}/access/{clientId}
    /// </summary>
    public async Task<ClaimDto?> GetUserAccessClaim(string userId, string clientId, CancellationToken cancellationToken = default) => await GetQuery($"{userId}/access/{clientId}", UserAccessJsonContext.Default.ClaimDto, cancellationToken);

    /// <summary>
    /// Check whether a user has access to a client.
    /// GET /api/users/{userId}/hasaccess/{clientId}
    /// </summary>
    public async Task<bool> HasUserAccess(string userId, string clientId, CancellationToken cancellationToken = default) => await GetQuery($"{userId}/hasaccess/{clientId}", UserAccessJsonContext.Default.Boolean, cancellationToken);


    /// <summary>
    /// List all users with access to a client.
    /// GET /api/users/clients/{clientId}
    /// </summary>
    public async Task<IEnumerable<UserDto>?> GetClientUsers(string clientId, CancellationToken cancellationToken = default) => await GetQuery($"clients/{clientId}", UserAccessJsonContext.Default.IEnumerableUserDto, cancellationToken);

    /// <summary>
    /// Get a specific user with access to a client.
    /// GET /api/users/clients/{clientId}/{userId}
    /// </summary>
    public async Task<UserDto?> GetClientUser(string clientId, string userId, CancellationToken cancellationToken = default) => await GetQuery($"clients/{clientId}/{userId}", UserAccessJsonContext.Default.UserDto, cancellationToken);

    /// <summary>
    /// Grant or update a user's access claim.
    /// POST /api/users/access
    /// </summary>
    public async Task SetUserAccess(string userId, string clientId, string accessValue, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateHttpClientAsync();
        SetUserAccessRequest payload = new(userId, clientId, accessValue);
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("access", payload, UserAccessJsonContext.Default.SetUserAccessRequest, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Remove a user's access claim.
    /// DELETE /api/users/access
    /// </summary>
    public async Task RemoveUserAccess(string userId, string clientId, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateHttpClientAsync();
        RemoveUserAccessRequest payload = new(userId, clientId);
        using HttpRequestMessage request = new(HttpMethod.Delete, "access")
        {
            Content = JsonContent.Create(payload, options: UserAccessJsonContext.Default.Options)
        };

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
    }
}
