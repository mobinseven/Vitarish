using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Constants;
using Vitarish.Clients.Identity.Extensions;
using Vitarish.Clients.Identity.Options;

namespace Vitarish.Clients.Identity.Internal;

public class IdentityClientService(
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache,
    IOptions<IdentityClientOptions> options) : IIdentityClientService
{
    private const string TokenCacheKeyPrefix = "IdentityApiAccessToken";
    private const int RefreshBufferSeconds = 60;
    private static readonly TimeSpan MinimumTokenLifetime = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string? tokenCacheKey;
    private readonly SemaphoreSlim tokenLock = new(1, 1);

    public async Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null)
    {
        HttpClient httpClient = httpClientFactory.CreateClient();
        string token = await GetAccessTokenAsync();
        httpClient.BaseAddress = new Uri(endpoint is null ? options.Value.Authority : UrlHelper.UrlCombine(options.Value.Authority, endpoint));
        httpClient.SetBearerToken(token);
        return httpClient;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        //if (TryGetValidToken(out string? cachedToken))
        //{
        //    return cachedToken;
        //}

        await tokenLock.WaitAsync();
        try
        {
            //if (TryGetValidToken(out cachedToken))
            //{
            //    return cachedToken;
            //}

            HttpClient client = httpClientFactory.CreateClient();
            DiscoveryDocumentResponse discoveryDocument = await client.GetDiscoveryDocumentAsync(options.Value.Authority);
            if (discoveryDocument.IsError)
            {
                throw new Exception($"Error retrieving discovery document: {discoveryDocument.Error}");
            }

            TokenResponse tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = IdentityConstants.AccessClient,
                ClientSecret = options.Value.Secret,
                Scope = IdentityConstants.AccessScope,
            });

            if (tokenResponse.IsError || tokenResponse.AccessToken is null)
            {
                throw new Exception($"Error retrieving token: {tokenResponse.Error}");
            }

            TimeSpan lifetime = TimeSpan.FromSeconds(Math.Max(tokenResponse.ExpiresIn - RefreshBufferSeconds, MinimumTokenLifetime.TotalSeconds));
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
            CachedAccessToken accessToken = new(tokenResponse.AccessToken, expiresAt);
            cache.SetString(
                GetTokenCacheKey(),
                Serialize(accessToken),
                new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt });

            return accessToken.Value;
        }
        finally
        {
            _ = tokenLock.Release();
        }
    }

    private bool TryGetValidToken([NotNullWhen(true)] out string? token)
    {
        if (Deserialize(cache.GetString(GetTokenCacheKey())) is { } cachedToken)
        {
            if (cachedToken.ExpiresAt > DateTimeOffset.UtcNow)
            {
                token = cachedToken.Value;
                return true;
            }

            cache.Remove(GetTokenCacheKey());
        }

        token = null;
        return false;
    }

    private sealed record CachedAccessToken(string Value, DateTimeOffset ExpiresAt);

    private static string Serialize(CachedAccessToken token) => JsonSerializer.Serialize(token, JsonOptions);

    private static CachedAccessToken? Deserialize(string? payload) =>
        payload is null ? null : JsonSerializer.Deserialize<CachedAccessToken>(payload, JsonOptions);

    private string GetTokenCacheKey() => tokenCacheKey ??=
        $"{TokenCacheKeyPrefix}:{CreateConfigurationHash()}";

    private string CreateConfigurationHash()
    {
        string composite = string.Join(":", options.Value.Authority, IdentityConstants.AccessClient, IdentityConstants.AccessScope, options.Value.Secret);
        byte[] data = Encoding.UTF8.GetBytes(composite);
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }

}
