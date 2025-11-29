namespace Vorn.Aaas.Client.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Context;
using Vorn.Aaas.Client.Models;

public interface IPersonalAccessTokenClient
{
    Task<string?> CreateTokenAsync(
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default);
}

public sealed class PersonalAccessTokenClient(
    IRequestIdentityContext identityContext,
    IAaasClientFactory clientFactory,
    ILogger<PersonalAccessTokenClient> logger,
    IMemoryCache memoryCache) : IPersonalAccessTokenClient
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
        string userId = identityContext.UserId ??
            throw new InvalidOperationException("User identifier is required to create a personal access token.");
        List<string>? scopeList = scopes?
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .ToList();
        string scopeKey = scopeList is { Count: > 0 }
            ? string.Join('\u001f', scopeList.OrderBy(static scope => scope, StringComparer.Ordinal))
            : string.Empty;
        CacheKey cacheKey = new(userId, lifetimeDays, scopeKey, isReferenceToken);

        if (memoryCache.TryGetValue(cacheKey, out string? cachedToken) && cachedToken is not null)
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
            memoryCache.Set(cacheKey, token.Token, TimeSpan.FromDays(days));
        }

        return token.Token;
    }

    private readonly record struct CacheKey(string UserId, int? LifetimeDays, string Scopes, bool IsReferenceToken);
}
