using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using Vitarish.Clinic.Application.Assistant.Surgeries;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

public sealed class RedisSurgeryAssistantProposalStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : ISurgeryAssistantProposalStore
{
    // Kept a day past expiry, like the actions themselves, so an expired action still explains itself.
    private static readonly TimeSpan Retention = TimeSpan.FromDays(1);
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task SaveAsync(SurgeryAssistantProposal proposal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();
        string payload = JsonSerializer.Serialize(proposal, SurgeryAssistantProposalJsonSerializerContext.Default.SurgeryAssistantProposal);
        // Proposal ids are new for every preparation; an existing key means a collision, never a retry.
        bool saved = await _database.StringSetAsync(
            Key(proposal.Id), payload, proposal.ExpiresAt - timeProvider.GetUtcNow() + Retention, When.NotExists);
        if (!saved) throw new InvalidOperationException("A surgery proposal with the same id already exists.");
    }

    public async Task<SurgeryAssistantProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue value = await _database.StringGetAsync(Key(proposalId));
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize((string)value!, SurgeryAssistantProposalJsonSerializerContext.Default.SurgeryAssistantProposal);
    }

    private static RedisKey Key(Guid proposalId) => $"clinic:surgery-assistant-proposal:{proposalId:D}";
}

[JsonSerializable(typeof(SurgeryAssistantProposal))]
internal sealed partial class SurgeryAssistantProposalJsonSerializerContext : JsonSerializerContext;
