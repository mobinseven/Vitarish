using System.Data.Common;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class GetSecurityAccessPolicyHandler(
    IAuthenticationSecurityPolicyStore policyStore,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext)
{
    public async Task<SecurityAccessPolicyState> Handle(GetSecurityAccessPolicyQuery query, CancellationToken cancellationToken)
    {
        bool authorized = AuthenticationSecurityAuthorization.CanReadSessionPolicy(requestContext.User);
        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        SecurityAuditWriteResult started = await WriteAuditAsync(
            authorized ? SecurityAuditOutcome.Started : SecurityAuditOutcome.Denied,
            operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyStartedSuffix,
            authorized ? null : AuthenticationSecurityAuditEvents.Unauthorized,
            cancellationToken);

        if (!authorized)
        {
            await WriteAuditAsync(SecurityAuditOutcome.Denied, operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix, AuthenticationSecurityAuditEvents.Unauthorized, cancellationToken);
            throw new UnauthorizedAccessException("Only platform administrators may read the security access policy.");
        }
        if (started is SecurityAuditWriteResult.AlreadyRecorded)
            throw new InvalidOperationException("The security policy operation is already in progress or its outcome is already recorded.");

        SecurityAccessPolicyState policy = await policyStore.GetStateAsync(cancellationToken);
        _ = await WriteAuditAsync(SecurityAuditOutcome.Succeeded, operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix, null, cancellationToken);
        return policy;
    }

    private async Task<SecurityAuditWriteResult> WriteAuditAsync(string outcome, string idempotencyKey, string? reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            throw new InvalidOperationException("A tenant context is required for security auditing.");

        try
        {
            return await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = AuthenticationSecurityAuditEvents.SecurityAccessPolicy,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = requestContext.TenantId,
                Resource = AuthenticationSecurityAuditEvents.SecurityAccessPolicyResource,
                Operation = AuthenticationSecurityAuditEvents.SecurityAccessPolicyOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = idempotencyKey
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the policy read when its audit write cannot be stored.
            return SecurityAuditWriteResult.Recorded;
        }
    }
}

public sealed class GetSecurityAuthenticationPolicyHandler(
    IAuthenticationSecurityPolicyStore policyStore,
    AuthenticationProfilePolicy profilePolicy,
    IRequestContext requestContext)
{
    public async Task<SecurityAuthenticationPolicyManagementState> Handle(CancellationToken cancellationToken)
    {
        if (!AuthenticationSecurityAuthorization.CanReadSessionPolicy(requestContext.User))
            throw new UnauthorizedAccessException("Only platform administrators may read the authentication policy.");

        SecurityAccessPolicyState stored = await policyStore.GetStateAsync(cancellationToken);
        return SecurityAuthenticationPolicyManagementState.ForState(stored, profilePolicy);
    }
}

