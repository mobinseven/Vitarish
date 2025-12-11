using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Vitarish.Clients.Gateway.Abstractions;
using Vitarish.Clients.Gateway.Contracts;
using Vitarish.Clients.Gateway.Extensions;
using Vitarish.Clients.Gateway.Options;

namespace Vitarish.Clients.Gateway.Internal;

public class ApiClientService(
    IIdentityClientFactory aaasClientFactory,
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache,
    IClientIdentityContext clientIdentityContext,
    IOptions<IdentityOptions> options) : IApiClientService
{
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    public async Task<ApiClientDto?> GetApiClient(string clientId, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await aaasClientFactory.CreateClient("/api/apiclients/");
        using HttpRequestMessage request = new(HttpMethod.Get, clientId);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ApiClientDto>(cancellationToken: cancellationToken);
    }
    private string accessTokenCacheKey(string clientId) => clientId + ".ApiAccessToken";
    public async Task<string?> GetAccessTokenAsync(ApiClientDto apiClient)
    {
        string cacheKey = accessTokenCacheKey(apiClient.Id);
        string? cachedToken = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        await tokenLock.WaitAsync();
        try
        {
            cachedToken = await cache.GetStringAsync(cacheKey); // Double-check after locking
            if (!string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            HttpClient client = httpClientFactory.CreateClient();
            DiscoveryDocumentResponse discoveryDocument = await client.GetDiscoveryDocumentAsync(clientIdentityContext.Authority);
            if (discoveryDocument.IsError)
            {
                throw new Exception($"Error retrieving discovery document: {discoveryDocument.Error}");
            }

            TokenResponse tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = apiClient.Id,
                ClientSecret = apiClient.Secret,
                Scope = apiClient.Scopes,
            });

            if (tokenResponse.IsError || tokenResponse.AccessToken is null)
            {
                throw new Exception($"Error retrieving token: {tokenResponse.Error}");
            }

            TimeSpan expiration = TimeSpan.FromSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 1)); // Refresh token 1 min before expiry
            await cache.SetStringAsync(
                cacheKey,
                tokenResponse.AccessToken,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });

            return tokenResponse.AccessToken;
        }
        finally
        {
            _ = tokenLock.Release();
        }
    }

    private async Task<(ApiClientDto client, string token)> GetClientAndToken(string targetClientId)
    {
        string apiClientId = $"api.{targetClientId}" + (options.Value.Local ?? false ? ".local" : options.Value.Dev ?? false ? ".dev" : string.Empty);
        ApiClientDto client = await GetApiClient(apiClientId) ?? throw new Exception("Api client retrivation failed");
        string token = await GetAccessTokenAsync(client) ?? throw new Exception("Null token");
        return (client, token);
    }

    public async Task<HttpClient> CreateClient(string targetClientId, string targetResourceId)
    {
        (ApiClientDto client, string token) = await GetClientAndToken(targetClientId);

        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://" + client.HostUri);
        httpClient.SetBearerToken(token);
        httpClient.BaseAddress = new Uri(UrlHelper.UrlCombine(httpClient.BaseAddress!.AbsoluteUri, targetResourceId));
        return httpClient;
    }
}
