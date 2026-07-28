using System.Text.Json;
using StackExchange.Redis;
using Vitarish.Clinic.Application.ClinicalEvents;

namespace Vitarish.Clinic.Infrastructure.ClinicalEvents;

public sealed class RedisClinicalEventProposalStore(IConnectionMultiplexer redis)
    : IClinicalEventProposalStore
{
    private const string PayloadField = "payload";
    private const string StatusField = "status";
    private const string EventIdField = "eventId";
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task SaveAsync(
        ClinicalEventProposal proposal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await _database.ScriptEvaluateAsync(
            """
            if redis.call('EXISTS', KEYS[1]) == 1 then
                return 0
            end
            redis.call('HSET', KEYS[1], 'payload', ARGV[1], 'status', ARGV[2])
            redis.call('PEXPIREAT', KEYS[1], ARGV[3])
            return 1
            """,
            [Key(proposal.ActionId)],
            [
                JsonSerializer.Serialize(proposal),
                (int)ClinicalEventProposalStatus.Prepared,
                proposal.ExpiresAt.ToUnixTimeMilliseconds()
            ]);
        if ((long)result != 1)
            throw new InvalidOperationException("A clinical event proposal with the same action id already exists.");
    }

    public async Task<ClinicalEventProposal?> GetAsync(
        Guid actionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue[] values = await _database.HashGetAsync(
            Key(actionId),
            [PayloadField, StatusField, EventIdField]);
        if (values[0].IsNullOrEmpty) return null;
        if (!int.TryParse((string?)values[1], out int statusValue) ||
            !Enum.IsDefined(typeof(ClinicalEventProposalStatus), statusValue))
            throw new InvalidOperationException("The stored clinical event proposal status is invalid.");

        ClinicalEventProposal proposal = JsonSerializer.Deserialize<ClinicalEventProposal>((string)values[0]!)
            ?? throw new InvalidOperationException("The stored clinical event proposal is invalid.");
        Guid? eventId = values[2].IsNullOrEmpty
            ? null
            : Guid.TryParse((string?)values[2], out Guid parsed)
                ? parsed
                : throw new InvalidOperationException("The stored clinical event id is invalid.");
        return proposal with
        {
            Status = (ClinicalEventProposalStatus)statusValue,
            CreatedEventId = eventId
        };
    }

    public Task<bool> TryBeginConfirmationAsync(
        Guid actionId,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            actionId,
            ClinicalEventProposalStatus.Prepared,
            ClinicalEventProposalStatus.Confirming,
            cancellationToken);

    public Task<bool> TryReleaseConfirmationAsync(
        Guid actionId,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            actionId,
            ClinicalEventProposalStatus.Confirming,
            ClinicalEventProposalStatus.Prepared,
            cancellationToken);

    public async Task<bool> CompleteAsync(
        Guid actionId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await _database.ScriptEvaluateAsync(
            """
            local current = redis.call('HGET', KEYS[1], 'status')
            if not current or tonumber(current) ~= tonumber(ARGV[1]) then
                return 0
            end
            redis.call('HSET', KEYS[1], 'status', ARGV[2], 'eventId', ARGV[3])
            return 1
            """,
            [Key(actionId)],
            [
                (int)ClinicalEventProposalStatus.Confirming,
                (int)ClinicalEventProposalStatus.Completed,
                eventId.ToString("D")
            ]);
        return (long)result == 1;
    }

    private async Task<bool> TransitionAsync(
        Guid actionId,
        ClinicalEventProposalStatus expected,
        ClinicalEventProposalStatus next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisResult result = await _database.ScriptEvaluateAsync(
            """
            local current = redis.call('HGET', KEYS[1], 'status')
            if not current or tonumber(current) ~= tonumber(ARGV[1]) then
                return 0
            end
            redis.call('HSET', KEYS[1], 'status', ARGV[2])
            return 1
            """,
            [Key(actionId)],
            [(int)expected, (int)next]);
        return (long)result == 1;
    }

    private static RedisKey Key(Guid actionId) =>
        $"clinic:clinical-event-proposal:{actionId:D}";
}
