using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Client.Context;

namespace Vitarish.Clients.Interface.Identity.Services;

public sealed class PersonalAccessTokenClient(
    IIdentityClientService clientFactory,
    ILogger<PersonalAccessTokenClient> logger,
    HybridCache cache) : IPersonalAccessTokenClient
{
    private const string BaseUrl = "api/pat";

    // Small safety window so you don't serve a token right at its expiry edge.
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromMinutes(5);

    public async Task<string?> CreateTokenAsync(
        string userId,
        int? lifetimeDays,
        IEnumerable<string>? scopes,
        bool isReferenceToken,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentNullException.ThrowIfNull(lifetimeDays);
        ArgumentOutOfRangeException.ThrowIfLessThan(lifetimeDays.Value, 1);

        string[]? normalizedScopes = scopes?
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => s.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static s => s, StringComparer.Ordinal)
            .ToArray();

        string cacheKey = BuildHashedCacheKey(userId, lifetimeDays.Value, normalizedScopes, isReferenceToken);

        TimeSpan ttl = ComputeTtl(lifetimeDays.Value);

        var entryOptions = new HybridCacheEntryOptions
        {
            // Expiration applies to the distributed backend; LocalCacheExpiration controls the in-memory tier.
            Expiration = ttl,
            LocalCacheExpiration = ttl,
        };

        // Optional: tags allow invalidation (e.g., if you revoke tokens for a user).
        // Keep tags non-PII (hash userId).
        var tags = new[] { $"pat-user:{HashToHex(userId)}" };

        try
        {
            // TState overload lets us keep the factory static (no captures).
            return await cache.GetOrCreateAsync(
                cacheKey,
                (clientFactory, logger, userId, lifetimeDays.Value, normalizedScopes, isReferenceToken),
                static async (state, ct) =>
                {
                    (IIdentityClientService? clientFactory, ILogger<PersonalAccessTokenClient>? logger, string? userId, int lifetimeDays, string[]? scopes, bool isReferenceToken) = state;

                    HttpClient httpClient = await clientFactory.CreateClient(BaseUrl);

                    var payload = new CreatePersonalAccessTokenRequest(
                        userId,
                        lifetimeDays,
                        scopes is { Length: > 0 } ? scopes : null,
                        isReferenceToken);

                    logger.LogInformation("Requesting personal access token generation.");

                    using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                        string.Empty,
                        payload,
                        PersonalAccessTokenJsonContext.Default.CreatePersonalAccessTokenRequest,
                        ct);

                    _ = response.EnsureSuccessStatusCode();

                    PersonalAccessTokenResponse? token = await response.Content.ReadFromJsonAsync(
                        PersonalAccessTokenJsonContext.Default.PersonalAccessTokenResponse,
                        ct);

                    if (string.IsNullOrWhiteSpace(token?.Token))
                    {
                        // Throw so HybridCache doesn't cache a bad/empty result.
                        throw new InvalidOperationException("PAT endpoint returned an empty token payload.");
                    }

                    logger.LogInformation("Personal access token generation completed successfully.");
                    return token.Token;
                },
                entryOptions,
                tags,
                cancellationToken);
        }
        catch (InvalidOperationException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Token generation returned an invalid payload; not caching.");
            return null;
        }
    }

    private static TimeSpan ComputeTtl(int lifetimeDays)
    {
        TimeSpan ttl = TimeSpan.FromDays(lifetimeDays) - ExpirySkew;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromDays(lifetimeDays);
    }

    private static string BuildHashedCacheKey(string userId, int lifetimeDays, string[]? scopes, bool isReferenceToken)
    {
        // Hash the “identity” of this request to keep keys short, avoid key-length issues,
        // and avoid raw identifiers in cache keys (recommended key hygiene).
        var sb = new StringBuilder(256);
        _ = sb.Append("pat|uid=").Append(userId)
          .Append("|days=").Append(lifetimeDays)
          .Append("|ref=").Append(isReferenceToken);

        if (scopes is { Length: > 0 })
        {
            _ = sb.Append("|scopes=");
            foreach (var s in scopes)
            {
                _ = sb.Append(s).Append('\u001f');
            }
        }

        return "pat:" + HashToHex(sb.ToString());
    }

    private static string HashToHex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
