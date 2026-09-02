using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Security;
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
        bool authorized = requestContext.User?.IsInRole(IdentityDefinitions.PlatformAdmin) is true;
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
}

public sealed class ChangeSecurityAccessPolicyHandler(
    IAuthenticationSecurityPolicyStore policyStore,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext,
    IAuthenticationSessionRegistry sessionRegistry)
{
    public async Task<SecurityAccessPolicyState> Handle(ChangeSecurityAccessPolicyCommand command, CancellationToken cancellationToken)
    {
        bool authorized = requestContext.User?.IsInRole(IdentityDefinitions.PlatformAdmin) is true;
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
            if (current is not null && current.Version == command.ExpectedVersion + 1 && current.Policy.EquivalentTo(command.Policy))
            {
                ISecurityAuditOutcomeUpdater outcomeUpdater = OutcomeUpdater();
                string completedKey = operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix;
                SecurityAuditWriteResult retried = await outcomeUpdater.UpdateAsync(
                    requestContext.TenantId!,
                    completedKey,
                    SecurityAuditOutcome.Succeeded,
                    $"version:{current.Version}",
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
            command.Policy.ValidateConfiguration();
            await sessionRegistry.RevokeSessionsInvalidatedByPolicyAsync(command.Policy, cancellationToken);
            saved = await policyStore.SaveAsync(command.Policy, command.ExpectedVersion, cancellationToken);
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
        try
        {
            ISecurityAuditOutcomeUpdater outcomeUpdater = OutcomeUpdater();
            SecurityAuditWriteResult completed = await outcomeUpdater.UpdateAsync(
                requestContext.TenantId!,
                operationKey + AuthenticationSecurityAuditEvents.SecurityAccessPolicyCompletedSuffix,
                SecurityAuditOutcome.Succeeded,
                $"version:{saved.Version}",
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            exception.Data["policy_mutation_committed"] = true;
            throw;
        }
        return saved;
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
    }

    private async Task<SecurityAuditWriteResult> WriteAuditAsync(string outcome, string idempotencyKey, string? reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            throw new InvalidOperationException("A tenant context is required for security auditing.");

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

    private ISecurityAuditOutcomeUpdater OutcomeUpdater() => securityAuditWriter as ISecurityAuditOutcomeUpdater
        ?? throw new InvalidOperationException("The security audit store cannot complete pending outcomes.");
}
