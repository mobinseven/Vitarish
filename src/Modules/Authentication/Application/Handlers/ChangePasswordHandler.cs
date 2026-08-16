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
    IRequestContext requestContext)
{
    public async Task<AuthenticationResult<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserId))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "User ID is required."));

        if (!AuthenticationInputValidator.HasText(command.CurrentPassword))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "Current password is required."));

        if (!AuthenticationInputValidator.HasText(command.NewPassword))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "New password is required."));

        if (command.NewPassword.Length < 6)
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "New password must be at least 6 characters."));

        if (!string.Equals(requestContext.UserId, command.UserId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(requestContext.TenantId))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.Unauthorized, "The account does not belong to the current authenticated context."));

        string operationKey = command.IdempotencyKey ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        string startedKey = operationKey + AuthenticationSecurityAuditEvents.StartedKeySuffix;
        string completedKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix;
        try
        {
            SecurityAuditWriteResult started = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                command.UserId,
                SecurityAuditOutcome.Started,
                startedKey,
                null), cancellationToken);
            if (started is SecurityAuditWriteResult.AlreadyRecorded)
                return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureDuplicateInProgress));
        }
        catch (SecurityAuditStoreException)
        {
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureAuditUnavailable));
        }

        bool hasPassword = await accountUpdater.UserHasPasswordAsync(command.UserId, cancellationToken);
        bool updated;
        if (hasPassword)
        {
            updated = await accountUpdater.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
        }
        else
        {
            updated = await accountUpdater.ChangePasswordAsync(command.UserId, string.Empty, command.NewPassword, cancellationToken);
        }

        if (!updated)
        {
            return await CompleteFailureAsync(command.UserId, completedKey, AuthenticationFailureReason.InvalidCredentials, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
        }

        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(command.UserId, SecurityAuditOutcome.Succeeded, completedKey, null), cancellationToken);
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
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(userId, SecurityAuditOutcome.Failed, completedKey, failureReason), cancellationToken);
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
