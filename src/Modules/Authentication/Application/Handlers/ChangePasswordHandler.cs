using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class ChangePasswordHandler(
    IUserAccountUpdater accountUpdater,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider)
{
    public async Task<AuthenticationResult<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserId))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "User ID is required."));

        if (!AuthenticationInputValidator.HasText(command.CurrentPassword))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "Current password is required."));

        if (!AuthenticationInputValidator.HasText(command.NewPassword))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "New password is required."));

        if (!string.Equals(requestContext.UserId, command.UserId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(requestContext.TenantId))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.Unauthorized, "The account does not belong to the current authenticated context."));

        string operationKey = command.IdempotencyKey ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        string startedKey = operationKey + AuthenticationSecurityAuditEvents.StartedKeySuffix;
        string completedKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix;
        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        IReadOnlyList<PasswordPolicyViolation> policyViolations = securityPolicy.PasswordPolicy.Validate(command.NewPassword);
        if (policyViolations.Count > 0)
        {
            try
            {
                _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                    command.UserId,
                    SecurityAuditOutcome.Denied,
                    completedKey,
                    AuthenticationSecurityAuditEvents.FailurePasswordPolicyRejected), cancellationToken);
            }
            catch (SecurityAuditStoreException)
            {
                return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
            }
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "New password does not satisfy the active password policy."));
        }

        ISecurityAuditOutcomeUpdater outcomeUpdater;
        try
        {
            outcomeUpdater = securityAuditWriter as ISecurityAuditOutcomeUpdater
                ?? throw new InvalidOperationException("The security audit store cannot complete pending outcomes.");
        }
        catch (InvalidOperationException)
        {
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        try
        {
            SecurityAuditWriteResult started = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                command.UserId,
                SecurityAuditOutcome.Started,
                startedKey,
                null), cancellationToken);
            if (started is SecurityAuditWriteResult.AlreadyRecorded)
            {
                try
                {
                    string? existingOutcome = await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, completedKey, cancellationToken);
                    if (existingOutcome is SecurityAuditOutcome.Succeeded)
                    {
                        if (await accountUpdater.PasswordMatchesAsync(command.UserId, command.NewPassword, cancellationToken))
                            return new AuthenticationResult<bool>.Success(true);

                        return new AuthenticationResult<bool>.Failure(new(
                            AuthenticationFailureReason.InvalidInput,
                            "The idempotency key was already used for a different password change."));
                    }

                    if (existingOutcome is SecurityAuditOutcome.Pending &&
                        await accountUpdater.PasswordMatchesAsync(command.UserId, command.NewPassword, cancellationToken))
                    {
                        SecurityAuditWriteResult reconciled = await outcomeUpdater.UpdateAsync(
                            requestContext.TenantId!, completedKey, SecurityAuditOutcome.Succeeded, null, cancellationToken);
                        if (reconciled is SecurityAuditWriteResult.Recorded or SecurityAuditWriteResult.AlreadyRecorded)
                        {
                            string? reconciledOutcome = await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, completedKey, cancellationToken);
                            if (reconciledOutcome is SecurityAuditOutcome.Succeeded)
                                return new AuthenticationResult<bool>.Success(true);
                        }
                    }
                }
                catch (SecurityAuditStoreException)
                {
                    return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
                }

                return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureDuplicateInProgress));
            }
        }
        catch (SecurityAuditStoreException)
        {
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        SecurityAuditWriteResult pending;
        try
        {
            pending = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                command.UserId,
                SecurityAuditOutcome.Pending,
                completedKey,
                "awaiting_mutation_outcome"), cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }
        if (pending is SecurityAuditWriteResult.AlreadyRecorded)
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureDuplicateInProgress));

        bool hasPassword = await accountUpdater.UserHasPasswordAsync(command.UserId, cancellationToken);
        PasswordChangeOutcome updated;
        if (hasPassword)
        {
            updated = await accountUpdater.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
        }
        else
        {
            updated = await accountUpdater.ChangePasswordAsync(command.UserId, string.Empty, command.NewPassword, cancellationToken);
        }

        if (!updated.Succeeded)
        {
            AuthenticationFailureReason reason = updated.Status is PasswordChangeStatus.PolicyRejected
                ? AuthenticationFailureReason.InvalidInput
                : AuthenticationFailureReason.InvalidCredentials;
            string failureReason = updated.Status is PasswordChangeStatus.PolicyRejected
                ? AuthenticationSecurityAuditEvents.FailurePasswordPolicyRejected
                : AuthenticationSecurityAuditEvents.FailureMutationRejected;
            return await CompleteFailureAsync(command.UserId, completedKey, reason, failureReason, outcomeUpdater, cancellationToken);
        }

        try
        {
            SecurityAuditWriteResult completed = await outcomeUpdater.UpdateAsync(
                requestContext.TenantId!, completedKey, SecurityAuditOutcome.Succeeded, null, cancellationToken);
            if (completed is SecurityAuditWriteResult.AlreadyRecorded)
            {
                string? confirmedOutcome = await outcomeUpdater.GetOutcomeAsync(requestContext.TenantId!, completedKey, cancellationToken);
                if (confirmedOutcome is not SecurityAuditOutcome.Succeeded)
                    return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
            }
        }
        catch (SecurityAuditStoreException)
        {
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        return new AuthenticationResult<bool>.Success(true);
    }

    private async Task<AuthenticationResult<bool>> CompleteFailureAsync(
        string userId,
        string completedKey,
        AuthenticationFailureReason reason,
        string failureReason,
        ISecurityAuditOutcomeUpdater outcomeUpdater,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await outcomeUpdater.UpdateAsync(requestContext.TenantId!, completedKey, SecurityAuditOutcome.Failed, failureReason, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        return new AuthenticationResult<bool>.Failure(new(reason, reason is AuthenticationFailureReason.InvalidCredentials ? "Current password is incorrect." : "Failed to set password."));
    }

    private SecurityAuditEvent CreateAuditEvent(string userId, string outcome, string idempotencyKey, string? reason) => new()
    {
        EventName = AuthenticationSecurityAuditEvents.PasswordChange,
        Outcome = outcome,
        ActorId = requestContext.UserId,
        SubjectId = userId,
        TenantId = requestContext.TenantId!,
        Resource = AuthenticationSecurityAuditEvents.ResourcePrefix + userId,
        Operation = AuthenticationSecurityAuditEvents.Operation,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
        SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
        RedactedReason = reason,
        IdempotencyKey = idempotencyKey
    };
}
