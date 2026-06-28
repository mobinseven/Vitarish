using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Infrastructure;

public sealed class DistributedAuthenticationChallengeStore(IDistributedCache cache) : IAuthenticationChallengeStore
{
    private const string KeyPrefix = "authentication:challenge:";

    public async Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(AuthenticationChallengeSnapshot.FromChallenge(challenge));
        await cache.SetAsync(
            KeyPrefix + challenge.Id.Value,
            data,
            new DistributedCacheEntryOptions { AbsoluteExpiration = challenge.ExpiresAt },
            cancellationToken);
    }

    public async Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken)
    {
        byte[]? data = await cache.GetAsync(KeyPrefix + id.Value, cancellationToken);
        AuthenticationChallengeSnapshot? snapshot = data is null ? null : JsonSerializer.Deserialize<AuthenticationChallengeSnapshot>(data);
        return snapshot?.ToChallenge();
    }

    public async Task ReplaceAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(AuthenticationChallengeSnapshot.FromChallenge(challenge));
        await cache.SetAsync(
            KeyPrefix + challenge.Id.Value,
            data,
            new DistributedCacheEntryOptions { AbsoluteExpiration = challenge.ExpiresAt },
            cancellationToken);
    }

    private sealed record AuthenticationChallengeSnapshot(
        string Id,
        AuthenticationChallengePurpose Purpose,
        string UserId,
        string Destination,
        string TokenPurpose,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        int MaximumAttempts,
        int FailedAttempts,
        AuthenticationChallengeStatus Status,
        DateTimeOffset? CompletedAt)
    {
        public static AuthenticationChallengeSnapshot FromChallenge(AuthenticationChallenge challenge) => new(
            challenge.Id.Value,
            challenge.Purpose,
            challenge.UserId,
            challenge.Destination,
            challenge.TokenPurpose,
            challenge.CreatedAt,
            challenge.ExpiresAt,
            challenge.MaximumAttempts,
            challenge.FailedAttempts,
            challenge.Status,
            challenge.CompletedAt);

        public AuthenticationChallenge ToChallenge() => AuthenticationChallenge.Restore(
            new AuthenticationChallengeId(Id),
            Purpose,
            UserId,
            Destination,
            TokenPurpose,
            CreatedAt,
            ExpiresAt,
            MaximumAttempts,
            FailedAttempts,
            Status,
            CompletedAt);
    }
}
