using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class GetUserInactivityTimeoutHandler(
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationSessionRegistry sessionRegistry,
    IRequestContext requestContext)
{
    public async Task<AuthenticationUserInactivityTimeoutState?> Handle(string userId, CancellationToken cancellationToken)
    {
        if (!AuthenticationSecurityAuthorization.CanChangeUserInactivityPolicy(requestContext.User))
            throw new UnauthorizedAccessException("Only platform administrators may read a user's inactivity policy.");

        if (string.IsNullOrWhiteSpace(userId) || await userDirectory.FindByIdAsync(userId, cancellationToken) is null)
            return null;

        (_, int? minutes, long version) = await sessionRegistry.GetUserInactivityTimeoutStateAsync(userId, cancellationToken);
        return new AuthenticationUserInactivityTimeoutState(version, minutes);
    }
}

public sealed class GetAuthenticationSessionNoticesHandler(IAuthenticationSessionRegistry sessionRegistry)
{
    public Task<IReadOnlyList<AuthenticationSessionNotice>> Handle(string sessionId, string userId, CancellationToken cancellationToken) =>
        sessionRegistry.GetNoticesAsync(sessionId, userId, cancellationToken);
}

public sealed class AcknowledgeAuthenticationSessionNoticeHandler(IAuthenticationSessionRegistry sessionRegistry)
{
    public Task<bool> Handle(Guid noticeId, string sessionId, string userId, CancellationToken cancellationToken) =>
        sessionRegistry.AcknowledgeNoticeAsync(noticeId.ToString("D"), sessionId, userId, cancellationToken);
}

public sealed class AcknowledgeAuthenticationAccessHistoryHandler(IAuthenticationSessionRegistry sessionRegistry)
{
    public Task<AuthenticationAccessHistory?> Handle(string userId, string? tenantId, CancellationToken cancellationToken) =>
        sessionRegistry.AcknowledgeAccessHistoryAsync(userId, tenantId, cancellationToken);
}

