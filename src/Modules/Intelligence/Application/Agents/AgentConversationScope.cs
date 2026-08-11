using System.Text.Json;

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

public sealed record AgentConversationOwnerDeletionResult(
    AgentConversationOutcome Outcome,
    int DeletedCount);

public sealed record AgentConversationPresentation(
    string UserTurn,
    string AssistantResponse,
    IReadOnlyCollection<Guid> PreparedActionIds,
    string? Title = null,
    JsonElement? Parts = null);

public sealed record AgentConversationSummary(
    Guid Id,
    string Title,
    string? LatestTextPreview,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AgentConversationDisplay(
    Guid Id,
    string Title,
    string? LatestTextPreview,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Transcript,
    string? PresentationTranscript = null);

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

    Task<AgentConversationOutcome> ReplaceHistoryAsync(
        AgentConversationLease lease,
        AgentConversationScope scope,
        string history,
        string historyFormat,
        AgentConversationPresentation? presentation,
        CancellationToken cancellationToken) => ReplaceHistoryAsync(lease, scope, history, historyFormat, cancellationToken);

    Task<IReadOnlyList<AgentConversationSummary>> ListAsync(
        AgentConversationScope scope,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken);

    Task<AgentConversationDisplay?> GetDisplayAsync(
        Guid conversationId,
        AgentConversationScope scope,
        CancellationToken cancellationToken);

    Task<bool> RenameAsync(Guid conversationId, AgentConversationScope scope, string title, CancellationToken cancellationToken);

    Task ReleaseAsync(AgentConversationLease lease, CancellationToken cancellationToken);

    Task<AgentConversationOutcome> DeleteAsync(
        Guid conversationId,
        AgentConversationScope scope,
        CancellationToken cancellationToken);

    Task<AgentConversationOwnerDeletionResult> DeleteForOwnerAsync(
        AgentConversationOwnerScope owner,
        CancellationToken cancellationToken);

    Task<int> DeleteExpiredAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);
}
