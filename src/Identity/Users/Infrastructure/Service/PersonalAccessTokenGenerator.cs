
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class PersonalAccessTokenGenerator(
    UserManager<User> userManager,
    ITokenService tokenService,
    IIssuerNameService issuerNameService,
    IRequestContext requestContext,
    IOptions<PersonalAccessTokenOptions> preferences) : IPersonalAccessTokenGenerator
{
    private const string TokenProvider = nameof(Vitarish);
    private const string TokenName = "PersonalAccessToken";

    public async Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays,
        IEnumerable<string>? scopes,
        bool isReferenceToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        int defaultLifetimeDays = preferences.Value.DefaultLifetimeDays;
        if (defaultLifetimeDays < 1)
        {
            throw new InvalidOperationException("Configured personal access token lifetime must be at least one day.");
        }

        if (lifetimeDays is { } overrideLifetime && overrideLifetime < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetimeDays));
        }

        int effectiveLifetimeDays = lifetimeDays ?? defaultLifetimeDays;

        User user = await userManager.FindByIdAsync(subjectId)
            ?? throw new InvalidOperationException("User not found for personal access token generation.");

        IList<Claim> userClaims = await userManager.GetClaimsAsync(user);
        HashSet<string> allowedScopes = userClaims
            .Where(static claim => string.Equals(claim.Type, IdentityDefinitions.AccessClaim, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(claim.Value))
            .Select(static claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);
        IList<string> roleNames = await userManager.GetRolesAsync(user); // ASP.NET Core Identity roles :contentReference[oaicite:3]{index=3}
        HashSet<string> roleSet = roleNames
            .Where(static r => !string.IsNullOrWhiteSpace(r))
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> scopeSet = new(allowedScopes, StringComparer.Ordinal);
        List<string>? invalidScopes = null;

        if (scopes is not null)
        {
            foreach (string scope in scopes)
            {
                if (string.IsNullOrWhiteSpace(scope) ||
                    string.Equals(scope, IdentityDefinitions.AccessScope, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!allowedScopes.Any(c => c.StartsWith(scope)))
                {
                    (invalidScopes ??= []).Add(scope);
                    continue;
                }

                _ = scopeSet.Add(scope);
            }
        }

        if (invalidScopes is not null)
        {
            throw new InvalidOperationException($"Requested scopes are not permitted: {string.Join(", ", invalidScopes)}");
        }

        _ = scopeSet.Add(IdentityDefinitions.AccessScope);

        if (scopeSet.Count == 0)
        {
            throw new InvalidOperationException("At least one scope must be provided.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        PersonalAccessTokenCacheEntry? cached = await GetCachedTokenAsync(user, cancellationToken);
        if (cached is not null && cached.IsReusable(scopeSet, isReferenceToken, now))
        {
            return cached.Token;
        }

        TimeSpan lifetime = TimeSpan.FromDays(effectiveLifetimeDays);
        Token token = new(IdentityServerConstants.TokenTypes.AccessToken)
        {
            Issuer = await issuerNameService.GetCurrentAsync(),
            Lifetime = (int)lifetime.TotalSeconds,
            CreationTime = now.UtcDateTime,
            ClientId = requestContext.ClientId,
            AccessTokenType = isReferenceToken ? AccessTokenType.Reference : AccessTokenType.Jwt,
            Claims =
            [
                new Claim("client_id", requestContext.ClientId),
                new Claim(IdentityDefinitions.OwnerIdClaim, requestContext.OwnerId),
                new Claim(IdentityDefinitions.TenantIdClaim, requestContext.TenantId),
                new Claim("sub", subjectId)
            ]
        };

        foreach (string scope in scopeSet)
        {
            token.Audiences.Add(scope);
            token.Claims.Add(new Claim("scope", scope));
        }

        foreach (string role in roleSet)
        {
            token.Claims.Add(new Claim(JwtClaimTypes.Role, role));
        }

        string securityToken = await tokenService.CreateSecurityTokenAsync(token);

        PersonalAccessTokenCacheEntry entry = new(
            securityToken,
            now.Add(lifetime),
            scopeSet.ToArray(),
            isReferenceToken);

        await StoreTokenAsync(user, entry, cancellationToken);

        return securityToken;
    }

    private async Task<PersonalAccessTokenCacheEntry?> GetCachedTokenAsync(
        User user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? stored = await userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenName);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PersonalAccessTokenCacheEntry>(stored);
        }
        catch (JsonException)
        {
            _ = await userManager.RemoveAuthenticationTokenAsync(user, TokenProvider, TokenName);
            return null;
        }
    }

    private Task<IdentityResult> StoreTokenAsync(
        User user,
        PersonalAccessTokenCacheEntry entry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string payload = JsonSerializer.Serialize(entry);
        return userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenName, payload);
    }

    private sealed record PersonalAccessTokenCacheEntry(
        string Token,
        DateTimeOffset ExpiresAt,
        string[] Scopes,
        bool IsReferenceToken)
    {
        public bool IsReusable(ISet<string> requiredScopes, bool requiresReferenceToken, DateTimeOffset now) => ExpiresAt > now && IsReferenceToken == requiresReferenceToken && Scopes.Length == requiredScopes.Count &&
                new HashSet<string>(Scopes, StringComparer.Ordinal).SetEquals(requiredScopes);
    }
}
