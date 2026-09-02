using System.Collections.Concurrent;
using StackExchange.Redis;
using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class InMemoryAuthenticationAttemptTracker(TimeProvider? timeProvider = null) : IAuthenticationAttemptTracker
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, AttemptState> attempts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> gates = new(StringComparer.Ordinal);

    public Task<bool> IsDeniedAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string key = NormalizeKey(identifier);
        lock (gates.GetOrAdd(key, static _ => new object()))
        {
            DateTimeOffset now = clock.GetUtcNow();
            if (!attempts.TryGetValue(key, out AttemptState? state) || state.RetentionUntil <= now)
            {
                _ = attempts.TryRemove(key, out _);
                return Task.FromResult(false);
            }
            return Task.FromResult(state.DeniedUntil > now);
        }
    }

    public Task<int> RegisterFailureAsync(string identifier, int threshold, TimeSpan denialDuration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string key = NormalizeKey(identifier);
        lock (gates.GetOrAdd(key, static _ => new object()))
        {
            DateTimeOffset now = clock.GetUtcNow();
            if (attempts.TryGetValue(key, out AttemptState? existing) && existing.DeniedUntil > now)
                return Task.FromResult(threshold);
            int failures = existing is null || existing.FailureWindowExpiresAt <= now ? 1 : existing.Failures + 1;
            DateTimeOffset failureWindowExpiresAt = existing is not null && existing.FailureWindowExpiresAt > now
                ? existing.FailureWindowExpiresAt
                : now.Add(denialDuration);
            DateTimeOffset deniedUntil = failures >= threshold ? now.Add(denialDuration) : DateTimeOffset.MinValue;
            attempts[key] = new AttemptState(
                failures,
                deniedUntil,
                failureWindowExpiresAt,
                deniedUntil > failureWindowExpiresAt ? deniedUntil : failureWindowExpiresAt);
            return Task.FromResult(failures);
        }
    }

    public Task ResetAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = attempts.TryRemove(NormalizeKey(identifier), out _);
        return Task.CompletedTask;
    }

    private static string NormalizeKey(string identifier) => identifier.Trim();
    private sealed record AttemptState(
        int Failures,
        DateTimeOffset DeniedUntil,
        DateTimeOffset FailureWindowExpiresAt,
        DateTimeOffset RetentionUntil);
}

public sealed class RedisAuthenticationAttemptTracker(IConnectionMultiplexer redis) : IAuthenticationAttemptTracker
{
    private const string KeyPrefix = "authentication:failed-attempt:";
    private const string DenialSuffix = ":denied";
    private const string RegisterScript = """
        if redis.call('EXISTS', KEYS[2]) == 1 then return tonumber(ARGV[1]) end
        local failures = redis.call('INCR', KEYS[1])
        if failures == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[2]) end
        if failures >= tonumber(ARGV[1]) then redis.call('SET', KEYS[2], '1', 'PX', ARGV[3]) end
        return failures
        """;

    public async Task<bool> IsDeniedAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await redis.GetDatabase().KeyExistsAsync(DenialKey(identifier));
    }

    public async Task<int> RegisterFailureAsync(string identifier, int threshold, TimeSpan denialDuration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        long milliseconds = Math.Max(1, (long)denialDuration.TotalMilliseconds);
        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(RegisterScript, [FailureKey(identifier), DenialKey(identifier)], [threshold, milliseconds, milliseconds]);
        return (int)(long)result;
    }

    public async Task ResetAsync(string identifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await redis.GetDatabase().KeyDeleteAsync([FailureKey(identifier), DenialKey(identifier)]);
    }

    private static string FailureKey(string identifier) => KeyPrefix + NormalizeKey(identifier);
    private static string DenialKey(string identifier) => FailureKey(identifier) + DenialSuffix;
    private static string NormalizeKey(string identifier) => identifier.Trim();
}
