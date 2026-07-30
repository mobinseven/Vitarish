using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Infrastructure.Data;

public sealed class EfAgentConversationStore(
    IntelligenceDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider) : IAgentConversationStore
{
    public const int MaximumPlaintextBytes = 1024 * 1024;
    public const string ProtectorPurpose = "Vitarish.Intelligence.AgentConversationHistory.v1";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

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
        CancellationToken cancellationToken)
    {
        if (!string.Equals(historyFormat, RunAgentConversationHandler.HistoryFormat, StringComparison.Ordinal))
            return AgentConversationOutcome.InvalidHistory;
        if (Encoding.UTF8.GetByteCount(history) > MaximumPlaintextBytes)
            return AgentConversationOutcome.HistoryLimitExceeded;

        string protectedHistory = _protector.Protect(history);
        DateTimeOffset now = timeProvider.GetUtcNow();
        int updated = await Owned(scope)
            .Where(conversation => conversation.Id == lease.ConversationId &&
                conversation.ActiveLeaseId == lease.LeaseId &&
                conversation.Revision == lease.Revision)
            .ExecuteUpdateAsync(update => update
                .SetProperty(conversation => conversation.ProtectedHistory, protectedHistory)
                .SetProperty(conversation => conversation.HistoryFormat, historyFormat)
                .SetProperty(conversation => conversation.UpdatedAt, now)
                .SetProperty(conversation => conversation.Revision, lease.Revision + 1)
                .SetProperty(conversation => conversation.ActiveLeaseId, (Guid?)null)
                .SetProperty(conversation => conversation.ActiveLeaseExpiresAt, (DateTimeOffset?)null), cancellationToken);
        return updated == 1 ? AgentConversationOutcome.Succeeded : AgentConversationOutcome.Busy;
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

    public Task<int> DeleteForOwnerAsync(AgentConversationOwnerScope owner, CancellationToken cancellationToken) =>
        DeleteWhereAsync(query =>
            (owner.TenantId == null || query.TenantId == owner.TenantId) &&
            (owner.CreatingUserId == null || query.CreatingUserId == owner.CreatingUserId) &&
            (owner.SubjectKind == null || query.SubjectKind == owner.SubjectKind) &&
            (owner.SubjectId == null || query.SubjectId == owner.SubjectId), cancellationToken);

    public Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        DeleteWhereAsync(query => query.UpdatedAt < cutoff, cancellationToken);

    private Task<int> DeleteWhereAsync(
        System.Linq.Expressions.Expression<Func<AgentConversationEntity, bool>> predicate,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return dbContext.AgentConversations
            .Where(predicate)
            .Where(conversation => conversation.ActiveLeaseId == null || conversation.ActiveLeaseExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private IQueryable<AgentConversationEntity> Owned(AgentConversationScope scope) =>
        dbContext.AgentConversations.Where(conversation =>
            conversation.TenantId == scope.TenantId &&
            conversation.CreatingUserId == scope.CreatingUserId &&
            conversation.AgentKey == scope.AgentKey &&
            conversation.SubjectKind == scope.SubjectKind &&
            conversation.SubjectId == scope.SubjectId);
}
