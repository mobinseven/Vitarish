using StackExchange.Redis;
using Vitarish.Audit.Application;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Keeps the audit-store outage counter in Redis, which persists it (append-only file) independently of
/// the PostgreSQL audit store, so the count survives a restart of Main during the outage. A refused write
/// waits on Redis for at most <see cref="RecordTimeout"/>, so an unreachable Redis does not stall the request.
/// </summary>
internal sealed class RedisAuditStorageOutageCounter(IConnectionMultiplexer redis) : IAuditStorageOutageCounter
{
    private const string Key = "audit:storage-outage";
    private const string PendingSummaryKey = Key + ":pending-summary";
    private const string StartedField = "started";
    private const string LastFailedField = "last-failed";
    private const string FailedWritesField = "failed-writes";
    internal static readonly TimeSpan RecordTimeout = TimeSpan.FromMilliseconds(250);

    private const string RecordScript = """
        local now = tonumber(ARGV[4])
        local started = redis.call('HGET', KEYS[1], ARGV[1])
        if not started or now < tonumber(started) then redis.call('HSET', KEYS[1], ARGV[1], ARGV[4]) end
        local last = tonumber(redis.call('HGET', KEYS[1], ARGV[2]) or '0')
        if now > last then redis.call('HSET', KEYS[1], ARGV[2], ARGV[4]) end
        return redis.call('HINCRBY', KEYS[1], ARGV[3], 1)
        """;

    private const string ReserveScript = """
        if redis.call('EXISTS', KEYS[2]) == 0 and redis.call('EXISTS', KEYS[1]) == 1 then
          redis.call('RENAME', KEYS[1], KEYS[2])
        end
        return redis.call('HMGET', KEYS[2], ARGV[1], ARGV[2], ARGV[3])
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
                [StartedField, LastFailedField, FailedWritesField, ToUnixMilliseconds(failedAt)])
            .WaitAsync(RecordTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AuditStorageOutage?> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue[] values = await redis.GetDatabase()
            .HashGetAsync(PendingSummaryKey, [StartedField, LastFailedField, FailedWritesField])
            .WaitAsync(RecordTimeout, cancellationToken).ConfigureAwait(false);
        if (values.Any(value => value.IsNull))
            values = await redis.GetDatabase()
                .HashGetAsync(Key, [StartedField, LastFailedField, FailedWritesField])
                .WaitAsync(RecordTimeout, cancellationToken).ConfigureAwait(false);
        return FromValues(values);
    }

    public async Task<AuditStorageOutage?> ReserveSummaryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(
            ReserveScript, [Key, PendingSummaryKey], [StartedField, LastFailedField, FailedWritesField])
            .WaitAsync(RecordTimeout, cancellationToken).ConfigureAwait(false);
        return FromValues((RedisValue[])result!);
    }

    public async Task<bool> ClearAsync(AuditStorageOutage summarized, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await redis.GetDatabase().ScriptEvaluateAsync(
            ClearScript,
            [PendingSummaryKey],
            [StartedField, FailedWritesField, ToUnixMilliseconds(summarized.StartedAt), summarized.FailedWrites])
            .WaitAsync(RecordTimeout, cancellationToken).ConfigureAwait(false);
        return (long)result == 1;
    }

    private static AuditStorageOutage? FromValues(RedisValue[] values) =>
        values.Any(value => value.IsNull) ? null : new AuditStorageOutage(
            FromUnixMilliseconds((long)values[0]), FromUnixMilliseconds((long)values[1]), (long)values[2]);

    private static long ToUnixMilliseconds(DateTime value) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    private static DateTime FromUnixMilliseconds(long value) => DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
}
