using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Vitarish.Client.Context;

namespace Vitarish.Clients.Interface.Identity.Services;

public sealed class PersonalAccessTokenClient(
    IRequestContextClient identityContext,
    IIdentityClientService clientFactory,
    ILogger<PersonalAccessTokenClient> logger,
    IDistributedCache cache) : IPersonalAccessTokenClient
{
    private const string BaseUrl = "api/pat";

    public async Task<string?> CreateTokenAsync(
        int? lifetimeDays,
        IEnumerable<string>? scopes,
        bool isReferenceToken,
        CancellationToken cancellationToken)
    {
        if (lifetimeDays is { } value && value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetimeDays));
        }
        string userId = identityContext.Request?.UserId ??
            throw new InvalidOperationException("User identifier is required to create a personal access token.");
        List<string>? scopeList = scopes?
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .ToList();
        string scopeKey = scopeList is { Count: > 0 }
            ? string.Join('\u001f', scopeList.OrderBy(static scope => scope, StringComparer.Ordinal))
            : string.Empty;
        string cacheKey = BuildCacheKey(userId, lifetimeDays, scopeKey, isReferenceToken);

        string? cachedToken = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedToken))
        {
            logger.LogInformation("Returning cached personal access token.");
            return cachedToken;
        }

        HttpClient httpClient = await clientFactory.CreateClient(BaseUrl);

        CreatePersonalAccessTokenRequest payload = new(
            userId,
            lifetimeDays,
            scopeList is { Count: > 0 } ? scopeList : null,
            isReferenceToken);

        logger.LogInformation("Requesting personal access token generation.");

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            string.Empty,
            payload,
            PersonalAccessTokenJsonContext.Default.CreatePersonalAccessTokenRequest,
            cancellationToken);

        _ = response.EnsureSuccessStatusCode();

        PersonalAccessTokenResponse? token = await response.Content.ReadFromJsonAsync(
            PersonalAccessTokenJsonContext.Default.PersonalAccessTokenResponse,
            cancellationToken);

        if (token is null)
        {
            return null;
        }

        logger.LogInformation("Personal access token generation completed successfully.");

        if (lifetimeDays is { } days)
        {
            await cache.SetStringAsync(
                cacheKey,
                token.Token,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(days) },
                cancellationToken);
        }

        return token.Token;
    }

    private static string BuildCacheKey(string userId, int? lifetimeDays, string scopeKey, bool isReferenceToken) =>
        $"pat:{userId}:{lifetimeDays?.ToString() ?? "null"}:{scopeKey}:{isReferenceToken}";
}
