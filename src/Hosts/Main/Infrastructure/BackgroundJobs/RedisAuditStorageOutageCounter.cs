using StackExchange.Redis;
using Vitarish.Audit.Application;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Keeps the audit-store outage counter in Redis, which persists it (append-only file) independently of
/// the PostgreSQL audit store, so the count survives a restart of Main during the outage.
/// </summary>
internal sealed class RedisAuditStorageOutageCounter(IConnectionMultiplexer redis) : IAuditStorageOutageCounter
{
    private const string Key = "audit:storage-outage";
    private const string StartedField = "started";
    private const string LastFailedField = "last-failed";
    private const string FailedWritesField = "failed-writes";

    private const string RecordScript = """
        redis.call('HSETNX', KEYS[1], ARGV[1], ARGV[4])
        local last = tonumber(redis.call('HGET', KEYS[1], ARGV[2]) or '0')
        if tonumber(ARGV[4]) > last then redis.call('HSET', KEYS[1], ARGV[2], ARGV[4]) end
        return redis.call('HINCRBY', KEYS[1], ARGV[3], 1)
        """;

    private const string ClearScript = """
        if redis.call('HGET', KEYS[1], ARGV[1]) == ARGV[3] and redis.call('HGET', KEYS[1], ARGV[2]) == ARGV[4] then
          return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    public async Task RecordFailedWriteAsync(DateTime failedAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await redis.GetDatabase().ScriptEvaluateAsync(
            RecordScript,
            [Key],
            [StartedField, LastFailedField, FailedWritesField, ToUnixMilliseconds(failedAt)]);
    }

    public async Task<AuditStorageOutage?> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue[] values = await redis.GetDatabase().HashGetAsync(Key, [StartedField, LastFailedField, FailedWritesField]);
        if (values.Any(value => value.IsNull))
            return null;

        return new AuditStorageOutage(
            FromUnixMilliseconds((long)values[0]),
            FromUnixMilliseconds((long)values[1]),
            (long)values[2]);
    }

    public async Task<bool> ClearAsync(AuditStorageOutage summarized, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(
            ClearScript,
            [Key],
            [StartedField, FailedWritesField, ToUnixMilliseconds(summarized.StartedAt), summarized.FailedWrites]);
        return (long)result == 1;
    }

    private static long ToUnixMilliseconds(DateTime value) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    private static DateTime FromUnixMilliseconds(long value) => DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
}
