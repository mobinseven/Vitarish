using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

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
    IAuthenticationSecurityPolicyProvider policyProvider)
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
        SecurityAccessPolicy currentPolicy = await policyProvider.GetAsync(cancellationToken);
        (bool previousPolicyExists, int? previousMinutes, long previousVersion) = await sessionRegistry.GetUserInactivityTimeoutStateAsync(userId, cancellationToken);
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
