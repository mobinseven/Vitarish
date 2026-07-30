namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentConversationScope(
    string TenantId,
    string CreatingUserId,
    string AgentKey,
    string SubjectKind,
    string SubjectId);

public sealed record AgentConversationOwnerScope(
    string? TenantId = null,
    string? CreatingUserId = null,
    string? SubjectKind = null,
    string? SubjectId = null);

public enum AgentConversationOutcome
{
    Succeeded,
    NotFound,
    Busy,
    HistoryLimitExceeded,
    ProviderFailure,
    InvalidHistory
}

public sealed record AgentConversationLease(
    Guid ConversationId,
    Guid LeaseId,
    long Revision,
    string? History);

public sealed record AgentConversationStoreResult(
    AgentConversationOutcome Outcome,
    AgentConversationLease? Lease = null);

public interface IAgentConversationStore
{
    Task<AgentConversationStoreResult> AcquireAsync(
        Guid? conversationId,
        AgentConversationScope scope,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<AgentConversationOutcome> ReplaceHistoryAsync(
        AgentConversationLease lease,
        AgentConversationScope scope,
        string history,
        string historyFormat,
        CancellationToken cancellationToken);

    Task ReleaseAsync(AgentConversationLease lease, CancellationToken cancellationToken);

    Task<AgentConversationOutcome> DeleteAsync(
        Guid conversationId,
        AgentConversationScope scope,
        CancellationToken cancellationToken);

    Task<int> DeleteForOwnerAsync(
        AgentConversationOwnerScope owner,
        CancellationToken cancellationToken);

    Task<int> DeleteExpiredAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);
}
