using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Models;
using Vorn.Aaas.Client.Api.Options;

namespace Vorn.Aaas.Client.Api.Internal;

public class ApiClientService(
    IAaasClientFactory aaasClientFactory,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IRequestIdentityContext vornContext,
    IOptions<VornOptions> options) : IApiClientService
{
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    public async Task<ApiClientDto?> GetApiClient(string clientId, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await aaasClientFactory.CreateClient("/api/apiclients/");
        using HttpRequestMessage request = new(HttpMethod.Get, clientId);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ApiClientDto>(cancellationToken: cancellationToken);
    }
    private string accessTokenCacheKey(string clientId) => clientId + ".ApiAccessToken";
    public async Task<string?> GetAccessTokenAsync(ApiClientDto apiClient)
    {
        if (cache.TryGetValue(accessTokenCacheKey(apiClient.Id), out string? cachedToken))
        {
            return cachedToken;
        }

        await tokenLock.WaitAsync();
        try
        {
            if (cache.TryGetValue(accessTokenCacheKey(apiClient.Id), out cachedToken)) // Double-check after locking
            {
                return cachedToken;
            }

            HttpClient client = httpClientFactory.CreateClient();
            DiscoveryDocumentResponse discoveryDocument = await client.GetDiscoveryDocumentAsync(vornContext.Authority);
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

            TimeSpan expiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60); // Refresh token 1 min before expiry
            cache.Set(accessTokenCacheKey(apiClient.Id), tokenResponse.AccessToken, expiration);

            return tokenResponse.AccessToken;
        }
        finally
        {
            tokenLock.Release();
        }
    }

    private async Task<(ApiClientDto client, string token)> GetClientAndToken(string targetClientId)
    {
        string apiClientId = $"api.{targetClientId}" + (options.Value.Aaas.Local ?? false ? ".local" : options.Value.Aaas.Dev ?? false ? ".dev" : string.Empty);
        ApiClientDto client = await GetApiClient(apiClientId) ?? throw new Exception($"No such client: {apiClientId}");
        string token = await GetAccessTokenAsync(client) ?? throw new Exception("Null token");
        return (client, token);
    }

    public async Task<HttpClient> CreateClient(string targetClientId, string targetResourceId, string? clientId = null)
    {
        (ApiClientDto client, string token) = await GetClientAndToken(targetClientId);

        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://" + client.HostUri);
        RequestIdentityContextHeaders.Apply(httpClient.DefaultRequestHeaders, vornContext, clientId);
        httpClient.SetBearerToken(token);
        httpClient.BaseAddress = new Uri(UrlHelper.UrlCombine(httpClient.BaseAddress!.AbsoluteUri, targetResourceId));
        return httpClient;
    }
}
