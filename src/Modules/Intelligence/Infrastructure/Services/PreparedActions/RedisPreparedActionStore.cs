using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Vitarish.Intelligence.Infrastructure.Services.PreparedActions;

/// <summary>
/// Keeps prepared actions in Redis under an Intelligence-owned key namespace. A record lives until one day after
/// it expires. The stored string is the version: a replacement succeeds only while the key still holds exactly
/// the string that was read, so concurrent transitions never overwrite each other.
/// </summary>
public sealed class RedisPreparedActionStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : IPreparedActionStore
{
    public const string KeyPrefix = "intelligence:prepared-action:";
    private static readonly TimeSpan RetentionAfterExpiry = TimeSpan.FromDays(1);

    private const string ReplaceScript =
        "local value=redis.call('GET',KEYS[1]); if value ~= ARGV[1] then return 0 end " +
        "redis.call('SET',KEYS[1],ARGV[2],'KEEPTTL'); return 1";

    private readonly IDatabase _database = redis.GetDatabase();

    public async Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        string payload = Serialize(action);
        RedisKey key = Key(action.ActionId);
        TimeSpan lifetime = action.ExpiresAt - timeProvider.GetUtcNow() + RetentionAfterExpiry;
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentException("The prepared action is already past its retention.", nameof(action));
        if (await _database.StringSetAsync(key, payload, lifetime, When.NotExists))
            return;

        RedisValue existing = await _database.StringGetAsync(key);
        if (existing.HasValue && string.Equals(existing.ToString(), payload, StringComparison.Ordinal))
            return;
        throw new InvalidOperationException("A prepared action with the same id already exists with different data.");
    }

    public async Task<StoredPreparedAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue value = await _database.StringGetAsync(Key(actionId));
        if (value.IsNullOrEmpty) return null;
        string version = value.ToString();
        PreparedAction action = JsonSerializer.Deserialize(version, PreparedActionJsonSerializerContext.Default.PreparedAction)
            ?? throw new InvalidOperationException("A stored prepared action could not be read.");
        return new(action, version);
    }

    public async Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(next);
        if (current.Action.ActionId != next.ActionId)
            throw new ArgumentException("A replacement must keep the action id.", nameof(next));
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await _database.ScriptEvaluateAsync(
            ReplaceScript, [Key(next.ActionId)], [current.Version, Serialize(next)]);
        return (long)result == 1;
    }

    private static string Serialize(PreparedAction action) =>
        JsonSerializer.Serialize(action, PreparedActionJsonSerializerContext.Default.PreparedAction);

    private static RedisKey Key(Guid actionId) => KeyPrefix + actionId.ToString("D");
}

[JsonSerializable(typeof(PreparedAction))]
internal sealed partial class PreparedActionJsonSerializerContext : JsonSerializerContext;
