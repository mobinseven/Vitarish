using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Options;

namespace Vorn.Aaas.Client.Api.Internal;

public class AaasClientFactory(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IRequestIdentityContext vornContext,
    IOptions<VornOptions> options) : IAaasClientFactory
{
    private const string TokenCacheKey = "AaasApiAccessToken";
    private const int RefreshBufferSeconds = 60;
    private static readonly TimeSpan MinimumTokenLifetime = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim tokenLock = new(1, 1);

    public async Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null)
    {
        HttpClient httpClient = httpClientFactory.CreateClient();
        string token = await GetAccessTokenAsync();
        httpClient.BaseAddress = new Uri(endpoint is null ? vornContext.Authority : UrlHelper.UrlCombine(vornContext.Authority, endpoint));
        RequestIdentityContextHeaders.Apply(httpClient.DefaultRequestHeaders, vornContext, clientId);
        httpClient.SetBearerToken(token);
        return httpClient;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if(TryGetValidToken(out string? cachedToken))
        {
            return cachedToken;
        }

        await tokenLock.WaitAsync();
        try
        {
            if(TryGetValidToken(out cachedToken))
            {
                return cachedToken;
            }

            HttpClient client = httpClientFactory.CreateClient();
            DiscoveryDocumentResponse discoveryDocument = await client.GetDiscoveryDocumentAsync(vornContext.Authority);
            if(discoveryDocument.IsError)
            {
                throw new Exception($"Error retrieving discovery document: {discoveryDocument.Error}");
            }

            TokenResponse tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = AaasConstants.AccessClient,
                ClientSecret = options.Value.Aaas.Secret,
                Scope = AaasConstants.AccessScope,
            });

            if(tokenResponse.IsError || tokenResponse.AccessToken is null)
            {
                throw new Exception($"Error retrieving token: {tokenResponse.Error}");
            }

            TimeSpan lifetime = TimeSpan.FromSeconds(Math.Max(tokenResponse.ExpiresIn - RefreshBufferSeconds, MinimumTokenLifetime.TotalSeconds));
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
            CachedAccessToken accessToken = new(tokenResponse.AccessToken, expiresAt);
            cache.Set(TokenCacheKey, accessToken, new MemoryCacheEntryOptions { AbsoluteExpiration = expiresAt });

            return accessToken.Value;
        }
        finally
        {
            tokenLock.Release();
        }
    }

    private bool TryGetValidToken([NotNullWhen(true)] out string? token)
    {
        if(cache.TryGetValue(TokenCacheKey, out CachedAccessToken? cachedToken) && cachedToken is not null)
        {
            if(cachedToken.ExpiresAt > DateTimeOffset.UtcNow)
            {
                token = cachedToken.Value;
                return true;
            }

            cache.Remove(TokenCacheKey);
        }

        token = null;
        return false;
    }

    private sealed record CachedAccessToken(string Value, DateTimeOffset ExpiresAt);
}
