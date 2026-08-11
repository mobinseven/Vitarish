using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Infrastructure.Data;

public sealed class EfAgentConversationStore(
    IntelligenceDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider) : IAgentConversationStore
{
    private static readonly JsonSerializerOptions PresentationJsonOptions = new(JsonSerializerDefaults.Web);
    public const int MaximumPlaintextBytes = 1024 * 1024;
    public const string ProtectorPurpose = "Vitarish.Intelligence.AgentConversationHistory.v1";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public Task<AgentConversationOutcome> ReplaceHistoryAsync(AgentConversationLease lease, AgentConversationScope scope, string history, string historyFormat, CancellationToken cancellationToken) =>
        ReplaceHistoryAsync(lease, scope, history, historyFormat, null, cancellationToken);

    public async Task<AgentConversationStoreResult> AcquireAsync(
        Guid? conversationId,
        AgentConversationScope scope,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid leaseId = Guid.CreateVersion7();
        DateTimeOffset expiresAt = now.Add(leaseDuration);
        if (conversationId is null)
        {
            AgentConversationEntity created = new()
            {
                Id = Guid.CreateVersion7(),
                TenantId = scope.TenantId,
                CreatingUserId = scope.CreatingUserId,
                AgentKey = scope.AgentKey,
                SubjectKind = scope.SubjectKind,
                SubjectId = scope.SubjectId,
                HistoryFormat = RunAgentConversationHandler.HistoryFormat,
                CreatedAt = now,
                UpdatedAt = now,
                Revision = 0,
                ActiveLeaseId = leaseId,
                ActiveLeaseExpiresAt = expiresAt
            };
            dbContext.AgentConversations.Add(created);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(AgentConversationOutcome.Succeeded, new(created.Id, leaseId, 0, null));
        }

        int acquired = await Owned(scope)
            .Where(conversation => conversation.Id == conversationId &&
                (conversation.ActiveLeaseId == null || conversation.ActiveLeaseExpiresAt <= now))
            .ExecuteUpdateAsync(update => update
                .SetProperty(conversation => conversation.ActiveLeaseId, leaseId)
                .SetProperty(conversation => conversation.ActiveLeaseExpiresAt, expiresAt), cancellationToken);
        if (acquired == 0)
        {
            bool owned = await Owned(scope).AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
            return new(owned ? AgentConversationOutcome.Busy : AgentConversationOutcome.NotFound);
        }

        AgentConversationEntity entity = await Owned(scope)
            .AsNoTracking()
            .SingleAsync(conversation => conversation.Id == conversationId, cancellationToken);
        if (!string.Equals(entity.HistoryFormat, RunAgentConversationHandler.HistoryFormat, StringComparison.Ordinal))
        {
            await ReleaseAsync(new(entity.Id, leaseId, entity.Revision, null), CancellationToken.None);
            return new(AgentConversationOutcome.InvalidHistory);
        }
        string? history;
        try
        {
            history = entity.ProtectedHistory is null ? null : _protector.Unprotect(entity.ProtectedHistory);
        }
        catch (CryptographicException)
        {
            await ReleaseAsync(new(entity.Id, leaseId, entity.Revision, null), CancellationToken.None);
            return new(AgentConversationOutcome.InvalidHistory);
        }
        if (history is not null && Encoding.UTF8.GetByteCount(history) > MaximumPlaintextBytes)
        {
            await ReleaseAsync(new(entity.Id, leaseId, entity.Revision, null), CancellationToken.None);
            return new(AgentConversationOutcome.HistoryLimitExceeded);
        }
        return new(AgentConversationOutcome.Succeeded, new(entity.Id, leaseId, entity.Revision, history));
    }

    public async Task<AgentConversationOutcome> ReplaceHistoryAsync(
        AgentConversationLease lease,
        AgentConversationScope scope,
        string history,
        string historyFormat,
        AgentConversationPresentation? presentation,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(historyFormat, RunAgentConversationHandler.HistoryFormat, StringComparison.Ordinal))
            return AgentConversationOutcome.InvalidHistory;
        if (Encoding.UTF8.GetByteCount(history) > MaximumPlaintextBytes)
            return AgentConversationOutcome.HistoryLimitExceeded;

        string protectedHistory = _protector.Protect(history);
        string? protectedPresentation = presentation is null
            ? null
            : _protector.Protect(AppendPresentation(lease.ConversationId, presentation));
        if (protectedPresentation is not null && Encoding.UTF8.GetByteCount(_protector.Unprotect(protectedPresentation)) > MaximumPlaintextBytes)
            return AgentConversationOutcome.HistoryLimitExceeded;
        DateTimeOffset now = timeProvider.GetUtcNow();
        IQueryable<AgentConversationEntity> owned = Owned(scope).Where(conversation => conversation.Id == lease.ConversationId && conversation.ActiveLeaseId == lease.LeaseId && conversation.Revision == lease.Revision);
        int updated;
        if (presentation is null)
        {
            updated = await owned.ExecuteUpdateAsync(update => update
                .SetProperty(conversation => conversation.ProtectedHistory, protectedHistory)
                .SetProperty(conversation => conversation.HistoryFormat, historyFormat)
                .SetProperty(conversation => conversation.UpdatedAt, now)
                .SetProperty(conversation => conversation.Revision, lease.Revision + 1)
                .SetProperty(conversation => conversation.ActiveLeaseId, (Guid?)null)
                .SetProperty(conversation => conversation.ActiveLeaseExpiresAt, (DateTimeOffset?)null), cancellationToken);
        }
        else
        {
            string prompt = presentation.UserTurn.Trim();
            string promptTitle = prompt[..Math.Min(80, prompt.Length)];
            string assistantPreview = presentation.AssistantResponse.Trim()[..Math.Min(240, presentation.AssistantResponse.Trim().Length)];
            updated = await owned.ExecuteUpdateAsync(update => update
                .SetProperty(conversation => conversation.ProtectedHistory, protectedHistory)
                .SetProperty(conversation => conversation.ProtectedPresentationTranscript, protectedPresentation)
                .SetProperty(conversation => conversation.Title, presentation.Title is null ? promptTitle : presentation.Title.Trim())
                .SetProperty(conversation => conversation.LatestTextPreview, assistantPreview)
                .SetProperty(conversation => conversation.HistoryFormat, historyFormat)
                .SetProperty(conversation => conversation.UpdatedAt, now)
                .SetProperty(conversation => conversation.Revision, lease.Revision + 1)
                .SetProperty(conversation => conversation.ActiveLeaseId, (Guid?)null)
                .SetProperty(conversation => conversation.ActiveLeaseExpiresAt, (DateTimeOffset?)null), cancellationToken);
        }
        return updated == 1 ? AgentConversationOutcome.Succeeded : AgentConversationOutcome.Busy;
    }

    private string AppendPresentation(Guid conversationId, AgentConversationPresentation presentation)
    {
        // The lease serializes turns for a conversation. Reading the previous protected
        // presentation before the compare-and-set update keeps the display transcript
        // additive while provider history remains private and separately protected.
        AgentConversationEntity? entity = dbContext.AgentConversations
            .AsNoTracking()
            .SingleOrDefault(conversation => conversation.Id == conversationId);
        List<PresentationTurn> turns = [];
        if (entity?.ProtectedPresentationTranscript is { Length: > 0 } previous)
        {
            try
            {
                string json = _protector.Unprotect(previous);
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
            catch (CryptographicException) { }
        }
        turns.Add(new(presentation.UserTurn, presentation.AssistantResponse, presentation.PreparedActionIds, presentation.Parts));
        return JsonSerializer.Serialize(turns, PresentationJsonOptions);
    }

    private sealed record PresentationTurn(
        string User,
        string Assistant,
        IReadOnlyCollection<Guid> PreparedActionIds,
        JsonElement? Parts = null);

    public async Task<IReadOnlyList<AgentConversationSummary>> ListAsync(AgentConversationScope scope, DateTimeOffset? before, int limit, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 100);
        IQueryable<AgentConversationEntity> query = Owned(scope).AsNoTracking().OrderByDescending(x => x.UpdatedAt);
        if (before is { } cursor) query = query.Where(x => x.UpdatedAt < cursor).OrderByDescending(x => x.UpdatedAt);
        return await query.Take(limit).Select(x => new AgentConversationSummary(x.Id, x.Title ?? "New conversation", x.LatestTextPreview, x.CreatedAt, x.UpdatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<AgentConversationDisplay?> GetDisplayAsync(Guid conversationId, AgentConversationScope scope, CancellationToken cancellationToken)
    {
        AgentConversationEntity? entity = await Owned(scope).AsNoTracking().SingleOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
        if (entity is null) return null;
        string transcript = string.Empty;
        if (entity.ProtectedPresentationTranscript is not null)
        {
            try { transcript = _protector.Unprotect(entity.ProtectedPresentationTranscript); }
            catch (CryptographicException) { return null; }
        }
        return new(entity.Id, entity.Title ?? "New conversation", entity.LatestTextPreview, entity.CreatedAt, entity.UpdatedAt, transcript, transcript);
    }

    public async Task<bool> RenameAsync(Guid conversationId, AgentConversationScope scope, string title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120) return false;
        int updated = await Owned(scope).Where(x => x.Id == conversationId).ExecuteUpdateAsync(set => set.SetProperty(x => x.Title, title.Trim()).SetProperty(x => x.UpdatedAt, timeProvider.GetUtcNow()), cancellationToken);
        return updated == 1;
    }

    public async Task ReleaseAsync(AgentConversationLease lease, CancellationToken cancellationToken)
    {
        _ = await dbContext.AgentConversations
            .Where(conversation => conversation.Id == lease.ConversationId &&
                conversation.ActiveLeaseId == lease.LeaseId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(conversation => conversation.ActiveLeaseId, (Guid?)null)
                .SetProperty(conversation => conversation.ActiveLeaseExpiresAt, (DateTimeOffset?)null), cancellationToken);
    }

    public async Task<AgentConversationOutcome> DeleteAsync(
        Guid conversationId,
        AgentConversationScope scope,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        int deleted = await Owned(scope)
            .Where(conversation => conversation.Id == conversationId &&
                (conversation.ActiveLeaseId == null || conversation.ActiveLeaseExpiresAt <= now))
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted == 1) return AgentConversationOutcome.Succeeded;
        bool owned = await Owned(scope).AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
        return owned ? AgentConversationOutcome.Busy : AgentConversationOutcome.NotFound;
    }

    public async Task<AgentConversationOwnerDeletionResult> DeleteForOwnerAsync(
        AgentConversationOwnerScope owner,
        CancellationToken cancellationToken)
    {
        ValidateOwner(owner);
        System.Linq.Expressions.Expression<Func<AgentConversationEntity, bool>> predicate = query =>
            (string.IsNullOrEmpty(owner.TenantId) || query.TenantId == owner.TenantId) &&
            (string.IsNullOrEmpty(owner.CreatingUserId) || query.CreatingUserId == owner.CreatingUserId) &&
            (string.IsNullOrEmpty(owner.SubjectKind) || query.SubjectKind == owner.SubjectKind) &&
            (string.IsNullOrEmpty(owner.SubjectId) || query.SubjectId == owner.SubjectId);

        bool hasLiveLease = await dbContext.AgentConversations.IgnoreQueryFilters()
            .Where(predicate)
            .AnyAsync(query => query.ActiveLeaseId != null && query.ActiveLeaseExpiresAt > timeProvider.GetUtcNow(), cancellationToken);
        if (hasLiveLease)
            return new(AgentConversationOutcome.Busy, 0);

        int deleted = await DeleteWhereAsync(predicate, cancellationToken);
        return new(AgentConversationOutcome.Succeeded, deleted);
    }

    public Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        DeleteWhereAsync(query => query.UpdatedAt < cutoff, cancellationToken);

    private Task<int> DeleteWhereAsync(
        System.Linq.Expressions.Expression<Func<AgentConversationEntity, bool>> predicate,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return dbContext.AgentConversations
            .IgnoreQueryFilters()
            .Where(predicate)
            .Where(conversation => conversation.ActiveLeaseId == null || conversation.ActiveLeaseExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static void ValidateOwner(AgentConversationOwnerScope owner)
    {
        if (string.IsNullOrWhiteSpace(owner.TenantId) &&
            string.IsNullOrWhiteSpace(owner.CreatingUserId) &&
            string.IsNullOrWhiteSpace(owner.SubjectKind) &&
            string.IsNullOrWhiteSpace(owner.SubjectId))
            throw new ArgumentException("At least one non-empty owner scope filter must be provided.", nameof(owner));
    }

    private IQueryable<AgentConversationEntity> Owned(AgentConversationScope scope) =>
        dbContext.AgentConversations.Where(conversation =>
            conversation.TenantId == scope.TenantId &&
            conversation.CreatingUserId == scope.CreatingUserId &&
            conversation.AgentKey == scope.AgentKey &&
            conversation.SubjectKind == scope.SubjectKind &&
            conversation.SubjectId == scope.SubjectId);
}
