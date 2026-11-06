using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class SetFirstPasswordHandler(
    IUserAccountUpdater accountUpdater,
    IAuthenticationSessionRegistry sessions,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationUtcClock clock,
    IRequestContext requestContext,
    AuthenticationAttemptAuditor auditor)
{
    public async Task<AuthenticationResult<bool>> Handle(SetFirstPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserId) || !AuthenticationInputValidator.HasText(command.NewPassword))
        {
            await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
            return Failure(AuthenticationFailureReason.InvalidInput, "A new password is required.");
        }

        if (!string.Equals(requestContext.UserId, command.UserId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.Unauthorized, cancellationToken);
            return Failure(AuthenticationFailureReason.Unauthorized, "The account does not belong to the current authenticated context.");
        }

        DateTime? authenticatedAt = await sessions.GetCurrentAuthenticationAtAsync(command.UserId, cancellationToken);
        if (!RecentAuthenticationPolicy.IsRecent(authenticatedAt, clock.UtcNow))
        {
            await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.Unauthorized, cancellationToken);
            return Failure(AuthenticationFailureReason.Unauthorized, "A recent sign-in is required to set a password.");
        }

        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (securityPolicy.PasswordPolicy.Validate(command.NewPassword).Count > 0)
        {
            await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailurePasswordPolicyRejected, cancellationToken);
            return Failure(AuthenticationFailureReason.InvalidInput, "New password does not satisfy the active password policy.");
        }

        if (await accountUpdater.UserHasPasswordAsync(command.UserId, cancellationToken))
        {
            await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
            return Failure(AuthenticationFailureReason.Conflict, "A password is already set. Use the password change action.");
        }

        PasswordChangeOutcome outcome = await accountUpdater.AddFirstPasswordAsync(command.UserId, command.NewPassword, cancellationToken);
        if (!outcome.Succeeded)
        {
            if (outcome.Status is PasswordChangeStatus.PolicyRejected)
            {
                await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailurePasswordPolicyRejected, cancellationToken);
                return Failure(AuthenticationFailureReason.InvalidInput, "New password does not satisfy the active password policy.");
            }

            if (outcome.Status is PasswordChangeStatus.AlreadyHasPassword)
            {
                await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
                return Failure(AuthenticationFailureReason.Conflict, "A password was set before this request completed.");
            }

            await AuditAsync(command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
            return Failure(AuthenticationFailureReason.ProviderFailure, "The password could not be set.");
        }

        await AuditAsync(command.UserId, succeeded: true, failureReason: null, cancellationToken);
        return new AuthenticationResult<bool>.Success(true);
    }

    private Task AuditAsync(string userId, bool succeeded, string? failureReason, CancellationToken cancellationToken) =>
        auditor.RecordAsync(
            AuthenticationSecurityAuditEvents.PasswordChange,
            AuthenticationSecurityAuditEvents.FirstPasswordSetOperation,
            userId,
            succeeded ? null : failureReason,
            cancellationToken);

    private static AuthenticationResult<bool> Failure(AuthenticationFailureReason reason, string message) =>
        new AuthenticationResult<bool>.Failure(new(reason, message));
}
