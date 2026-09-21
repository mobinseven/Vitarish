using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class AuditingSignInManager(
    UserManager<User> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<User> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<AuditingSignInManager> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<User> confirmation,
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext)
    : SignInManager<User>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    private readonly ILogger<AuditingSignInManager> _logger = logger;

    public override Task<SignInResult> PasswordSignInAsync(User user, string password, bool isPersistent, bool lockoutOnFailure) =>
        ExecuteAsync(UsersSecurityAuditEvents.IdentityPasswordSignInUser, UsersSecurityAuditEvents.IdentityPasswordSignInUserOperation, UsersSecurityAuditEvents.IdentityPasswordSignInUserIdempotencySuffix,
            () => Task.FromResult<string?>(user.Id), () => base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure), false);

    public override Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure) =>
        ExecuteAsync(UsersSecurityAuditEvents.IdentityPasswordSignInName, UsersSecurityAuditEvents.IdentityPasswordSignInNameOperation, UsersSecurityAuditEvents.IdentityPasswordSignInNameIdempotencySuffix,
            () => TryFindByNameAsync(userName), () => base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure), false);

    public override Task<SignInResult> CheckPasswordSignInAsync(User user, string password, bool lockoutOnFailure) =>
        ExecuteAsync(UsersSecurityAuditEvents.IdentityPasswordCheck, UsersSecurityAuditEvents.IdentityPasswordCheckOperation, UsersSecurityAuditEvents.IdentityPasswordCheckIdempotencySuffix,
            () => Task.FromResult<string?>(user.Id), () => base.CheckPasswordSignInAsync(user, password, lockoutOnFailure), false);

    public override Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor) =>
        ExecuteAsync(UsersSecurityAuditEvents.IdentityExternalLogin, UsersSecurityAuditEvents.IdentityExternalLoginOperation, UsersSecurityAuditEvents.IdentityExternalLoginIdempotencySuffix,
            () => FindByLoginAsync(loginProvider, providerKey), () => base.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor), false);

    public override Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient) =>
        ExecuteAsync(UsersSecurityAuditEvents.IdentityAuthenticatorMfa, UsersSecurityAuditEvents.IdentityAuthenticatorMfaOperation, UsersSecurityAuditEvents.IdentityAuthenticatorMfaIdempotencySuffix,
            FindTwoFactorUserIdAsync, () => base.TwoFactorAuthenticatorSignInAsync(code, isPersistent, rememberClient), true);

    public override Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode) =>
        ExecuteAsync(UsersSecurityAuditEvents.IdentityRecoveryCodeMfa, UsersSecurityAuditEvents.IdentityRecoveryCodeMfaOperation, UsersSecurityAuditEvents.IdentityRecoveryCodeMfaIdempotencySuffix,
            FindTwoFactorUserIdAsync, () => base.TwoFactorRecoveryCodeSignInAsync(recoveryCode), true);

    public override Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent, bool rememberClient) =>
        ExecuteAsync(UsersSecurityAuditEvents.IdentityProviderMfa, UsersSecurityAuditEvents.IdentityProviderMfaOperation, UsersSecurityAuditEvents.IdentityProviderMfaIdempotencySuffix,
            FindTwoFactorUserIdAsync, () => base.TwoFactorSignInAsync(provider, code, isPersistent, rememberClient), true);

    private async Task<SignInResult> ExecuteAsync(string eventName, string operation, string idempotencySuffix, Func<Task<string?>> subjectIdFactory, Func<Task<SignInResult>> signIn, bool mfa)
    {
        SignInResult result;
        try
        {
            result = await signIn().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            string? subjectId = await TryGetSubjectIdAsync(subjectIdFactory).ConfigureAwait(false);
            await TryWriteAsync(eventName, operation, idempotencySuffix, subjectId, SecurityAuditOutcome.Failed, FailureReasonForException()).ConfigureAwait(false);
            throw;
        }

        string? resolvedSubjectId = await TryGetSubjectIdAsync(subjectIdFactory).ConfigureAwait(false);
        (string outcome, string? reason) = Outcome(result, mfa);
        await TryWriteAsync(eventName, operation, idempotencySuffix, resolvedSubjectId, outcome, reason).ConfigureAwait(false);
        return result;
    }

    private async Task TryWriteAsync(string eventName, string operation, string idempotencySuffix, string? subjectId, string outcome, string? reason)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            LogAuditUnavailable(eventName, UsersSecurityAuditEvents.IdentityTenantContextMissing);
            return;
        }

        string requestKey = requestContext.RequestId ?? requestContext.TraceId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                EventCategory = SecurityAuditEventCatalog.AuthenticationCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = subjectId,
                TenantId = requestContext.TenantId,
                Resource = UsersSecurityAuditEvents.IdentityResource,
                Operation = operation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestKey,
                SourceBoundary = UsersSecurityAuditEvents.IdentitySourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = requestKey + idempotencySuffix
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (SecurityAuditStoreException)
        {
            LogAuditUnavailable(eventName, UsersSecurityAuditEvents.IdentityAuditUnavailable);
        }
    }

    private void LogAuditUnavailable(string eventName, string reason) =>
        _logger.LogError("Security audit unavailable for Identity sign-in operation {EventName}; reason {Reason}.", eventName, reason);

    private async Task<string?> TryGetSubjectIdAsync(Func<Task<string?>> subjectIdFactory)
    {
        try
        {
            return await subjectIdFactory().ConfigureAwait(false);
        }
        catch (Exception)
        {
            _logger.LogDebug("Identity subject attribution was unavailable for a sign-in audit event.");
            return null;
        }
    }

    private async Task<string?> TryFindByNameAsync(string userName) =>
        string.IsNullOrWhiteSpace(userName) ? null : (await UserManager.FindByNameAsync(userName).ConfigureAwait(false))?.Id;

    private async Task<string?> FindByLoginAsync(string loginProvider, string providerKey) =>
        (await UserManager.FindByLoginAsync(loginProvider, providerKey).ConfigureAwait(false))?.Id;

    private async Task<string?> FindTwoFactorUserIdAsync() => (await GetTwoFactorAuthenticationUserAsync().ConfigureAwait(false))?.Id;

    internal static (string Outcome, string? Reason) Outcome(SignInResult result, bool mfa) =>
        result.RequiresTwoFactor
            ? (SecurityAuditOutcome.Pending, UsersSecurityAuditEvents.IdentitySecondFactorRequired)
            : result.Succeeded
                ? (SecurityAuditOutcome.Succeeded, null)
                : (SecurityAuditOutcome.Denied, FailureReasonForResult(mfa, result.IsLockedOut));

    private static string FailureReasonForException() => UsersSecurityAuditEvents.IdentityProviderFailure;

    private static string FailureReasonForResult(bool mfa, bool isLockedOut) =>
        isLockedOut
            ? UsersSecurityAuditEvents.IdentityLockout
            : mfa
                ? UsersSecurityAuditEvents.IdentityMfaFailure
                : UsersSecurityAuditEvents.IdentityAuthenticationFailed;
}