public sealed class ChangeUserInactivityTimeoutHandler(
    IAuthenticationSessionRegistry sessionRegistry,
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    IAuthenticationSecurityPolicyProvider policyProvider,
    IAuthenticationUserDirectory userDirectory)
{
    public async Task<long> Handle(string userId, int? minutes, long expectedVersion, CancellationToken cancellationToken)
    {
        if (!AuthenticationSecurityAuthorization.CanChangeUserInactivityPolicy(requestContext.User))
            throw new UnauthorizedAccessException("Only platform administrators may change inactivity policy.");
        if (minutes is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(minutes), "The inactivity timeout must be between 1 and 30 minutes.");
        if (expectedVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "The expected inactivity policy version cannot be negative.");
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            throw new InvalidOperationException("A tenant context is required for policy auditing.");
        string operationKey = $"{AuthenticationSecurityAuditEvents.SessionPolicy}:{requestContext.RequestId ?? Guid.NewGuid().ToString("N")}{AuthenticationSecurityAuditEvents.UserPolicyOperationSegment}{userId}";
        string timeoutReason = minutes.HasValue
            ? AuthenticationSecurityAuditEvents.UserTimeoutMinutesReason(minutes.Value)
            : AuthenticationSecurityAuditEvents.UserTimeoutDefaultReason;
        bool userExists = !string.IsNullOrWhiteSpace(userId)
            && await userDirectory.FindByIdAsync(userId, cancellationToken) is not null;
        SecurityAccessPolicy currentPolicy = await policyProvider.GetAsync(cancellationToken);
        (bool previousPolicyExists, int? previousMinutes, long previousVersion) = userExists
            ? await sessionRegistry.GetUserInactivityTimeoutStateAsync(userId, cancellationToken)
            : (false, null, 0);

        SecurityAuditWriteResult pending;
        try
        {
            pending = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = AuthenticationSecurityAuditEvents.SessionPolicy,
                Outcome = SecurityAuditOutcome.Pending,
                ActorId = requestContext.UserId,
                SubjectId = userId,
                TenantId = requestContext.TenantId!,
                Resource = AuthenticationSecurityAuditEvents.Resource,
                Operation = AuthenticationSecurityAuditEvents.SessionPolicyOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = timeoutReason,
                IdempotencyKey = operationKey
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the policy mutation when its audit start cannot be stored.
            pending = SecurityAuditWriteResult.Recorded;
        }

        if (!userExists)
        {
            await CompleteDeniedAuditBestEffortAsync(operationKey, AuthenticationSecurityAuditEvents.UserPolicyTargetNotFoundReason);
            throw new AuthenticationUserNotFoundException();
        }

        if (pending is SecurityAuditWriteResult.AlreadyRecorded)
        {
            if (auditWriter is not ISecurityAuditOutcomeUpdater reconciliationUpdater)
                throw new InvalidOperationException("The user inactivity policy operation is already in progress or its outcome is already recorded.");

            string? existingOutcome = await reconciliationUpdater.GetOutcomeAsync(requestContext.TenantId, operationKey, cancellationToken);
            (bool existingPolicyExists, int? existingMinutes, long existingVersion) = await sessionRegistry.GetUserInactivityTimeoutStateAsync(userId, cancellationToken);
            if (existingOutcome is SecurityAuditOutcome.Succeeded)
            {
                if (!existingPolicyExists || existingMinutes != minutes)
                    throw new InvalidOperationException("The idempotency key was already used for a different inactivity policy.");
                return existingVersion;
            }
            if (existingOutcome is SecurityAuditOutcome.Pending && existingPolicyExists && existingMinutes == minutes)
            {
                // The policy row proves that the mutation committed, but the prior
                // attempt may have failed while revoking an expired session. Re-run
                // only the idempotent revocation reconciliation before closing audit.
                await sessionRegistry.RevokeSessionsExpiredByUserPolicyAsync(
                    userId,
                    cancellationToken,
                    minutes ?? currentPolicy.DefaultInactivityTimeoutMinutes);
                SecurityAuditWriteResult reconciled = await reconciliationUpdater.UpdateAsync(
                    requestContext.TenantId,
                    operationKey,
                    SecurityAuditOutcome.Succeeded,
                    timeoutReason,
                    cancellationToken);
                if ((reconciled is SecurityAuditWriteResult.Recorded or SecurityAuditWriteResult.AlreadyRecorded) &&
                    await reconciliationUpdater.GetOutcomeAsync(requestContext.TenantId, operationKey, cancellationToken) is SecurityAuditOutcome.Succeeded)
                    return existingVersion;
            }
            throw new InvalidOperationException("The user inactivity policy operation is already in progress or has a terminal non-success outcome.");
        }

        bool mutationCommitted = false;
        long version = 0;
        try
        {
            if (previousVersion != expectedVersion)
                throw new AuthenticationUserSessionPolicyConcurrencyException();
            await sessionRegistry.RevokeSessionsExpiredByUserPolicyAsync(
                userId,
                cancellationToken,
                minutes ?? currentPolicy.DefaultInactivityTimeoutMinutes);
            version = await sessionRegistry.SetUserInactivityTimeoutAsync(userId, minutes, expectedVersion, cancellationToken);
            mutationCommitted = true;

            // Re-evaluate after the user policy is committed so a concurrent global-policy change cannot preserve expired sessions.
            SecurityAccessPolicy committedPolicy = await policyProvider.GetAsync(cancellationToken);
            await sessionRegistry.RevokeSessionsExpiredByUserPolicyAsync(
                userId,
                cancellationToken,
                minutes ?? committedPolicy.DefaultInactivityTimeoutMinutes);

            await CompleteAuditAsync(operationKey, timeoutReason, cancellationToken);
            return version;
        }
        catch
        {
            if (mutationCommitted)
            {
                try
                {
                    (bool currentPolicyExists, int? currentMinutes, long currentVersion) = await sessionRegistry.GetUserInactivityTimeoutStateAsync(userId, CancellationToken.None);
                    if (currentPolicyExists && currentMinutes == minutes && currentVersion == version && (!previousPolicyExists || previousMinutes != minutes))
                        _ = await sessionRegistry.SetUserInactivityTimeoutAsync(userId, previousMinutes, version, CancellationToken.None);
                }
                catch
                {
                    // Keep the stricter policy if compensation cannot be confirmed; expired sessions remain revoked fail-safe.
                }
            }
            await CompleteDeniedAuditBestEffortAsync(operationKey, timeoutReason);
            throw;
        }
    }

    private async Task CompleteAuditAsync(string operationKey, string timeoutReason, CancellationToken cancellationToken)
    {
        if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater)
        {
            try
            {
                SecurityAuditWriteResult completed = await outcomeUpdater.UpdateAsync(
                    requestContext.TenantId!,
                    operationKey,
                    SecurityAuditOutcome.Succeeded,
                    timeoutReason,
                    cancellationToken);
                if (completed is SecurityAuditWriteResult.AlreadyRecorded &&
                    await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, operationKey, cancellationToken) is not SecurityAuditOutcome.Succeeded)
                    throw new InvalidOperationException("The committed user inactivity mutation has no confirmed successful audit outcome.");
            }
            catch (SecurityAuditStoreException)
            {
                // AU-8 preserves the committed policy mutation when its audit completion cannot be stored.
            }
            return;
        }

        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = AuthenticationSecurityAuditEvents.SessionPolicy,
                Outcome = SecurityAuditOutcome.Succeeded,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = requestContext.TenantId!,
                Resource = AuthenticationSecurityAuditEvents.Resource,
                Operation = AuthenticationSecurityAuditEvents.SessionPolicyOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = timeoutReason,
                IdempotencyKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the committed policy mutation when its audit completion cannot be stored.
        }
    }

    private async Task CompleteDeniedAuditBestEffortAsync(string operationKey, string timeoutReason)
    {
        try
        {
            if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater)
            {
                _ = await outcomeUpdater.UpdateAsync(
                    requestContext.TenantId!,
                    operationKey,
                    SecurityAuditOutcome.Denied,
                    timeoutReason,
                    CancellationToken.None);
                return;
            }

            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = AuthenticationSecurityAuditEvents.SessionPolicy,
                Outcome = SecurityAuditOutcome.Denied,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = requestContext.TenantId!,
                Resource = AuthenticationSecurityAuditEvents.Resource,
                Operation = AuthenticationSecurityAuditEvents.SessionPolicyOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = timeoutReason,
                IdempotencyKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix
            }, CancellationToken.None);
        }
        catch
        {
            // The original operation failure remains authoritative; a pending audit is reconciled by a later retry.
        }
    }
}

