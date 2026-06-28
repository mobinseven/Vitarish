using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Infrastructure;

public sealed class DistributedAuthenticationChallengeStore(IDistributedCache cache) : IAuthenticationChallengeStore
{
    public async Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(AuthenticationChallengeSnapshot.FromChallenge(challenge));
        await cache.SetAsync(
            challenge.Id.Value,
            data,
            new DistributedCacheEntryOptions { AbsoluteExpiration = challenge.ExpiresAt },
            cancellationToken);
    }

    public async Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken)
    {
        byte[]? data = await cache.GetAsync(id.Value, cancellationToken);
        AuthenticationChallengeSnapshot? snapshot = data is null ? null : JsonSerializer.Deserialize<AuthenticationChallengeSnapshot>(data);
        return snapshot?.ToChallenge();
    }

    private sealed record AuthenticationChallengeSnapshot(
        string Id,
        AuthenticationChallengePurpose Purpose,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        int MaximumAttempts,
        int FailedAttempts,
        AuthenticationChallengeStatus Status)
    {
        public static AuthenticationChallengeSnapshot FromChallenge(AuthenticationChallenge challenge) => new(
            challenge.Id.Value,
            challenge.Purpose,
            challenge.CreatedAt,
            challenge.ExpiresAt,
            challenge.MaximumAttempts,
            challenge.FailedAttempts,
            challenge.Status);

        public AuthenticationChallenge ToChallenge() => AuthenticationChallenge.Restore(
            new AuthenticationChallengeId(Id),
            Purpose,
            CreatedAt,
            ExpiresAt,
            MaximumAttempts,
            FailedAttempts,
            Status);
    }
}
