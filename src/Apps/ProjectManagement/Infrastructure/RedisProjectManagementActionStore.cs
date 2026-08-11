using System.Text.Json;
using StackExchange.Redis;
using Vitarish.ProjectManagement.Application;

namespace Vitarish.ProjectManagement.Infrastructure;

public sealed class RedisProjectManagementActionStore(IConnectionMultiplexer redis) : IProjectManagementActionStore
{
    private readonly IDatabase _database = redis.GetDatabase();
    public async Task SaveAsync(ProjectManagementAction action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool saved = await _database.StringSetAsync(Key(action.ActionId), JsonSerializer.Serialize(action), action.ExpiresAt - DateTimeOffset.UtcNow + TimeSpan.FromDays(1), When.NotExists);
        if (!saved)
        {
            throw new InvalidOperationException("A project-management action with the same id already exists.");
        }
    }
    public async Task<ProjectManagementAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
    { cancellationToken.ThrowIfCancellationRequested(); RedisValue value = await _database.StringGetAsync(Key(actionId)); return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<ProjectManagementAction>((string)value!); }
    public Task<bool> TryBeginConfirmationAsync(Guid actionId, CancellationToken cancellationToken) => MutateAsync(actionId, "if a.Status ~= 0 and not (a.Status == 5 and a.Failure and a.Failure.Retryable == true) then return 0 end a.Status=1; a.Failure=nil; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1", [], cancellationToken);
    public Task<bool> TryRejectAsync(Guid actionId, string? reason, CancellationToken cancellationToken) => MutateAsync(actionId, "if a.Status ~= 0 then return 0 end a.Status=3; a.RejectionReason=ARGV[1]; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1", [reason ?? string.Empty], cancellationToken);
    public Task<bool> CompleteAsync(Guid actionId, string payload, CancellationToken cancellationToken) => MutateAsync(actionId, "if a.Status ~= 1 then return 0 end a.Status=2; a.CompletionPayload=ARGV[1]; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1", [payload], cancellationToken);
    public Task<bool> FailAsync(Guid actionId, ProjectManagementActionFailure failure, CancellationToken cancellationToken) => MutateAsync(actionId, "if a.Status ~= 1 and a.Status ~= 0 then return 0 end a.Status=5; a.Failure={Code=ARGV[1],Message=ARGV[2],Retryable=(ARGV[3]=='1')}; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1", [failure.Code, failure.Message, failure.Retryable ? "1" : "0"], cancellationToken);
    public Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken) => MutateAsync(actionId, "if a.Status ~= 0 and a.Status ~= 5 then return 0 end a.Status=4; redis.call('SET',KEYS[1],cjson.encode(a),'KEEPTTL'); return 1", [], cancellationToken);
    private async Task<bool> MutateAsync(Guid id, string mutation, RedisValue[] arguments, CancellationToken cancellationToken)
    { cancellationToken.ThrowIfCancellationRequested(); RedisResult result = await _database.ScriptEvaluateAsync("local value=redis.call('GET',KEYS[1]); if not value then return 0 end local a=cjson.decode(value); " + mutation, [Key(id)], arguments); return (long)result == 1; }
    private static RedisKey Key(Guid id) => $"project-management:action:{id:D}";
}
