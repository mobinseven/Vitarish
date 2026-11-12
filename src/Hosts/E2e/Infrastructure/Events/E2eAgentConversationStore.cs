using System.Text;
using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>
/// In-memory agent conversation store for the E2E host, whose in-memory EF provider cannot run the
/// production store's set-based updates and deletes. It mirrors the production semantics (ownership,
/// leases, compatibility keys, revisions, presentation transcript, retention). Production protects
/// history and transcripts at rest, which is a persistence concern, so they are held as plaintext here.
/// </summary>
internal sealed class E2eAgentConversationStore(TimeProvider timeProvider) : IAgentConversationStore
{
    private const int MaximumPlaintextBytes = 1024 * 1024;
    private const string DefaultTitle = "New conversation";
    private static readonly JsonSerializerOptions PresentationJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object gate = new();
    private readonly Dictionary<Guid, Conversation> conversations = [];

    public Task<AgentConversationStoreResult> AcquireAsync(
        Guid? conversationId,
        AgentConversationScope scope,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken) =>
        Task.FromResult(AcquireCore(conversationId, scope, leaseDuration, null, cancellationToken));

    public Task<AgentConversationStoreResult> AcquireAsync(
        Guid? conversationId,
        AgentConversationScope scope,
        TimeSpan leaseDuration,
        string compatibilityKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (conversationId is { } id)
            {
                Conversation? existing = FindOwned(id, scope);
                if (existing is null) return Task.FromResult(new AgentConversationStoreResult(AgentConversationOutcome.NotFound));
                if (existing.ProfileCompatibilityKey is null || !string.Equals(existing.ProfileCompatibilityKey, compatibilityKey, StringComparison.Ordinal))
                    return Task.FromResult(new AgentConversationStoreResult(AgentConversationOutcome.ProfileChanged));
            }
            return Task.FromResult(AcquireCore(conversationId, scope, leaseDuration, compatibilityKey, cancellationToken));
        }
    }

    public Task<AgentConversationOutcome> ReplaceHistoryAsync(
        AgentConversationLease lease,
        AgentConversationScope scope,
        string history,
        string historyFormat,
        CancellationToken cancellationToken) =>
        ReplaceHistoryAsync(lease, scope, history, historyFormat, null, cancellationToken);

    public Task<AgentConversationOutcome> ReplaceHistoryAsync(
        AgentConversationLease lease,
        AgentConversationScope scope,
        string history,
        string historyFormat,
        AgentConversationPresentation? presentation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(historyFormat, RunAgentConversationHandler.HistoryFormat, StringComparison.Ordinal))
            return Task.FromResult(AgentConversationOutcome.InvalidHistory);
        if (Encoding.UTF8.GetByteCount(history) > MaximumPlaintextBytes)
            return Task.FromResult(AgentConversationOutcome.HistoryLimitExceeded);

        lock (gate)
        {
            string? transcript = presentation is null ? null : AppendPresentation(lease.ConversationId, presentation);
            if (transcript is not null && Encoding.UTF8.GetByteCount(transcript) > MaximumPlaintextBytes)
                return Task.FromResult(AgentConversationOutcome.HistoryLimitExceeded);

            Conversation? owned = FindOwned(lease.ConversationId, scope);
            if (owned is null || owned.ActiveLeaseId != lease.LeaseId || owned.Revision != lease.Revision)
                return Task.FromResult(AgentConversationOutcome.Busy);

            owned.History = history;
            if (presentation is not null)
            {
                string prompt = presentation.UserTurn.Trim();
                string assistant = presentation.AssistantResponse.Trim();
                owned.PresentationTranscript = transcript;
                owned.Title = presentation.Title is null ? prompt[..Math.Min(80, prompt.Length)] : presentation.Title.Trim();
                owned.LatestTextPreview = assistant[..Math.Min(240, assistant.Length)];
            }
            owned.HistoryFormat = historyFormat;
            owned.UpdatedAt = timeProvider.GetUtcNow();
            owned.Revision = lease.Revision + 1;
            owned.ActiveLeaseId = null;
            owned.ActiveLeaseExpiresAt = null;
            return Task.FromResult(AgentConversationOutcome.Succeeded);
        }
    }

    public Task<IReadOnlyList<AgentConversationSummary>> ListAsync(
        AgentConversationScope scope,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        limit = Math.Clamp(limit, 1, 100);
        lock (gate)
        {
            IEnumerable<Conversation> query = conversations.Values.Where(x => IsOwnedBy(x, scope));
            if (before is { } cursor) query = query.Where(x => x.UpdatedAt < cursor);
            IReadOnlyList<AgentConversationSummary> result = query
                .OrderByDescending(x => x.UpdatedAt)
                .Take(limit)
                .Select(x => new AgentConversationSummary(x.Id, x.Title ?? DefaultTitle, x.LatestTextPreview, x.CreatedAt, x.UpdatedAt))
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<AgentConversationDisplay?> GetDisplayAsync(Guid conversationId, AgentConversationScope scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            Conversation? entity = FindOwned(conversationId, scope);
            if (entity is null) return Task.FromResult<AgentConversationDisplay?>(null);
            string transcript = entity.PresentationTranscript ?? string.Empty;
            return Task.FromResult<AgentConversationDisplay?>(new(
                entity.Id, entity.Title ?? DefaultTitle, entity.LatestTextPreview, entity.CreatedAt, entity.UpdatedAt, transcript, transcript));
        }
    }

    public Task<bool> RenameAsync(Guid conversationId, AgentConversationScope scope, string title, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120) return Task.FromResult(false);
        lock (gate)
        {
            Conversation? entity = FindOwned(conversationId, scope);
            if (entity is null) return Task.FromResult(false);
            entity.Title = title.Trim();
            entity.UpdatedAt = timeProvider.GetUtcNow();
            return Task.FromResult(true);
        }
    }

    public Task ReleaseAsync(AgentConversationLease lease, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (conversations.TryGetValue(lease.ConversationId, out Conversation? entity))
                Unlease(entity, lease.LeaseId);
        }
        return Task.CompletedTask;
    }

    public Task<AgentConversationOutcome> DeleteAsync(Guid conversationId, AgentConversationScope scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            Conversation? entity = FindOwned(conversationId, scope);
            if (entity is null) return Task.FromResult(AgentConversationOutcome.NotFound);
            if (IsLeased(entity, timeProvider.GetUtcNow())) return Task.FromResult(AgentConversationOutcome.Busy);
            _ = conversations.Remove(conversationId);
            return Task.FromResult(AgentConversationOutcome.Succeeded);
        }
    }

    public Task<AgentConversationOwnerDeletionResult> DeleteForOwnerAsync(AgentConversationOwnerScope owner, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(owner.TenantId) &&
            string.IsNullOrWhiteSpace(owner.CreatingUserId) &&
            string.IsNullOrWhiteSpace(owner.SubjectKind) &&
            string.IsNullOrWhiteSpace(owner.SubjectId))
            throw new ArgumentException("At least one non-empty owner scope filter must be provided.", nameof(owner));

        lock (gate)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            List<Conversation> matches = conversations.Values.Where(x =>
                (string.IsNullOrEmpty(owner.TenantId) || x.TenantId == owner.TenantId) &&
                (string.IsNullOrEmpty(owner.CreatingUserId) || x.CreatingUserId == owner.CreatingUserId) &&
                (string.IsNullOrEmpty(owner.SubjectKind) || x.SubjectKind == owner.SubjectKind) &&
                (string.IsNullOrEmpty(owner.SubjectId) || x.SubjectId == owner.SubjectId)).ToList();
            if (matches.Any(x => x.ActiveLeaseId is not null && x.ActiveLeaseExpiresAt > now))
                return Task.FromResult(new AgentConversationOwnerDeletionResult(AgentConversationOutcome.Busy, 0));
            return Task.FromResult(new AgentConversationOwnerDeletionResult(AgentConversationOutcome.Succeeded, RemoveUnleased(matches, now)));
        }
    }

    public Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            List<Conversation> matches = conversations.Values.Where(x => x.UpdatedAt < cutoff).ToList();
            return Task.FromResult(RemoveUnleased(matches, timeProvider.GetUtcNow()));
        }
    }

    private AgentConversationStoreResult AcquireCore(
        Guid? conversationId,
        AgentConversationScope scope,
        TimeSpan leaseDuration,
        string? compatibilityKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            Guid leaseId = Guid.CreateVersion7();
            DateTimeOffset expiresAt = now.Add(leaseDuration);
            if (conversationId is null)
            {
                Conversation created = new()
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = scope.TenantId,
                    CreatingUserId = scope.CreatingUserId,
                    AgentKey = scope.AgentKey,
                    SubjectKind = scope.SubjectKind,
                    SubjectId = scope.SubjectId,
                    HistoryFormat = RunAgentConversationHandler.HistoryFormat,
                    ProfileCompatibilityKey = compatibilityKey,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Revision = 0,
                    ActiveLeaseId = leaseId,
                    ActiveLeaseExpiresAt = expiresAt
                };
                conversations[created.Id] = created;
                return new(AgentConversationOutcome.Succeeded, new(created.Id, leaseId, 0, null));
            }

            Conversation? entity = FindOwned(conversationId.Value, scope);
            if (entity is null) return new(AgentConversationOutcome.NotFound);
            if (IsLeased(entity, now)) return new(AgentConversationOutcome.Busy);

            entity.ActiveLeaseId = leaseId;
            entity.ActiveLeaseExpiresAt = expiresAt;
            if (!string.Equals(entity.HistoryFormat, RunAgentConversationHandler.HistoryFormat, StringComparison.Ordinal))
            {
                Unlease(entity, leaseId);
                return new(AgentConversationOutcome.InvalidHistory);
            }
            if (entity.History is not null && Encoding.UTF8.GetByteCount(entity.History) > MaximumPlaintextBytes)
            {
                Unlease(entity, leaseId);
                return new(AgentConversationOutcome.HistoryLimitExceeded);
            }
            return new(AgentConversationOutcome.Succeeded, new(entity.Id, leaseId, entity.Revision, entity.History));
        }
    }

    // Called under the gate. Keeps the display transcript additive, tolerating the legacy single-turn shape.
    private string AppendPresentation(Guid conversationId, AgentConversationPresentation presentation)
    {
        List<PresentationTurn> turns = [];
        if (conversations.TryGetValue(conversationId, out Conversation? entity) && entity.PresentationTranscript is { Length: > 0 } json)
        {
            try
            {
                try
                {
                    turns = JsonSerializer.Deserialize<List<PresentationTurn>>(json, PresentationJsonOptions) ?? [];
                }
                catch (JsonException)
                {
                    PresentationTurn? legacy = JsonSerializer.Deserialize<PresentationTurn>(json, PresentationJsonOptions);
                    if (legacy is not null) turns.Add(legacy);
                }
            }
            catch (JsonException) { }
        }
        turns.Add(new(presentation.UserTurn, presentation.AssistantResponse, presentation.PreparedActionIds, presentation.Parts));
        return JsonSerializer.Serialize(turns, PresentationJsonOptions);
    }

    private int RemoveUnleased(List<Conversation> matches, DateTimeOffset now)
    {
        int deleted = 0;
        foreach (Conversation match in matches)
        {
            if (IsLeased(match, now)) continue;
            if (conversations.Remove(match.Id)) deleted++;
        }
        return deleted;
    }

    private Conversation? FindOwned(Guid id, AgentConversationScope scope) =>
        conversations.TryGetValue(id, out Conversation? entity) && IsOwnedBy(entity, scope) ? entity : null;

    private static bool IsOwnedBy(Conversation entity, AgentConversationScope scope) =>
        entity.TenantId == scope.TenantId &&
        entity.CreatingUserId == scope.CreatingUserId &&
        entity.AgentKey == scope.AgentKey &&
        entity.SubjectKind == scope.SubjectKind &&
        entity.SubjectId == scope.SubjectId;

    private static bool IsLeased(Conversation entity, DateTimeOffset now) =>
        entity.ActiveLeaseId is not null && !(entity.ActiveLeaseExpiresAt <= now);

    private static void Unlease(Conversation entity, Guid leaseId)
    {
        if (entity.ActiveLeaseId != leaseId) return;
        entity.ActiveLeaseId = null;
        entity.ActiveLeaseExpiresAt = null;
    }

    private sealed record PresentationTurn(
        string User,
        string Assistant,
        IReadOnlyCollection<Guid> PreparedActionIds,
        JsonElement? Parts = null);

    private sealed class Conversation
    {
        public Guid Id { get; init; }
        public string TenantId { get; init; } = string.Empty;
        public string CreatingUserId { get; init; } = string.Empty;
        public string AgentKey { get; init; } = string.Empty;
        public string SubjectKind { get; init; } = string.Empty;
        public string SubjectId { get; init; } = string.Empty;
        public string? ProfileCompatibilityKey { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public string HistoryFormat { get; set; } = string.Empty;
        public string? History { get; set; }
        public string? PresentationTranscript { get; set; }
        public string? Title { get; set; }
        public string? LatestTextPreview { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public long Revision { get; set; }
        public Guid? ActiveLeaseId { get; set; }
        public DateTimeOffset? ActiveLeaseExpiresAt { get; set; }
    }
}
