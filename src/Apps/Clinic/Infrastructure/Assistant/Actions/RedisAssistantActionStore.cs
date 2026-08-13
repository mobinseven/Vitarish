using System.Text.Json;
using StackExchange.Redis;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

public sealed class RedisAssistantActionStore(IConnectionMultiplexer redis) : IAssistantActionStore
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task SaveAsync(AssistantAction action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        string payload = JsonSerializer.Serialize(action, AssistantActionJsonSerializerContext.Default.AssistantAction);
        RedisKey key = Key(action.ActionId);
        bool saved = await _database.StringSetAsync(
            key, payload, action.ExpiresAt - DateTimeOffset.UtcNow + TimeSpan.FromDays(1), When.NotExists);
        if (saved) return;

        RedisValue existing = await _database.StringGetAsync(key);
        if (existing.HasValue && string.Equals(existing.ToString(), payload, StringComparison.Ordinal)) return;
        throw new InvalidOperationException("A clinic-assistant action with the same id already exists with different data.");
    }

    public async Task<AssistantAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue value = await _database.StringGetAsync(Key(actionId));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize((string)value!, AssistantActionJsonSerializerContext.Default.AssistantAction);
    }

    public Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken) =>
        MutateAsync(actionId,
            "if a.Status ~= 0 and not (ARGV[1] == '1' and a.Status == 5 and a.Failure and a.Failure.Retryable == true) then return 0 end " +
            "a.Status=1; a.Failure=nil; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1",
            [retryFailed ? "1" : "0"], cancellationToken);

    public Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken) =>
        MutateAsync(actionId,
            "if a.Status ~= 0 then return 0 end a.Status=3; a.RejectedAt=ARGV[1]; " +
            "if ARGV[2] ~= '' then a.RejectionReason=ARGV[2] end redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1",
            [rejectedAt.ToString("O"), reason ?? string.Empty], cancellationToken);

    public Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken) =>
        MutateAsync(actionId,
            "if a.Status ~= 1 then return 0 end a.Status=2; a.ConfirmedAt=ARGV[1]; a.ConfirmedByUserId=ARGV[2]; " +
            "a.CompletionPayload=ARGV[3]; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1",
            [confirmedAt.ToString("O"), confirmedByUserId, completionPayload], cancellationToken);

    public Task<bool> FailAsync(Guid actionId, AssistantActionFailure failure, CancellationToken cancellationToken) =>
        MutateAsync(actionId,
            "if a.Status ~= 1 and a.Status ~= 0 then return 0 end a.Status=5; " +
            "a.Failure={Code=ARGV[1],Message=ARGV[2],Retryable=(ARGV[3]=='1')}; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1",
            [failure.Code, failure.Message, failure.Retryable ? "1" : "0"], cancellationToken);

    public Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken) =>
        MutateAsync(actionId,
            "if a.Status ~= 0 and a.Status ~= 5 then return 0 end a.Status=4; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1",
            [], cancellationToken);

    private async Task<bool> MutateAsync(Guid actionId, string mutation, RedisValue[] arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await _database.ScriptEvaluateAsync(
            "local value=redis.call('GET',KEYS[1]); if not value then return 0 end local a=cjson.decode(value); " + mutation,
            [Key(actionId)], arguments);
        return (long)result == 1;
    }

    private static RedisKey Key(Guid actionId) => $"clinic:clinic-assistant-action:{actionId:D}";
}