public sealed class ChangeSecurityAccessPolicyHandler(
    IAuthenticationSecurityPolicyStore policyStore,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext,
    IAuthenticationSessionRegistry sessionRegistry,
    AuthenticationProfilePolicy? profilePolicy = null)
{
    public Task<SecurityAccessPolicyState> Handle(ChangeSecurityAccessPolicyCommand command, CancellationToken cancellationToken) =>
        HandleCore(command.ExpectedVersion, _ => command.Policy, validateProfileConstraints: true, cancellationToken);

    public Task<SecurityAccessPolicyState> Handle(ChangeSecuritySessionPolicyCommand command, CancellationToken cancellationToken) =>
        HandleCore(command.ExpectedVersion, current => current with
        {
            MaximumConcurrentSessions = command.MaximumConcurrentSessions,
            ConcurrentSessionsFunctionallyRequired = command.ConcurrentSessionsFunctionallyRequired,
            DefaultInactivityTimeoutMinutes = command.DefaultInactivityTimeoutMinutes,
            AllowedSessionStartCidrs = SessionStartCidr.NormalizeSet(command.AllowedSessionStartCidrs ?? [])
        }, validateProfileConstraints: false, cancellationToken);

    public Task<SecurityAccessPolicyState> Handle(ChangeSecurityAuthenticationPolicyCommand command, CancellationToken cancellationToken) =>
        HandleCore(command.ExpectedVersion, current => current with
        {
            PasswordPolicy = new PasswordPolicy(
                command.MinimumPasswordLength,
                command.RequirePasswordLowercase,
                command.RequirePasswordUppercase,
                command.RequirePasswordNumber,
                command.RequirePasswordSpecialCharacter),
            FailedAttemptThreshold = command.FailedAttemptThreshold,
            FailedAttemptDenialDuration = TimeSpan.FromSeconds(command.FailedAttemptDenialDurationSeconds),
            // Omitted by clients that predate the per-address threshold; they keep the stored value.
            SourceFailedAttemptThreshold = command.SourceFailedAttemptThreshold ?? current.SourceFailedAttemptThreshold,
            FailedAttemptResponse = ParsePolicyEnum<FailedAttemptResponse>(command.FailedAttemptResponse),
            EnabledAuthenticationMechanisms = ParsePolicyEnumSet<AuthenticationMethod>(command.EnabledAuthenticationMechanisms),
            PermittedPreAuthenticationActions = ParsePolicyEnumSet<PreAuthenticationAction>(command.PermittedPreAuthenticationActions)
        }, validateProfileConstraints: true, cancellationToken);

    private async Task<SecurityAccessPolicyState> HandleCore(
        long expectedVersion,
        Func<SecurityAccessPolicy, SecurityAccessPolicy> applyChanges,
        bool validateProfileConstraints,
        CancellationToken cancellationToken)
    {
        bool authorized = AuthenticationSecurityAuthorization.CanChangeSessionPolicy(requestContext.User);
        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        SecurityAuditWriteResult started = await WriteAuditAsync(
            authorized ? SecurityAuditOutcome.Started : SecurityAuditOutcome.Denied,
            operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyStartedSuffix,
            authorized ? null : AuthenticationSecurityAuditEvents.Unauthorized,
            cancellationToken);

        if (!authorized)
        {
            await WriteAuditAsync(SecurityAuditOutcome.Denied, operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix, AuthenticationSecurityAuditEvents.Unauthorized, cancellationToken);
            throw new UnauthorizedAccessException("Only platform administrators may change the security access policy.");
        }
        if (started is SecurityAuditWriteResult.AlreadyRecorded)
        {
            SecurityAccessPolicyState? current = await policyStore.GetStateAsync(cancellationToken);
            if (current is not null && current.Version == expectedVersion + 1 && IsSameAsRetriedChange(current.Policy, applyChanges))
            {
                ISecurityAuditOutcomeUpdater outcomeUpdater = OutcomeUpdater();
                string completedKey = operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix;
                SecurityAuditWriteResult retried = await outcomeUpdater.UpdateAsync(
                    requestContext.TenantId!,
                    completedKey,
                    SecurityAuditOutcome.Succeeded,
                    SecurityAuditReasonCatalog.Version(current.Version),
                    cancellationToken);
                if (retried is SecurityAuditWriteResult.Recorded)
                    return current;
                if (retried is SecurityAuditWriteResult.AlreadyRecorded &&
                    await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, completedKey, cancellationToken) is SecurityAuditOutcome.Succeeded)
                    return current;
            }

            throw new InvalidOperationException("The security policy operation is already in progress or its outcome is already recorded.");
        }

        SecurityAuditWriteResult pending = await WriteAuditAsync(
            SecurityAuditOutcome.Pending,
            operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix,
            AuthenticationSecurityAuditEvents.AwaitingMutationOutcome,
            cancellationToken);
        if (pending is SecurityAuditWriteResult.AlreadyRecorded)
            throw new InvalidOperationException("The security policy completion outcome is already recorded.");

        SecurityAccessPolicyState saved;
        try
        {
            SecurityAccessPolicyState current = await policyStore.GetStateAsync(cancellationToken);
            SecurityAccessPolicy requested = applyChanges(current.Policy);
            requested.ValidateConfiguration();
            if (validateProfileConstraints)
                ValidateProfileConstraints(requested);
            saved = await sessionRegistry.SavePolicyAndRevokeInvalidatedSessionsAsync(requested, expectedVersion, cancellationToken);
        }
        catch (SecurityAccessPolicyConcurrencyException exception)
        {
            await CompletePendingAuditBestEffortAsync(
                operationKey,
                AuthenticationSecurityAuditEvents.SecurityAccessPolicyConcurrencyConflictReason,
                exception,
                CancellationToken.None);
            throw;
        }
        catch (ArgumentException exception)
        {
            await CompletePendingAuditBestEffortAsync(
                operationKey,
                AuthenticationSecurityAuditEvents.SecurityAccessPolicyValidationFailureReason,
                exception,
                CancellationToken.None);
            throw;
        }
        catch (InvalidOperationException exception)
        {
            await CompletePendingAuditBestEffortAsync(
                operationKey,
                AuthenticationSecurityAuditEvents.SecurityAccessPolicyValidationFailureReason,
                exception,
                CancellationToken.None);
            throw;
        }
        catch (SecurityAccessPolicyPersistenceException exception)
        {
            await CompletePendingAuditBestEffortAsync(
                operationKey,
                AuthenticationSecurityAuditEvents.SecurityAccessPolicyPersistenceFailureReason,
                exception,
                CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (exception is DbException or TimeoutException)
        {
            await CompletePendingAuditBestEffortAsync(
                operationKey,
                AuthenticationSecurityAuditEvents.SecurityAccessPolicyPersistenceFailureReason,
                exception,
                CancellationToken.None);
            throw new SecurityAccessPolicyPersistenceException(
                "The security access policy store is unavailable.",
                exception);
        }
        try
        {
            ISecurityAuditOutcomeUpdater outcomeUpdater = OutcomeUpdater();
            SecurityAuditWriteResult completed = await outcomeUpdater.UpdateAsync(
                requestContext.TenantId!,
                operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix,
                SecurityAuditOutcome.Succeeded,
                SecurityAuditReasonCatalog.Version(saved.Version),
                cancellationToken);
            if (completed is SecurityAuditWriteResult.AlreadyRecorded &&
                await outcomeUpdater.GetOutcomeAsync(
                    requestContext.TenantId!,
                    operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix,
                    cancellationToken) is not SecurityAuditOutcome.Succeeded)
            {
                throw new InvalidOperationException("The committed security policy mutation has no confirmed successful audit outcome.");
            }
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the committed policy mutation when its audit completion cannot be stored.
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            exception.Data["policy_mutation_committed"] = true;
            throw;
        }
        return saved;
    }

    private bool IsSameAsRetriedChange(SecurityAccessPolicy current, Func<SecurityAccessPolicy, SecurityAccessPolicy> applyChanges)
    {
        try
        {
            return current.EquivalentTo(applyChanges(current));
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void ValidateProfileConstraints(SecurityAccessPolicy policy)
    {
        AuthenticationProfilePolicy effectiveProfile = profilePolicy ?? AuthenticationProfilePolicy.Default;
        if (policy.EnabledAuthenticationMechanisms.Count == 0 || policy.PermittedPreAuthenticationActions.Count == 0)
            throw new ArgumentException("At least one authentication mechanism and pre-authentication action must remain enabled.");
        if (!policy.EnabledAuthenticationMechanisms.IsSubsetOf(effectiveProfile.EnabledAuthenticationMechanisms) ||
            !policy.PermittedPreAuthenticationActions.IsSubsetOf(effectiveProfile.PermittedPreAuthenticationActions) ||
            !effectiveProfile.RequiredAuthenticationMechanisms.IsSubsetOf(policy.EnabledAuthenticationMechanisms) ||
            !effectiveProfile.RequiredPreAuthenticationActions.IsSubsetOf(policy.PermittedPreAuthenticationActions))
            throw new ArgumentException("The authentication policy must preserve required choices and remain within the active deployment profile.");
    }

    private static TEnum ParsePolicyEnum<TEnum>(string value)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out TEnum parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException($"The configured {typeof(TEnum).Name} value is not supported.");

    private static IReadOnlySet<TEnum> ParsePolicyEnumSet<TEnum>(IReadOnlyList<string> values)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(values);
        HashSet<TEnum> parsed = [];
        foreach (string value in values)
        {
            TEnum item = ParsePolicyEnum<TEnum>(value);
            if (!parsed.Add(item))
                throw new ArgumentException($"The configured {typeof(TEnum).Name} values must not contain duplicates.");
        }
        if (parsed.Count == 0)
            throw new ArgumentException($"At least one {typeof(TEnum).Name} value must be enabled.");
        return parsed;
    }

    private async Task CompletePendingAuditBestEffortAsync(
        string operationKey,
        string reason,
        Exception originalException,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await OutcomeUpdater().UpdateAsync(
                requestContext.TenantId!,
                operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix,
                SecurityAuditOutcome.Denied,
                reason,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            originalException.Data["terminal_audit_cancellation"] = true;
        }
        catch (Exception auditException)
        {
            originalException.Data["terminal_audit_failure"] = auditException.GetType().Name;
        }

        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = AuthenticationSecurityAuditEvents.SecurityFailure,
                Outcome = SecurityAuditOutcome.Failed,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = requestContext.TenantId!,
                Resource = AuthenticationSecurityAuditEvents.SecurityAccessPolicyResource,
                Operation = AuthenticationSecurityAuditEvents.SecurityFailureOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = operationKey + AuthenticationSecurityAuditEvents.SecurityFailureKeySuffix
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            originalException.Data["security_failure_audit_cancellation"] = true;
        }
        catch (Exception auditException)
        {
            originalException.Data["security_failure_audit_failure"] = auditException.GetType().Name;
        }
    }

    private async Task<SecurityAuditWriteResult> WriteAuditAsync(string outcome, string idempotencyKey, string? reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            throw new InvalidOperationException("A tenant context is required for security auditing.");

        try
        {
            return await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = AuthenticationSecurityAuditEvents.SecurityAccessPolicy,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = requestContext.TenantId,
                Resource = AuthenticationSecurityAuditEvents.SecurityAccessPolicyResource,
                Operation = AuthenticationSecurityAuditEvents.SecurityAccessPolicyOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = idempotencyKey
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the policy mutation when its audit write cannot be stored.
            return SecurityAuditWriteResult.Recorded;
        }
    }

    private ISecurityAuditOutcomeUpdater OutcomeUpdater() => securityAuditWriter as ISecurityAuditOutcomeUpdater
        ?? throw new InvalidOperationException("The security audit store cannot complete pending outcomes.");
}
