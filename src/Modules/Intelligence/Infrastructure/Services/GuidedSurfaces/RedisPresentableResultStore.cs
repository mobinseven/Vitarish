using System.Globalization;
using System.Text.Json;
using StackExchange.Redis;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.Intelligence.Infrastructure.Services.GuidedSurfaces;

/// <summary>
/// Keeps a run's presentable results in one Redis hash under an Intelligence-owned key namespace. Handles are
/// numbered per run (<c>r1</c>, <c>r2</c>, ...); the hash expires a while after the run's last read, long enough
/// for the run to finish and for its reply to be built.
/// </summary>
public sealed class RedisPresentableResultStore(IConnectionMultiplexer redis) : IPresentableResultStore
{
    public const string KeyPrefix = "intelligence:presentable:";
    private const string CounterField = "#next";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<string> SaveAsync(string executionId, PresentableResult result, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();
        RedisKey key = Key(executionId);
        long number = await _database.HashIncrementAsync(key, CounterField);
        string handle = "r" + number.ToString(CultureInfo.InvariantCulture);
        await _database.HashSetAsync(key, handle, JsonSerializer.Serialize(result, PresentableJsonContext.Default.PresentableResult));
        await _database.KeyExpireAsync(key, Lifetime);
        return handle;
    }

    public async Task<PresentableResult?> GetAsync(string executionId, string handle, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(handle) || handle == CounterField) return null;
        RedisValue value = await _database.HashGetAsync(Key(executionId), handle);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize(value.ToString(), PresentableJsonContext.Default.PresentableResult);
    }

    private static RedisKey Key(string executionId) => KeyPrefix + executionId;
}
