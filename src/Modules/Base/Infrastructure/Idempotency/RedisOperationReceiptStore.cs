using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Modules.Base.Infrastructure.Idempotency;

internal sealed class RedisOperationReceiptStore<T>(IConnectionMultiplexer redis)
    : IOperationReceiptStore<T>
{
    private const string RevisionField = "revision";
    private const string PayloadField = "payload";
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<OperationReceipt<T>?> GetAsync(
        OperationReceiptKey key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue[] values = await _database.HashGetAsync(
            ToRedisKey(key),
            [RevisionField, PayloadField]);
        if (values[0].IsNullOrEmpty || values[1].IsNullOrEmpty)
        {
            return null;
        }

        if (!long.TryParse((string)values[0]!, out long revision))
        {
            throw new InvalidOperationException("The operation receipt revision is invalid.");
        }

        T? value = JsonSerializer.Deserialize<T>((string)values[1]!);
        return value is null
            ? throw new InvalidOperationException("The operation receipt payload is invalid.")
            : new(revision, value);
    }

    public async Task<bool> TryClaimAsync(
        OperationReceiptKey key,
        T value,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        ValidateLifetime(lifetime);
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await _database.ScriptEvaluateAsync(
            """
            if redis.call('EXISTS', KEYS[1]) == 1 then
                return 0
            end
            redis.call('HSET', KEYS[1], 'revision', '0', 'payload', ARGV[1])
            redis.call('EXPIRE', KEYS[1], ARGV[2])
            return 1
            """,
            [ToRedisKey(key)],
            [JsonSerializer.Serialize(value), LifetimeSeconds(lifetime)]);
        return (long)result == 1;
    }

    public async Task<bool> TryUpdateAsync(
        OperationReceiptKey key,
        long expectedRevision,
        T value,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        ValidateLifetime(lifetime);
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await _database.ScriptEvaluateAsync(
            """
            local current = redis.call('HGET', KEYS[1], 'revision')
            if not current or tonumber(current) ~= tonumber(ARGV[1]) then
                return 0
            end
            redis.call('HSET', KEYS[1], 'revision', ARGV[2], 'payload', ARGV[3])
            redis.call('EXPIRE', KEYS[1], ARGV[4])
            return 1
            """,
            [ToRedisKey(key)],
            [
                expectedRevision,
                checked(expectedRevision + 1),
                JsonSerializer.Serialize(value),
                LifetimeSeconds(lifetime)
            ]);
        return (long)result == 1;
    }

    private static RedisKey ToRedisKey(OperationReceiptKey key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key.Scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(key.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key.IdempotencyKey);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(key.IdempotencyKey));
        return $"operation-receipt:{key.Scope}:{key.TenantId}:{Convert.ToHexStringLower(digest)}";
    }

    private static long LifetimeSeconds(TimeSpan lifetime) =>
        Math.Max(1, checked((long)Math.Ceiling(lifetime.TotalSeconds)));

    private static void ValidateLifetime(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }
    }
}
