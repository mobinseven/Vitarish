using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Infrastructure;

public sealed class DistributedAuthenticationChallengeStore(IDistributedCache cache, IConnectionMultiplexer redis) : IAuthenticationChallengeStore
{
    private const string KeyPrefix = "authentication:challenge:";
    private const string ConsumedKeyPrefix = "authentication:challenge-consumed:";
    private const string FailedKeyPrefix = "authentication:challenge-failed:";
    private const string InFlightKeyPrefix = "authentication:challenge-in-flight:";
    private const string CompletionKeyPrefix = "authentication:challenge-completion:";

    private static string ConsumedKey(string challengeId) => $"{ConsumedKeyPrefix}{{{challengeId}}}";
    private static string FailedKey(string challengeId) => $"{FailedKeyPrefix}{{{challengeId}}}";
    private static string InFlightKey(string challengeId) => $"{InFlightKeyPrefix}{{{challengeId}}}";
    private static string CompletionKey(string challengeId) => $"{CompletionKeyPrefix}{{{challengeId}}}";

    private const string TryReserveAttemptScript = """
        if redis.call('EXISTS', KEYS[2]) == 1 or redis.call('EXISTS', KEYS[4]) == 1 then return 0 end
        local failed = tonumber(redis.call('GET', KEYS[1]) or '0')
        local in_flight = tonumber(redis.call('GET', KEYS[3]) or '0')
        if failed + in_flight >= tonumber(ARGV[1]) then return 0 end
        local reserved = redis.call('INCR', KEYS[3])
        if reserved == 1 then redis.call('PEXPIRE', KEYS[3], ARGV[2]) end
        return 1
        """;

    private const string ReleaseAttemptScript = """
        local in_flight = tonumber(redis.call('GET', KEYS[1]) or '0')
        if in_flight <= 0 then return 0 end
        if in_flight == 1 then redis.call('DEL', KEYS[1]) else redis.call('DECR', KEYS[1]) end
        return 1
        """;

    private const string TryConsumeScript = """
        if tonumber(ARGV[2]) == 1 and redis.call('EXISTS', KEYS[3]) == 0 then
            local in_flight = tonumber(redis.call('GET', KEYS[2]) or '0')
            if in_flight > 0 then
                if in_flight == 1 then redis.call('DEL', KEYS[2]) else redis.call('DECR', KEYS[2]) end
            end
            return 0
        end
        if redis.call('EXISTS', KEYS[1]) == 1 then
            local in_flight = tonumber(redis.call('GET', KEYS[2]) or '0')
            if in_flight > 0 then
                if in_flight == 1 then redis.call('DEL', KEYS[2]) else redis.call('DECR', KEYS[2]) end
            end
            redis.call('DEL', KEYS[3])
            return 0
        end
        local result = redis.call('SET', KEYS[1], '1', 'PX', ARGV[1], 'NX')
        if not result then return 0 end
        local in_flight = tonumber(redis.call('GET', KEYS[2]) or '0')
        if in_flight > 0 then
            if in_flight == 1 then redis.call('DEL', KEYS[2]) else redis.call('DECR', KEYS[2]) end
        end
        redis.call('DEL', KEYS[3])
        return 1
        """;

    private const string TryRegisterFailureScript = """
        local in_flight = tonumber(redis.call('GET', KEYS[3]) or '0')
        if in_flight <= 0 then return 0 end
        if in_flight == 1 then redis.call('DEL', KEYS[3]) else redis.call('DECR', KEYS[3]) end
        if redis.call('EXISTS', KEYS[2]) == 1 or redis.call('EXISTS', KEYS[4]) == 1 then return 0 end
        local failed = redis.call('INCR', KEYS[1])
        if failed == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[2]) end
        if failed > tonumber(ARGV[1]) then return 0 end
        return failed
        """;

