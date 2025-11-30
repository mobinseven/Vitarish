using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vitarish.Web.Clients.Abstractions;
using Vitarish.Web.Clients.Constants;
using Vitarish.Web.Clients.Extensions;
using Vitarish.Web.Clients.Options;

namespace Vitarish.Web.Clients.Services;

/// <summary>
/// Factory for creating HTTP clients with authentication to the identity server.
/// </summary>
internal class IdentityClientFactory(
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache,
    IOptions<VitarishOptions> options) : IIdentityClientFactory
{
    private const string TokenCacheKeyPrefix = "IdentityApiAccessToken";
    private const int RefreshBufferSeconds = 60;
    private static readonly TimeSpan MinimumTokenLifetime = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string? tokenCacheKey;
    private readonly SemaphoreSlim tokenLock = new(1, 1);

    /// <inheritdoc />
    public async Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null)
    {
        HttpClient httpClient = httpClientFactory.CreateClient();
        string token = await GetAccessTokenAsync();
        httpClient.BaseAddress = new Uri(endpoint is null
            ? options.Value.Identity.Authority
            : UrlExtensions.UrlCombine(options.Value.Identity.Authority, endpoint));
        httpClient.SetBearerToken(token);
        return httpClient;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (TryGetValidToken(out string? cachedToken))
        {
            return cachedToken;
        }

        await tokenLock.WaitAsync();
        try
        {
            if (TryGetValidToken(out cachedToken))
            {
                return cachedToken;
            }

            HttpClient client = httpClientFactory.CreateClient();
            DiscoveryDocumentResponse discoveryDocument = await client.GetDiscoveryDocumentAsync(options.Value.Identity.Authority);
            if (discoveryDocument.IsError)
            {
                throw new InvalidOperationException($"Error retrieving discovery document: {discoveryDocument.Error}");
            }

            TokenResponse tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = AuthConstants.AccessClient,
                ClientSecret = options.Value.Identity.Secret,
                Scope = AuthConstants.AccessScope,
            });

            if (tokenResponse.IsError || tokenResponse.AccessToken is null)
            {
                throw new InvalidOperationException($"Error retrieving token: {tokenResponse.Error}");
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
        string composite = string.Join(":", options.Value.Identity.Authority, AuthConstants.AccessClient, AuthConstants.AccessScope, options.Value.Identity.Secret);
        byte[] data = Encoding.UTF8.GetBytes(composite);
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }
}