public sealed class RevokeUserSessionsHandler(
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationSessionRegistry sessionRegistry,
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext)
{
    public async Task<int> Handle(string userId, CancellationToken cancellationToken)
    {
        string operationKey = BuildOperationKey(userId);
        if (!AuthenticationSecurityAuthorization.CanEndUserSessions(requestContext.User))
        {
            await WriteDeniedAuditBestEffortAsync(operationKey, AuthenticationSecurityAuditEvents.Unauthorized, userId);
            throw new UnauthorizedAccessException("Only platform administrators may end a user's sessions.");
        }
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            throw new InvalidOperationException("A tenant context is required for session-termination auditing.");

        bool userExists = !string.IsNullOrWhiteSpace(userId)
            && await userDirectory.FindByIdAsync(userId, cancellationToken) is not null;
        SecurityAuditWriteResult pending = await WriteAuditAsync(
            SecurityAuditOutcome.Pending,
            operationKey,
            AuthenticationSecurityAuditEvents.SessionAdministrativeRevocation,
            userId,
            cancellationToken);
        if (pending is SecurityAuditWriteResult.AlreadyRecorded)
        {
            if (auditWriter is ISecurityAuditOutcomeUpdater reconciliationUpdater)
            {
                string? existingOutcome = await reconciliationUpdater.GetOutcomeAsync(requestContext.TenantId, operationKey, cancellationToken);
                if (existingOutcome is SecurityAuditOutcome.Succeeded)
                {
                    // The original request already ended every session it could; a replay must not double-report them.
                    return 0;
                }
                if (existingOutcome is SecurityAuditOutcome.Pending)
                {
                    int reconciled = await sessionRegistry.RevokeActiveSessionsAsync(userId, cancellationToken);
                    await CompleteSucceededAuditAsync(operationKey, userId, cancellationToken);
                    return reconciled;
                }
            }

            throw new InvalidOperationException("The user session revocation is already in progress or has a terminal non-success outcome.");
        }

        if (!userExists)
        {
            await CompleteDeniedAuditBestEffortAsync(operationKey, AuthenticationSecurityAuditEvents.UserPolicyTargetNotFoundReason, userId);
            throw new AuthenticationUserNotFoundException();
        }

        try
        {
            int ended = await sessionRegistry.RevokeActiveSessionsAsync(userId, cancellationToken);
            await CompleteSucceededAuditAsync(operationKey, userId, cancellationToken);
            return ended;
        }
        catch
        {
            await CompleteDeniedAuditBestEffortAsync(operationKey, AuthenticationSecurityAuditEvents.SessionAdministrativeRevocation, userId);
            throw;
        }
    }

    private string BuildOperationKey(string userId) =>
        $"{AuthenticationSecurityAuditEvents.SessionTerminated}:{requestContext.RequestId ?? Guid.NewGuid().ToString("N")}{AuthenticationSecurityAuditEvents.UserSessionsOperationSegment}{userId}";

    private async Task<SecurityAuditWriteResult> WriteAuditAsync(string outcome, string operationKey, string? reason, string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await auditWriter.WriteAsync(CreateAuditEvent(outcome, operationKey, reason, userId), cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the session revocation when its initiating audit cannot be stored.
            return SecurityAuditWriteResult.Recorded;
        }
    }

    private async Task CompleteSucceededAuditAsync(string operationKey, string userId, CancellationToken cancellationToken)
    {
        if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater)
        {
            try
            {
                SecurityAuditWriteResult completed = await outcomeUpdater.UpdateAsync(
                    requestContext.TenantId!,
                    operationKey,
                    SecurityAuditOutcome.Succeeded,
                    AuthenticationSecurityAuditEvents.SessionAdministrativeRevocation,
                    cancellationToken);
                if (completed is SecurityAuditWriteResult.AlreadyRecorded &&
                    await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, operationKey, cancellationToken) is not SecurityAuditOutcome.Succeeded)
                {
                    throw new InvalidOperationException("The committed user session revocation has no confirmed successful audit outcome.");
                }
            }
            catch (SecurityAuditStoreException)
            {
                // AU-8 preserves the committed session revocation when its terminal audit cannot be stored.
            }
            return;
        }

        try
        {
            _ = await auditWriter.WriteAsync(
                CreateAuditEvent(
                    SecurityAuditOutcome.Succeeded,
                    operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix,
                    AuthenticationSecurityAuditEvents.SessionAdministrativeRevocation,
                    userId),
                cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the committed session revocation when its terminal audit cannot be stored.
        }
    }

    private async Task CompleteDeniedAuditBestEffortAsync(string operationKey, string reason, string userId)
    {
        try
        {
            if (auditWriter is ISecurityAuditOutcomeUpdater outcomeUpdater)
            {
                _ = await outcomeUpdater.UpdateAsync(
                    requestContext.TenantId!,
                    operationKey,
                    SecurityAuditOutcome.Denied,
                    reason,
                    CancellationToken.None);
                return;
            }

            _ = await auditWriter.WriteAsync(
                CreateAuditEvent(
                    SecurityAuditOutcome.Denied,
                    operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix,
                    reason,
                    userId),
                CancellationToken.None);
        }
        catch
        {
            // The original operation failure remains authoritative; a pending audit is reconciled by a later retry.
        }
    }

    private async Task WriteDeniedAuditBestEffortAsync(string operationKey, string reason, string userId)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            return;
        try
        {
            _ = await auditWriter.WriteAsync(CreateAuditEvent(SecurityAuditOutcome.Denied, operationKey, reason, userId), CancellationToken.None);
        }
        catch
        {
            // The authorization failure remains authoritative; the transport policy already denied the request.
        }
    }

    private SecurityAuditEvent CreateAuditEvent(string outcome, string idempotencyKey, string? reason, string userId) => new()
    {
        EventName = AuthenticationSecurityAuditEvents.SessionTerminated,
        Outcome = outcome,
        ActorId = requestContext.UserId,
        SubjectId = userId,
        TenantId = requestContext.TenantId!,
        Resource = AuthenticationSecurityAuditEvents.Resource,
        Operation = AuthenticationSecurityAuditEvents.SessionTerminatedOperation,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
        SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
        RedactedReason = reason,
        IdempotencyKey = idempotencyKey
    };
}