    private const string TryReserveCompletionScript = """
        if redis.call('EXISTS', KEYS[1]) == 1 then return 0 end
        if tonumber(redis.call('GET', KEYS[2]) or '0') <= 0 then return 0 end
        local result = redis.call('SET', KEYS[3], '1', 'PX', ARGV[1], 'NX')
        if not result then return 0 end
        return 1
        """;

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
        AuthenticationChallenge? challenge = snapshot?.ToChallenge();
        if (challenge is { Status: AuthenticationChallengeStatus.Pending })
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (await redis.GetDatabase().KeyExistsAsync(ConsumedKey(id.Value)))
                challenge.Complete(now);
            else if (await redis.GetDatabase().StringGetAsync(FailedKey(id.Value)) is RedisValue failedValue &&
                     long.TryParse(failedValue.ToString(), out long failedAttempts))
            {
                for (long attempt = challenge.FailedAttempts; attempt < Math.Min(failedAttempts, challenge.MaximumAttempts); attempt++)
                    challenge.RegisterFailure(now);
            }
        }

        return challenge;
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

    public async Task<bool> TryReserveAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TimeSpan lifetime = challenge.ExpiresAt - DateTimeOffset.UtcNow;
        if (lifetime <= TimeSpan.Zero)
            return false;

        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(
            TryReserveAttemptScript,
            [FailedKey(challenge.Id.Value), ConsumedKey(challenge.Id.Value), InFlightKey(challenge.Id.Value), CompletionKey(challenge.Id.Value)],
            [challenge.MaximumAttempts, Math.Max(1, (long)lifetime.TotalMilliseconds)]);
        return (long)result == 1;
    }

    public async Task ReleaseAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await redis.GetDatabase().ScriptEvaluateAsync(
            ReleaseAttemptScript,
            [InFlightKey(challenge.Id.Value)]);
    }

    public async Task<bool> TryReserveCompletionAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TimeSpan lifetime = challenge.ExpiresAt - DateTimeOffset.UtcNow;
        if (lifetime <= TimeSpan.Zero)
            return false;

        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(
            TryReserveCompletionScript,
            [ConsumedKey(challenge.Id.Value), InFlightKey(challenge.Id.Value), CompletionKey(challenge.Id.Value)],
            [Math.Max(1, (long)lifetime.TotalMilliseconds)]);
        return (long)result == 1;
    }

    public async Task ReleaseCompletionAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await redis.GetDatabase().KeyDeleteAsync(CompletionKey(challenge.Id.Value));
    }

    public async Task<bool> TryConsumeAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TimeSpan lifetime = challenge.ExpiresAt - DateTimeOffset.UtcNow;
        if (lifetime <= TimeSpan.Zero)
            return false;

        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(
            TryConsumeScript,
            [ConsumedKey(challenge.Id.Value), InFlightKey(challenge.Id.Value), CompletionKey(challenge.Id.Value)],
            [
                Math.Max(1, (long)lifetime.TotalMilliseconds),
                challenge.Purpose is AuthenticationChallengePurpose.EmailPasswordRecovery or AuthenticationChallengePurpose.PhonePasswordRecovery ? 1 : 0
            ]);
        return (long)result == 1;
    }

    public async Task<int> TryRegisterFailureAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TimeSpan lifetime = challenge.ExpiresAt - DateTimeOffset.UtcNow;
        if (lifetime <= TimeSpan.Zero)
            return 0;

        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(
            TryRegisterFailureScript,
            [FailedKey(challenge.Id.Value), ConsumedKey(challenge.Id.Value), InFlightKey(challenge.Id.Value), CompletionKey(challenge.Id.Value)],
            [challenge.MaximumAttempts, Math.Max(1, (long)lifetime.TotalMilliseconds)]);
        long failedAttempts = (long)result;
        return failedAttempts > int.MaxValue ? int.MaxValue : (int)failedAttempts;
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
        DateTimeOffset? CompletedAt,
        bool RequiresProfileCompletion = false)
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
            challenge.CompletedAt,
            challenge.RequiresProfileCompletion);

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
            CompletedAt,
            RequiresProfileCompletion);
    }
}
