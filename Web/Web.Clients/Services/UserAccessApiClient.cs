using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using Vitarish.Web.Clients.Abstractions;
using Vitarish.Web.Clients.Context;
using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Services;

/// <summary>
/// API client for user access operations.
/// </summary>
public class UserAccessApiClient(IIdentityClientFactory clientFactory, ILogger<UserAccessApiClient> logger) : IUserAccessApiClient
{
    private const string BaseUrl = "api/users/";

    private Task<HttpClient> CreateHttpClientAsync(string? clientId = null) =>
        clientFactory.CreateClient(BaseUrl, clientId);

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

    /// <inheritdoc />
    public async Task<IEnumerable<ClaimDto>?> GetUserClaims(string userId, CancellationToken cancellationToken = default) =>
        await GetQuery($"{userId}/claims", UserAccessJsonContext.Default.IEnumerableClaimDto, cancellationToken);

    /// <inheritdoc />
    public async Task<ClaimDto?> GetUserAccessClaim(string userId, string clientId, CancellationToken cancellationToken = default) =>
        await GetQuery($"{userId}/access/{clientId}", UserAccessJsonContext.Default.ClaimDto, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasUserAccess(string userId, string clientId, CancellationToken cancellationToken = default) =>
        await GetQuery($"{userId}/hasaccess/{clientId}", UserAccessJsonContext.Default.Boolean, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>?> GetClientUsers(string clientId, CancellationToken cancellationToken = default) =>
        await GetQuery($"clients/{clientId}", UserAccessJsonContext.Default.IEnumerableUserDto, cancellationToken);

    /// <inheritdoc />
    public async Task<UserDto?> GetClientUser(string clientId, string userId, CancellationToken cancellationToken = default) =>
        await GetQuery($"clients/{clientId}/{userId}", UserAccessJsonContext.Default.UserDto, cancellationToken);

    /// <inheritdoc />
    public async Task SetUserAccess(string userId, string clientId, string accessValue, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateHttpClientAsync();
        SetUserAccessRequest payload = new(userId, clientId, accessValue);
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "access", payload, UserAccessJsonContext.Default.SetUserAccessRequest, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
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
