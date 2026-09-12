using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Entities.Server.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationSessionRegistry(
    AuthenticationDbContext dbContext,
    IAuthenticationSecurityPolicyProvider policyProvider,
    IClientIpAddressAccessor clientIp,
    IRequestContext requestContext,
    ISecurityAuditWriter auditWriter,
    TimeProvider timeProvider) : IAuthenticationSessionRegistry, IAuthenticationActiveSessionQuery
{
    public async Task<IAuthenticationSessionPreparation> PrepareAsync(string userId, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        if (!dbContext.Database.IsRelational())
            return await strategy.ExecuteAsync(async ct => await PrepareCoreAsync(userId, ct), cancellationToken);

        TaskCompletionSource<IAuthenticationSessionPreparation> prepared = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<object?> operationCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task strategyOperation = null!;
        strategyOperation = strategy.ExecuteAsync(async ct =>
        {
            AuthenticationSessionPreparation operation;
            try
            {
                operation = (AuthenticationSessionPreparation)await PrepareCoreAsync(userId, ct);
            }
            catch
            {
                // Failures before the prepared operation is published remain retryable.
                throw;
            }

            operation.AttachStrategyCompletion(completion, operationCompleted.Task);
            prepared.TrySetResult(operation);
            try
            {
                bool commit = await completion.Task;
                await operation.FinishTransactionAsync(commit, ct);
                operationCompleted.TrySetResult(null);
            }
            catch (OperationCanceledException exception)
            {
                operationCompleted.TrySetException(exception);
                throw;
            }
            catch (Exception exception)
            {
                operationCompleted.TrySetException(exception);
                throw new AuthenticationSessionTransactionException(exception);
            }
        }, cancellationToken);

        _ = strategyOperation.ContinueWith(
            task =>
            {
                if (prepared.Task.IsCompleted)
                    return;
                if (task.IsCanceled)
                    _ = prepared.TrySetCanceled(cancellationToken);
                else if (task.IsFaulted)
                    _ = prepared.TrySetException(task.Exception!.GetBaseException());
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        try
        {
            return await prepared.Task;
        }
        catch
        {
            completion.TrySetResult(false);
            await strategyOperation;
            throw;
        }
    }

    public async Task<AuthenticationSessionEstablishmentResult> EstablishAsync(string userId, CancellationToken cancellationToken)
    {
        await using IAuthenticationSessionPreparation preparation = await PrepareAsync(userId, cancellationToken);
        if (!preparation.Result.Succeeded)
            return preparation.Result;
        await preparation.CompleteAsync(cancellationToken);
        return preparation.Result;
    }

    private async Task<IAuthenticationSessionPreparation> PrepareCoreAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await PrepareCoreUnsafeAsync(userId, cancellationToken);
        }
        catch
        {
            IDbContextTransaction? transaction = dbContext.Database.CurrentTransaction;
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                await transaction.DisposeAsync();
            }
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<IAuthenticationSessionPreparation> PrepareCoreUnsafeAsync(string userId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        SecurityAccessPolicy policy = await policyProvider.GetAsync(cancellationToken);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        string? tenantId = requestContext.TenantId;
        string? address = clientIp.ClientIpAddress;

        if (string.IsNullOrWhiteSpace(tenantId))
            return new AuthenticationSessionPreparation(AuthenticationSessionEstablishmentResult.Failed(new(AuthenticationFailureReason.ProviderFailure, "A tenant context is required to establish a session.")));

        IReadOnlySet<string> allowedCidrs = policy.AllowedSessionStartCidrs ?? new HashSet<string>(StringComparer.Ordinal);
        if (allowedCidrs.Count > 0 && (address is null || !System.Net.IPAddress.TryParse(address, out System.Net.IPAddress? parsedAddress) || !allowedCidrs.Any(cidr => SessionStartCidr.Contains(cidr, parsedAddress))))
        {
            await WriteAuditAsync(AuthenticationSecurityAuditEvents.SessionEstablished, SecurityAuditOutcome.Denied, userId, AuthenticationSecurityAuditEvents.SessionIpDenied, AuthenticationSecurityAuditEvents.SessionEstablishedOperation, AuthenticationSecurityAuditEvents.SessionIpDenied, cancellationToken);
            return new AuthenticationSessionPreparation(AuthenticationSessionEstablishmentResult.Failed(new(AuthenticationFailureReason.NotAllowed, "Session creation is not allowed from this client address.")));
        }

        IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        List<(string Reason, string EventKey)> terminated = [];
        List<(AuthenticationSession Session, AuthenticationSessionTerminationReason Reason)> sessionsToTerminate = [];
        int inactivityMinutes = await GetEffectiveInactivityTimeoutAsync(userId, policy, cancellationToken);
        AuthenticationAccessHistory? existingHistory = await dbContext.AuthenticationAccessHistories
            .SingleOrDefaultAsync(item => item.UserId == userId && item.TenantId == tenantId, cancellationToken);
        AuthenticationAccessHistoryState? originalHistory = existingHistory is null ? null : Capture(existingHistory);
        List<AuthenticationSession> active = await dbContext.AuthenticationSessions
            .Where(session => session.UserId == userId && session.TerminatedAt == null)
            .ToListAsync(cancellationToken);
        List<AuthenticationSessionState> originalSessions = active.Select(Capture).ToList();

        foreach (AuthenticationSession expired in active.Where(session => session.LastActivityAt.AddMinutes(inactivityMinutes) <= now))
        {
            sessionsToTerminate.Add((expired, AuthenticationSessionTerminationReason.InactivityTimeout));
            terminated.Add((AuthenticationSecurityAuditEvents.SessionInactive, expired.Id.ToString("N")));
        }
        active = active.Where(session => session.LastActivityAt.AddMinutes(inactivityMinutes) > now).ToList();

        if (policy.MaximumConcurrentSessions == 1)
        {
            foreach (AuthenticationSession previous in active)
            {
                sessionsToTerminate.Add((previous, AuthenticationSessionTerminationReason.ReplacedByNewSession));
                terminated.Add((AuthenticationSecurityAuditEvents.SessionReplaced, previous.Id.ToString("N")));
            }
            active.Clear();
        }
        else if (active.Count >= policy.MaximumConcurrentSessions)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            await WriteAuditAsync(AuthenticationSecurityAuditEvents.SessionEstablished, SecurityAuditOutcome.Denied, userId, AuthenticationSecurityAuditEvents.SessionConcurrentLimit, AuthenticationSecurityAuditEvents.SessionEstablishedOperation, AuthenticationSecurityAuditEvents.SessionConcurrentLimit, cancellationToken);
            if (transaction is not null)
                await transaction.DisposeAsync();
            return new AuthenticationSessionPreparation(AuthenticationSessionEstablishmentResult.Failed(new(AuthenticationFailureReason.TooManyAttempts, "The maximum number of simultaneous sessions has been reached.")));
        }

        AuthenticationSession session = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            CreatedAt = now,
            LastActivityAt = now,
            ClientIpAddress = address
        };
        _ = dbContext.AuthenticationSessions.Add(session);

        List<AuthenticationSessionNotice> createdNotices = [];
        if (policy.MaximumConcurrentSessions > 1)
        {
            foreach (AuthenticationSession previous in active)
            {
                AuthenticationSessionNotice notice = new()
                {
                    Id = Guid.NewGuid(),
                    SessionId = previous.Id,
                    UserId = userId,
                    CreatedAt = now,
                    NoticeType = AuthenticationSessionNotice.ParallelSessionEstablishedNoticeType
                };
                createdNotices.Add(notice);
                _ = dbContext.AuthenticationSessionNotices.Add(notice);
            }
        }

        foreach ((string reason, string eventKey) in terminated)
            _ = await WriteAuditAsync(AuthenticationSecurityAuditEvents.SessionTerminated, SecurityAuditOutcome.Pending, userId, reason, AuthenticationSecurityAuditEvents.SessionTerminatedOperation, eventKey, cancellationToken);
        _ = await WriteAuditAsync(AuthenticationSecurityAuditEvents.SessionEstablished, SecurityAuditOutcome.Pending, userId, null, AuthenticationSecurityAuditEvents.SessionEstablishedOperation, session.Id.ToString("N"), cancellationToken);
        foreach ((AuthenticationSession sessionToTerminate, AuthenticationSessionTerminationReason reason) in sessionsToTerminate)
            Terminate(sessionToTerminate, now, reason);
        AuthenticationAccessHistory history = await RecordSuccessfulAttemptAsync(userId, tenantId, now, cancellationToken);
        await SaveAsync(cancellationToken);

        return new AuthenticationSessionPreparation(
            AuthenticationSessionEstablishmentResult.Success(session.Id.ToString("N"), Snapshot(history), inactivityMinutes),
            transaction,
            dbContext,
            originalSessions,
            history,
            originalHistory,
            session,
            createdNotices,
            CompensateEstablishmentAsync,
            completeAudit: completeCancellationToken => CompleteEstablishmentAuditAsync(userId, terminated, session.Id.ToString("N"), completeCancellationToken));
    }

    private async Task CompleteEstablishmentAuditAsync(
        string userId,
        IReadOnlyList<(string Reason, string EventKey)> terminated,
        string sessionId,
        CancellationToken cancellationToken)
    {
        foreach ((string reason, string eventKey) in terminated)
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionTerminated, SecurityAuditOutcome.Succeeded, userId, reason, AuthenticationSecurityAuditEvents.SessionTerminatedOperation, eventKey, cancellationToken);
        await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionEstablished, SecurityAuditOutcome.Succeeded, userId, null, AuthenticationSecurityAuditEvents.SessionEstablishedOperation, sessionId, cancellationToken);
    }

    public async Task RecordFailedAttemptAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(requestContext.TenantId))
            return;
        string tenantId = requestContext.TenantId;
        AuthenticationAccessHistory? history = await dbContext.AuthenticationAccessHistories.SingleOrDefaultAsync(item => item.UserId == userId && item.TenantId == tenantId, cancellationToken);
        AuthenticationAccessHistoryState? original = history is null ? null : Capture(history);
        SecurityAuditWriteResult pending = await WriteAuditAsync(AuthenticationSecurityAuditEvents.SessionEstablished, SecurityAuditOutcome.Pending, userId, AuthenticationSecurityAuditEvents.AuthenticationFailed, AuthenticationSecurityAuditEvents.SessionEstablishedOperation, AuthenticationSecurityAuditEvents.AuthenticationFailed, cancellationToken);
        if (pending is SecurityAuditWriteResult.AlreadyRecorded)
        {
            if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater &&
                await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId, BuildAuditIdempotencyKey(AuthenticationSecurityAuditEvents.SessionEstablished, userId, AuthenticationSecurityAuditEvents.AuthenticationFailed, AuthenticationSecurityAuditEvents.AuthenticationFailed), cancellationToken) is SecurityAuditOutcome.Denied)
                return;
            throw new InvalidOperationException("The failed authentication outcome is already in progress or its terminal outcome is unconfirmed.");
        }
        history ??= new AuthenticationAccessHistory { UserId = userId, TenantId = tenantId };
        history.LastUnsuccessfulAt = timeProvider.GetUtcNow().UtcDateTime;
        history.CurrentFailedAttempts++;
        if (dbContext.Entry(history).State == EntityState.Detached)
            _ = dbContext.AuthenticationAccessHistories.Add(history);
        bool persisted = false;
        try
        {
            await SaveAsync(cancellationToken);
            persisted = true;
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionEstablished, SecurityAuditOutcome.Denied, userId, AuthenticationSecurityAuditEvents.AuthenticationFailed, AuthenticationSecurityAuditEvents.SessionEstablishedOperation, AuthenticationSecurityAuditEvents.AuthenticationFailed, cancellationToken);
        }
        catch
        {
            if (persisted)
                await CompensateAccessHistoryAsync(history, original);
            throw;
        }
    }

    public async Task<AuthenticationSessionValidationResult> ValidateAndTouchAsync(string sessionId, string userId, string? tenantId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out Guid id) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tenantId))
            return new(false);

        AuthenticationSession? session = await dbContext.AuthenticationSessions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null || session.TerminatedAt is not null || session.UserId != userId || session.TenantId != tenantId)
            return new(false);

        SecurityAccessPolicy policy = await policyProvider.GetAsync(cancellationToken);
        int inactivityMinutes = await GetEffectiveInactivityTimeoutAsync(userId, policy, cancellationToken);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (session.LastActivityAt.AddMinutes(inactivityMinutes) <= now)
        {
            string eventKey = session.Id.ToString("N");
            await BeginTerminationAuditAsync(userId, AuthenticationSecurityAuditEvents.SessionInactive, eventKey, cancellationToken);
            Terminate(session, now, AuthenticationSessionTerminationReason.InactivityTimeout);
            await SaveAsync(cancellationToken);
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionTerminated, SecurityAuditOutcome.Succeeded, userId, AuthenticationSecurityAuditEvents.SessionInactive, AuthenticationSecurityAuditEvents.SessionTerminatedOperation, eventKey, cancellationToken);
            return new(false);
        }

        session.LastActivityAt = now;
        await SaveAsync(cancellationToken);
        return new(true, inactivityMinutes);
    }

    public async Task RevokeAsync(string sessionId, AuthenticationSessionTerminationReason reason, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out Guid id))
            return;
        AuthenticationSession? session = await dbContext.AuthenticationSessions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
            return;
        string reasonText = AuthenticationSecurityAuditEvents.SessionTerminationReason(reason);
        string eventKey = session.Id.ToString("N");
        if (session.TerminatedAt is not null)
        {
            await ReconcilePendingTerminationAuditAsync(session.UserId, reasonText, eventKey, cancellationToken);
            return;
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        await BeginTerminationAuditAsync(session.UserId, reasonText, eventKey, cancellationToken);
        Terminate(session, now, reason);
        await SaveAsync(cancellationToken);
        await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionTerminated, SecurityAuditOutcome.Succeeded, session.UserId, reasonText, AuthenticationSecurityAuditEvents.SessionTerminatedOperation, eventKey, cancellationToken);
    }

    public async Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        SecurityAccessPolicy policy = await policyProvider.GetAsync(cancellationToken);
        int inactivityMinutes = await GetEffectiveInactivityTimeoutAsync(userId, policy, cancellationToken);
        DateTime cutoff = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-inactivityMinutes);
        return await dbContext.AuthenticationSessions
            .AnyAsync(session => session.UserId == userId && session.TerminatedAt == null && session.LastActivityAt > cutoff, cancellationToken);
    }

    public async Task<AuthenticationAccessHistory?> GetAccessHistoryAsync(string userId, string? tenantId, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(tenantId)
            ? null
            : await dbContext.AuthenticationAccessHistories.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId && item.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<AuthenticationSessionNotice>> GetNoticesAsync(string sessionId, string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out Guid id))
            return [];
        return await dbContext.AuthenticationSessionNotices.AsNoTracking()
            .Where(notice => notice.SessionId == id && notice.UserId == userId && notice.AcknowledgedAt == null)
            .OrderBy(notice => notice.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AcknowledgeNoticeAsync(string noticeId, string sessionId, string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(noticeId, out Guid id) || !Guid.TryParse(sessionId, out Guid session))
            return false;
        AuthenticationSessionNotice? notice = await dbContext.AuthenticationSessionNotices.SingleOrDefaultAsync(item => item.Id == id && item.SessionId == session && item.UserId == userId, cancellationToken);
        if (notice is null)
            return false;
        bool alreadyAcknowledged = notice.AcknowledgedAt.HasValue;
        SecurityAuditWriteResult pending = await WriteAuditAsync(AuthenticationSecurityAuditEvents.SessionNotice, SecurityAuditOutcome.Pending, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, AuthenticationSecurityAuditEvents.SessionNoticeOperation, notice.Id.ToString("N"), cancellationToken);
        if (pending is SecurityAuditWriteResult.AlreadyRecorded && !alreadyAcknowledged)
        {
            if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater &&
                await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, BuildAuditIdempotencyKey(AuthenticationSecurityAuditEvents.SessionNotice, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, notice.Id.ToString("N")), cancellationToken) is SecurityAuditOutcome.Succeeded)
                return true;
            throw new InvalidOperationException("The session-notice acknowledgement is already in progress or its terminal outcome is unconfirmed.");
        }
        if (alreadyAcknowledged)
        {
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionNotice, SecurityAuditOutcome.Succeeded, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, AuthenticationSecurityAuditEvents.SessionNoticeOperation, notice.Id.ToString("N"), cancellationToken);
            return true;
        }
        DateTime? originalAcknowledgedAt = notice.AcknowledgedAt;
        notice.AcknowledgedAt ??= timeProvider.GetUtcNow().UtcDateTime;
        bool persisted = false;
        try
        {
            await SaveAsync(cancellationToken);
            persisted = true;
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionNotice, SecurityAuditOutcome.Succeeded, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, AuthenticationSecurityAuditEvents.SessionNoticeOperation, notice.Id.ToString("N"), cancellationToken);
        }
        catch
        {
            if (persisted)
            {
                notice.AcknowledgedAt = originalAcknowledgedAt;
                await SaveAsync(CancellationToken.None);
            }
            throw;
        }
        return true;
    }

    public async Task<AuthenticationAccessHistory?> AcknowledgeAccessHistoryAsync(string userId, string? tenantId, CancellationToken cancellationToken)
    {
        AuthenticationAccessHistory? history = string.IsNullOrWhiteSpace(tenantId)
            ? null
            : await dbContext.AuthenticationAccessHistories.SingleOrDefaultAsync(item => item.UserId == userId && item.TenantId == tenantId, cancellationToken);
        if (history is null)
            return null;
        bool alreadyAcknowledged = history.AcknowledgedAt.HasValue;
        SecurityAuditWriteResult pending = await WriteAuditAsync(AuthenticationSecurityAuditEvents.SessionAccessHistory, SecurityAuditOutcome.Pending, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, AuthenticationSecurityAuditEvents.SessionAccessHistoryOperation, AuthenticationSecurityAuditEvents.SessionAcknowledged, cancellationToken);
        if (pending is SecurityAuditWriteResult.AlreadyRecorded && !alreadyAcknowledged)
        {
            if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater &&
                await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, BuildAuditIdempotencyKey(AuthenticationSecurityAuditEvents.SessionAccessHistory, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, AuthenticationSecurityAuditEvents.SessionAcknowledged), cancellationToken) is SecurityAuditOutcome.Succeeded)
                return history;
            throw new InvalidOperationException("The access-history acknowledgement is already in progress or its terminal outcome is unconfirmed.");
        }
        if (alreadyAcknowledged)
        {
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionAccessHistory, SecurityAuditOutcome.Succeeded, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, AuthenticationSecurityAuditEvents.SessionAccessHistoryOperation, AuthenticationSecurityAuditEvents.SessionAcknowledged, cancellationToken);
            return history;
        }
        DateTime? originalAcknowledgedAt = history.AcknowledgedAt;
        history.AcknowledgedAt ??= timeProvider.GetUtcNow().UtcDateTime;
        bool persisted = false;
        try
        {
            await SaveAsync(cancellationToken);
            persisted = true;
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionAccessHistory, SecurityAuditOutcome.Succeeded, userId, AuthenticationSecurityAuditEvents.SessionAcknowledged, AuthenticationSecurityAuditEvents.SessionAccessHistoryOperation, AuthenticationSecurityAuditEvents.SessionAcknowledged, cancellationToken);
        }
        catch
        {
            if (persisted)
            {
                history.AcknowledgedAt = originalAcknowledgedAt;
                await SaveAsync(CancellationToken.None);
            }
            throw;
        }
        return history;
    }

    public async Task<int?> GetUserInactivityTimeoutAsync(string userId, CancellationToken cancellationToken) =>
        (await GetUserInactivityTimeoutStateAsync(userId, cancellationToken)).Minutes;

    public async Task<(bool Exists, int? Minutes, long Version)> GetUserInactivityTimeoutStateAsync(string userId, CancellationToken cancellationToken)
    {
        AuthenticationUserSessionPolicyRecord? record = await dbContext.AuthenticationUserSessionPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        return (record is not null, record?.InactivityTimeoutMinutes, record?.Version ?? 0);
    }

    public async Task<long> SetUserInactivityTimeoutAsync(string userId, int? minutes, long expectedVersion, CancellationToken cancellationToken)
    {
        if (minutes is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(minutes), "The inactivity timeout must be between 1 and 30 minutes.");
        if (expectedVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "The expected inactivity policy version cannot be negative.");
        AuthenticationUserSessionPolicyRecord? record = await dbContext.AuthenticationUserSessionPolicies.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (record is null)
        {
            if (expectedVersion != 0)
                throw ConcurrentUserSessionPolicyUpdate();
            record = new AuthenticationUserSessionPolicyRecord { UserId = userId, InactivityTimeoutMinutes = minutes, Version = 1 };
            _ = dbContext.AuthenticationUserSessionPolicies.Add(record);
        }
        else if (record.Version != expectedVersion)
        {
            throw ConcurrentUserSessionPolicyUpdate();
        }
        else if (record.InactivityTimeoutMinutes == minutes)
        {
            return record.Version;
        }
        else
        {
            record.InactivityTimeoutMinutes = minutes;
            record.Version++;
        }
        try
        {
            await SaveAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw ConcurrentUserSessionPolicyUpdate(exception);
        }
        catch (DbUpdateException exception) when (DatabaseExceptions.IsUniqueConstraintViolation(exception))
        {
            dbContext.Entry(record).State = EntityState.Detached;
            throw ConcurrentUserSessionPolicyUpdate(exception);
        }
        return record.Version;
    }

    public async Task RevokeSessionsExpiredByUserPolicyAsync(string userId, CancellationToken cancellationToken, int? inactivityTimeoutOverride = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        SecurityAccessPolicy policy = await policyProvider.GetAsync(cancellationToken);
        int inactivityMinutes = inactivityTimeoutOverride ?? await GetEffectiveInactivityTimeoutAsync(userId, policy, cancellationToken);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        List<AuthenticationSession> allSessions = await dbContext.AuthenticationSessions
            .Where(session => session.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (AuthenticationSession session in allSessions.Where(session => session.TerminatedAt is not null))
            await ReconcilePendingTerminationAuditAsync(userId, AuthenticationSecurityAuditEvents.SessionUserPolicyExpired, session.Id.ToString("N"), cancellationToken);
        List<AuthenticationSession> sessions = allSessions
            .Where(session => session.TerminatedAt is null && session.LastActivityAt.AddMinutes(inactivityMinutes) <= now)
            .ToList();
        foreach (AuthenticationSession session in sessions)
        {
            await BeginTerminationAuditAsync(userId, AuthenticationSecurityAuditEvents.SessionUserPolicyExpired, session.Id.ToString("N"), cancellationToken);
        }
        foreach (AuthenticationSession session in sessions)
            Terminate(session, now, AuthenticationSessionTerminationReason.AdministrativeRevocation);
        if (sessions.Count > 0)
            await SaveAsync(cancellationToken);
        foreach (AuthenticationSession session in sessions)
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionTerminated, SecurityAuditOutcome.Succeeded, userId, AuthenticationSecurityAuditEvents.SessionUserPolicyExpired, AuthenticationSecurityAuditEvents.SessionTerminatedOperation, session.Id.ToString("N"), cancellationToken);
    }

    public async Task RevokeSessionsInvalidatedByPolicyAsync(SecurityAccessPolicy policy, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        List<AuthenticationSession> allSessions = await dbContext.AuthenticationSessions
            .Where(session => session.TerminatedAt == null)
            .OrderByDescending(session => session.CreatedAt)
            .ToListAsync(cancellationToken);
        List<AuthenticationSession> terminatedSessions = await dbContext.AuthenticationSessions
            .Where(session => session.TerminatedAt != null)
            .ToListAsync(cancellationToken);
        foreach (AuthenticationSession session in terminatedSessions)
            await ReconcilePendingTerminationAuditAsync(session.UserId, AuthenticationSecurityAuditEvents.SessionPolicyInvalidated, session.Id.ToString("N"), cancellationToken);
        List<AuthenticationSession> sessions = allSessions;
        Dictionary<string, int?> userTimeouts = await dbContext.AuthenticationUserSessionPolicies
            .AsNoTracking()
            .ToDictionaryAsync(item => item.UserId, item => item.InactivityTimeoutMinutes, cancellationToken);
        Dictionary<string, int> retainedByUser = [];
        List<AuthenticationSession> invalidated = [];
        foreach (AuthenticationSession session in sessions)
        {
            int retained = retainedByUser.GetValueOrDefault(session.UserId);
            bool overLimit = retained >= policy.MaximumConcurrentSessions;
            int inactivityMinutes = userTimeouts.GetValueOrDefault(session.UserId) ?? policy.DefaultInactivityTimeoutMinutes;
            bool expired = session.LastActivityAt.AddMinutes(inactivityMinutes) <= now;
            bool disallowedIp = policy.AllowedSessionStartCidrs is { Count: > 0 } &&
                (!System.Net.IPAddress.TryParse(session.ClientIpAddress, out System.Net.IPAddress? address) || !policy.AllowedSessionStartCidrs.Any(cidr => SessionStartCidr.Contains(cidr, address)));
            if (overLimit || expired || disallowedIp)
            {
                invalidated.Add(session);
                continue;
            }
            retainedByUser[session.UserId] = retained + 1;
        }
        foreach (AuthenticationSession session in invalidated)
            await BeginTerminationAuditAsync(session.UserId, AuthenticationSecurityAuditEvents.SessionPolicyInvalidated, session.Id.ToString("N"), cancellationToken);
        foreach (AuthenticationSession session in invalidated)
            Terminate(session, now, AuthenticationSessionTerminationReason.AdministrativeRevocation);
        if (invalidated.Count > 0)
            await SaveAsync(cancellationToken);
        foreach (AuthenticationSession session in invalidated)
            await CompleteAuditAsync(AuthenticationSecurityAuditEvents.SessionTerminated, SecurityAuditOutcome.Succeeded, session.UserId, AuthenticationSecurityAuditEvents.SessionPolicyInvalidated, AuthenticationSecurityAuditEvents.SessionTerminatedOperation, session.Id.ToString("N"), cancellationToken);
    }

    private async Task<AuthenticationAccessHistory> RecordSuccessfulAttemptAsync(string userId, string tenantId, DateTime now, CancellationToken cancellationToken)
    {
        AuthenticationAccessHistory? history = await dbContext.AuthenticationAccessHistories.SingleOrDefaultAsync(item => item.UserId == userId && item.TenantId == tenantId, cancellationToken);
        history ??= new AuthenticationAccessHistory { UserId = userId, TenantId = tenantId };
        history.PreviousSuccessfulAt = history.LastSuccessfulAt;
        history.LastSuccessfulAt = now;
        history.FailedAttemptsSincePreviousSuccessful = history.CurrentFailedAttempts;
        history.CurrentFailedAttempts = 0;
        if (history.UserId == userId && history.TenantId == tenantId && dbContext.Entry(history).State == EntityState.Detached)
            _ = dbContext.AuthenticationAccessHistories.Add(history);
        return history;
    }

    private static void Terminate(AuthenticationSession session, DateTime now, AuthenticationSessionTerminationReason reason)
    {
        session.TerminatedAt = now;
        session.TerminationReason = reason;
    }

    private static AuthenticationAccessHistory Snapshot(AuthenticationAccessHistory history) => new()
    {
        UserId = history.UserId,
        TenantId = history.TenantId,
        PreviousSuccessfulAt = history.PreviousSuccessfulAt,
        LastSuccessfulAt = history.LastSuccessfulAt,
        LastUnsuccessfulAt = history.LastUnsuccessfulAt,
        FailedAttemptsSincePreviousSuccessful = history.FailedAttemptsSincePreviousSuccessful,
        CurrentFailedAttempts = history.CurrentFailedAttempts,
        AcknowledgedAt = history.AcknowledgedAt
    };

    private async Task CompensateEstablishmentAsync(
        IReadOnlyList<AuthenticationSessionState> originalSessions,
        AuthenticationAccessHistory? history,
        AuthenticationAccessHistoryState? originalHistory,
        AuthenticationSession session,
        IReadOnlyList<AuthenticationSessionNotice> createdNotices)
    {
        async Task CompensateCoreAsync(CancellationToken cancellationToken)
        {
            foreach (AuthenticationSessionState original in originalSessions)
            {
                original.Session.TerminatedAt = original.TerminatedAt;
                original.Session.TerminationReason = original.TerminationReason;
            }

            foreach (AuthenticationSessionNotice notice in createdNotices)
            {
                if (dbContext.Entry(notice).State != EntityState.Detached)
                    _ = dbContext.AuthenticationSessionNotices.Remove(notice);
            }

            if (dbContext.Entry(session).State != EntityState.Detached)
                _ = dbContext.AuthenticationSessions.Remove(session);

            if (history is not null)
            {
                if (originalHistory is null)
                    _ = dbContext.AuthenticationAccessHistories.Remove(history);
                else
                    Restore(history, originalHistory);
            }

            await SaveAsync(cancellationToken);
        }

        if (dbContext.Database.IsRelational())
            await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(CompensateCoreAsync, CancellationToken.None);
        else
            await CompensateCoreAsync(CancellationToken.None);
    }

    private async Task CompensateAccessHistoryAsync(AuthenticationAccessHistory history, AuthenticationAccessHistoryState? original)
    {
        if (original is null)
            _ = dbContext.AuthenticationAccessHistories.Remove(history);
        else
            Restore(history, original);
        await SaveAsync(CancellationToken.None);
    }

    private static AuthenticationSessionState Capture(AuthenticationSession session) =>
        new(session, session.TerminatedAt, session.TerminationReason);

    private static AuthenticationAccessHistoryState Capture(AuthenticationAccessHistory history) =>
        new(
            history.PreviousSuccessfulAt,
            history.LastSuccessfulAt,
            history.LastUnsuccessfulAt,
            history.FailedAttemptsSincePreviousSuccessful,
            history.CurrentFailedAttempts,
            history.AcknowledgedAt);

    private static void Restore(AuthenticationAccessHistory history, AuthenticationAccessHistoryState original)
    {
        history.PreviousSuccessfulAt = original.PreviousSuccessfulAt;
        history.LastSuccessfulAt = original.LastSuccessfulAt;
        history.LastUnsuccessfulAt = original.LastUnsuccessfulAt;
        history.FailedAttemptsSincePreviousSuccessful = original.FailedAttemptsSincePreviousSuccessful;
        history.CurrentFailedAttempts = original.CurrentFailedAttempts;
        history.AcknowledgedAt = original.AcknowledgedAt;
    }

    private Task SaveAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private async Task<SecurityAuditWriteResult> WriteAuditAsync(string eventName, string outcome, string userId, string? reason, string operation, string? eventKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            return SecurityAuditWriteResult.Recorded;

        try
        {
            return await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = userId,
                TenantId = requestContext.TenantId,
                Resource = AuthenticationSecurityAuditEvents.Resource,
                Operation = operation,
                OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N"),
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = $"{eventName}:{requestContext.RequestId ?? requestContext.TraceId ?? userId}:{eventKey ?? reason ?? AuthenticationSecurityAuditEvents.DefaultEventKey}"
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the session operation; the writer emits the redacted host error.
            return SecurityAuditWriteResult.Recorded;
        }
    }

    private async Task<SecurityAuditWriteResult> CompleteAuditAsync(string eventName, string outcome, string userId, string? reason, string operation, string? eventKey, CancellationToken cancellationToken)
    {
        try
        {
            if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater && !string.IsNullOrWhiteSpace(requestContext.TenantId))
            {
                string idempotencyKey = BuildAuditIdempotencyKey(eventName, userId, reason, eventKey);
                SecurityAuditWriteResult result = await outcomeUpdater.UpdateAsync(
                    requestContext.TenantId,
                    idempotencyKey,
                    outcome,
                    reason,
                    cancellationToken);
                if (result is SecurityAuditWriteResult.AlreadyRecorded)
                {
                    string? confirmed = await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId, idempotencyKey, cancellationToken);
                    if (!string.Equals(confirmed, outcome, StringComparison.Ordinal))
                        throw new SecurityAuditStoreException("The mandatory security audit event could not confirm its terminal outcome.", new InvalidOperationException());
                }
                return result;
            }

            return await WriteAuditAsync(eventName, outcome, userId, reason, operation, eventKey is null ? null : eventKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the committed session operation when its terminal audit cannot be stored.
            return SecurityAuditWriteResult.Recorded;
        }
    }

    private async Task BeginTerminationAuditAsync(string userId, string reason, string eventKey, CancellationToken cancellationToken)
    {
        SecurityAuditWriteResult pending = await WriteAuditAsync(
            AuthenticationSecurityAuditEvents.SessionTerminated,
            SecurityAuditOutcome.Pending,
            userId,
            reason,
            AuthenticationSecurityAuditEvents.SessionTerminatedOperation,
            eventKey,
            cancellationToken);
        if (pending is not SecurityAuditWriteResult.AlreadyRecorded)
            return;

        if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater && !string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            string? existingOutcome = await outcomeUpdater.GetOutcomeAsync(
                requestContext.TenantId,
                BuildAuditIdempotencyKey(AuthenticationSecurityAuditEvents.SessionTerminated, userId, reason, eventKey),
                cancellationToken);
            if (existingOutcome is SecurityAuditOutcome.Succeeded or SecurityAuditOutcome.Denied or SecurityAuditOutcome.Failed)
                throw new InvalidOperationException("The session-termination operation already has a terminal outcome while the session remains active.");
        }

        throw new InvalidOperationException("The session-termination operation is already in progress or its terminal outcome is unconfirmed.");
    }

    private async Task ReconcilePendingTerminationAuditAsync(string userId, string reason, string eventKey, CancellationToken cancellationToken)
    {
        if (auditWriter is not ISecurityAuditOutcomeUpdater outcomeUpdater || string.IsNullOrWhiteSpace(requestContext.TenantId))
            return;

        string idempotencyKey = BuildAuditIdempotencyKey(
            AuthenticationSecurityAuditEvents.SessionTerminated,
            userId,
            reason,
            eventKey);
        string? existingOutcome = await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId, idempotencyKey, cancellationToken);
        if (existingOutcome is SecurityAuditOutcome.Pending)
        {
            await CompleteAuditAsync(
                AuthenticationSecurityAuditEvents.SessionTerminated,
                SecurityAuditOutcome.Succeeded,
                userId,
                reason,
                AuthenticationSecurityAuditEvents.SessionTerminatedOperation,
                eventKey,
                cancellationToken);
        }
    }

    private string BuildAuditIdempotencyKey(string eventName, string userId, string? reason, string? eventKey) =>
        $"{eventName}:{requestContext.RequestId ?? requestContext.TraceId ?? userId}:{eventKey ?? reason ?? AuthenticationSecurityAuditEvents.DefaultEventKey}";

    private async Task<int> GetEffectiveInactivityTimeoutAsync(string userId, SecurityAccessPolicy policy, CancellationToken cancellationToken) =>
        (await GetUserInactivityTimeoutAsync(userId, cancellationToken)) ?? policy.DefaultInactivityTimeoutMinutes;

    private static AuthenticationUserSessionPolicyConcurrencyException ConcurrentUserSessionPolicyUpdate(Exception? innerException = null) =>
        new(innerException);

    private sealed class AuthenticationSessionTransactionException(Exception innerException)
        : Exception("The prepared authentication session transaction could not complete.", innerException);

    private sealed record AuthenticationSessionState(
        AuthenticationSession Session,
        DateTime? TerminatedAt,
        AuthenticationSessionTerminationReason? TerminationReason);

    private sealed record AuthenticationAccessHistoryState(
        DateTime? PreviousSuccessfulAt,
        DateTime? LastSuccessfulAt,
        DateTime? LastUnsuccessfulAt,
        int FailedAttemptsSincePreviousSuccessful,
        int CurrentFailedAttempts,
        DateTime? AcknowledgedAt);

    private sealed class AuthenticationSessionPreparation(
        AuthenticationSessionEstablishmentResult result,
        IDbContextTransaction? transaction = null,
        AuthenticationDbContext? dbContext = null,
        IReadOnlyList<AuthenticationSessionState>? originalSessions = null,
        AuthenticationAccessHistory? existingHistory = null,
        AuthenticationAccessHistoryState? originalHistory = null,
        AuthenticationSession? session = null,
        IReadOnlyList<AuthenticationSessionNotice>? createdNotices = null,
        Func<IReadOnlyList<AuthenticationSessionState>, AuthenticationAccessHistory?, AuthenticationAccessHistoryState?, AuthenticationSession, IReadOnlyList<AuthenticationSessionNotice>, Task>? compensate = null,
        Func<CancellationToken, Task>? completeAudit = null) : IAuthenticationSessionPreparation
    {
        private bool completed;
        private bool aborted;
        private TaskCompletionSource<bool>? strategyCompletion;
        private Task? strategyOperation;
        private bool transactionFinished;
        private bool transactionCommitted;

        public AuthenticationSessionEstablishmentResult Result { get; } = result;

        public void AttachStrategyCompletion(TaskCompletionSource<bool> completion, Task operation)
        {
            strategyCompletion = completion;
            strategyOperation = operation;
        }

        public async Task FinishTransactionAsync(bool commit, CancellationToken cancellationToken)
        {
            if (transactionFinished)
                return;
            transactionFinished = true;
            if (transaction is null)
                return;

            try
            {
                if (commit)
                {
                    try
                    {
                        await transaction.CommitAsync(cancellationToken);
                        transactionCommitted = true;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        throw new AuthenticationSessionCommitException(exception);
                    }
                }
                else
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
            }
            finally
            {
                await transaction.DisposeAsync();
                if (!commit)
                    dbContext?.ChangeTracker.Clear();
            }
        }

        public async Task CompleteAsync(CancellationToken cancellationToken)
        {
            if (completed || aborted) return;
            try
            {
                if (strategyCompletion is not null)
                {
                    strategyCompletion.TrySetResult(true);
                    await strategyOperation!;
                }
                else if (transaction is not null)
                {
                    await FinishTransactionAsync(true, cancellationToken);
                }
                if (completeAudit is not null)
                    await completeAudit(cancellationToken);
                completed = true;
            }
            catch
            {
                // A failed commit has an unknown outcome: the provider may have committed
                // before reporting the failure.  Compensate after any finished relational
                // transaction so a staged cookie can never leave a usable orphan session.
                if (compensate is not null && session is not null && (transaction is null || transactionCommitted || transactionFinished))
                {
                    await compensate(originalSessions ?? [], existingHistory, originalHistory, session, createdNotices ?? []);
                }
                aborted = true;
                throw;
            }
        }

        public async Task AbortAsync(CancellationToken cancellationToken)
        {
            if (completed || aborted) return;
            if (strategyCompletion is not null)
            {
                strategyCompletion.TrySetResult(false);
                await strategyOperation!;
            }
            else if (transaction is not null)
            {
                await FinishTransactionAsync(false, cancellationToken);
            }
            else if (session is not null && compensate is not null)
            {
                await compensate(originalSessions ?? [], existingHistory, originalHistory, session, createdNotices ?? []);
            }
            aborted = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!completed && !aborted)
                await AbortAsync(CancellationToken.None);
        }

        private sealed class AuthenticationSessionCommitException(Exception innerException)
            : Exception("The authentication session transaction could not be committed.", innerException);
    }
}
