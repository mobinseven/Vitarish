using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Authentication.Application.Validation;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class ChangeTwoStepSignInHandler(
    IUserAccountUpdater accountUpdater,
    IAuthenticationUserDirectory users,
    IAuthenticationSessionRegistry sessions,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationUtcClock clock,
    IRequestContext requestContext,
    AuthenticationAttemptAuditor auditor)
{
    public async Task<AuthenticationResult<bool>> Handle(ChangeTwoStepSignInCommand command, CancellationToken cancellationToken)
    {
        string operation = command.Enabled
            ? AuthenticationSecurityAuditEvents.TwoStepSignInEnableOperation
            : AuthenticationSecurityAuditEvents.TwoStepSignInDisableOperation;

        if (!AuthenticationInputValidator.HasText(command.UserId) ||
            !string.Equals(requestContext.UserId, command.UserId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            await AuditAsync(operation, command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.Unauthorized, cancellationToken);
            return Failure(AuthenticationFailureReason.Unauthorized, "The account does not belong to the current authenticated context.");
        }

        DateTime? authenticatedAt = await sessions.GetCurrentAuthenticationAtAsync(command.UserId, cancellationToken);
        if (!RecentAuthenticationPolicy.IsRecent(authenticatedAt, clock.UtcNow))
        {
            await AuditAsync(operation, command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.Unauthorized, cancellationToken);
            return Failure(AuthenticationFailureReason.Unauthorized, "A recent sign-in is required to change two-step sign-in.");
        }

        AuthenticationUserDescriptor? user = await users.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            await AuditAsync(operation, command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
            return Failure(AuthenticationFailureReason.InvalidCredentials, "The account is unavailable.");
        }

        AuthenticationMethod? expectedChannel = null;
        if (command.Enabled)
        {
            if (!user.HasPassword)
            {
                await AuditAsync(operation, command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
                return Failure(AuthenticationFailureReason.NotAllowed, "Set a password before enabling two-step sign-in.");
            }

            PasswordLoginSecondFactorChannel? channel = PasswordLoginSecondFactorChannel.Select(user);
            if (channel is null)
            {
                await AuditAsync(operation, command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
                return Failure(AuthenticationFailureReason.NotAllowed, "Confirm a phone number or email before enabling two-step sign-in.");
            }

            SecurityAccessPolicy policy = await securityPolicyProvider.GetAsync(cancellationToken);
            if (!policy.Allows(channel.Method, channel.PolicyAction))
            {
                await AuditAsync(operation, command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
                return Failure(AuthenticationFailureReason.NotAllowed, "The current security policy does not allow the selected second-step channel.");
            }

            expectedChannel = channel.Method;
        }

        if (!await accountUpdater.SetTwoFactorEnabledAsync(
                command.UserId,
                command.Enabled,
                user.TwoFactorEnabled,
                expectedChannel,
                cancellationToken))
        {
            await AuditAsync(operation, command.UserId, succeeded: false, AuthenticationSecurityAuditEvents.FailureMutationRejected, cancellationToken);
            return Failure(AuthenticationFailureReason.Conflict, "The account security state changed. Reload and retry.");
        }

        await AuditAsync(operation, command.UserId, succeeded: true, failureReason: null, cancellationToken);
        return new AuthenticationResult<bool>.Success(true);
    }

    private Task AuditAsync(string operation, string userId, bool succeeded, string? failureReason, CancellationToken cancellationToken) =>
        auditor.RecordAsync(
            AuthenticationSecurityAuditEvents.SecurityAttributesChanged,
            operation,
            userId,
            succeeded ? null : failureReason,
            cancellationToken,
            SecurityAuditEventCatalog.SecurityAttributeCategory);

    private static AuthenticationResult<bool> Failure(AuthenticationFailureReason reason, string message) =>
        new AuthenticationResult<bool>.Failure(new(reason, message));
}
