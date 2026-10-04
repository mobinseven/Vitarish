using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class LoginWithPasswordHandler(
    IAuthenticationSignInService signInService,
    IAuthenticationRedirectResolver redirects,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext,
    AuthenticationProfilePolicy profilePolicy,
    IAuthenticationChallengeStore challengeStore,
    AuthenticationChallengePolicy challengePolicy,
    IAuthenticationUserDirectory userDirectory,
    IAuthenticationCodeGenerator codeGenerator,
    IPhoneAuthenticationCodeSender phoneSender,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    IAuthenticationAttemptKeyFactory attemptKeyFactory)
{
    public async Task<AuthenticationResult<LoginResult>> Handle(LoginWithPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserNameOrEmail) || !AuthenticationInputValidator.HasText(command.Password))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Login and password are required."));

        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "A tenant context is required for security auditing."));

        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        string operationKey = command.IdempotencyKey ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        string startedKey = operationKey + AuthenticationSecurityAuditEvents.StartedKeySuffix;
        string completedKey = operationKey + AuthenticationSecurityAuditEvents.CompletedKeySuffix;
        if (!securityPolicy.Allows(AuthenticationMethod.Password, PreAuthenticationAction.PasswordLogin))
        {
            try
            {
                _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                    SecurityAuditOutcome.Denied,
                    completedKey,
                    null,
                    AuthenticationSecurityAuditEvents.FailurePasswordLoginDisabled), cancellationToken);
            }
            catch (SecurityAuditStoreException)
            {
                // AU-8 preserves the authentication denial when its audit record cannot be stored.
            }
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Password authentication is not enabled."));
        }
        try
        {
            SecurityAuditWriteResult started = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                SecurityAuditOutcome.Started,
                startedKey,
                null,
                null), cancellationToken);
            if (started is SecurityAuditWriteResult.AlreadyRecorded)
                return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, AuthenticationSecurityAuditEvents.FailureDuplicateInProgress));
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the initiating authentication result when its audit record cannot be stored.
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        PasswordSignInResult signInResult = profilePolicy.UsePhoneOtpForTwoFactorAccounts
            ? await signInService.CheckPasswordSignInAsync(command.UserNameOrEmail, command.Password, cancellationToken)
            : await signInService.PasswordSignInAsync(command.UserNameOrEmail, command.Password, command.RememberMe, cancellationToken);

        AuthenticationResult<LoginResult> result = signInResult.Status switch
        {
            PasswordSignInStatus.Succeeded when profilePolicy.UsePhoneOtpForTwoFactorAccounts && signInResult.RequiresTwoFactor =>
                await IssuePhoneOtpChallengeAsync(signInResult, command.RememberMe, cancellationToken),
            PasswordSignInStatus.RequiresTwoFactor when profilePolicy.UsePhoneOtpForTwoFactorAccounts =>
                await IssuePhoneOtpChallengeAsync(signInResult, command.RememberMe, cancellationToken),
            PasswordSignInStatus.Succeeded when profilePolicy.UsePhoneOtpForTwoFactorAccounts =>
                await CompletePasswordOnlyLoginAsync(signInResult, command, cancellationToken),
            PasswordSignInStatus.Succeeded =>
                new AuthenticationResult<LoginResult>.Success(new(
                    signInResult.UserId!,
                    await redirects.ResolveAsync(command.ReturnUrl, signInResult.RequiresProfileCompletion, signInResult.UserId, cancellationToken),
                    signInResult.RequiresProfileCompletion)),
            PasswordSignInStatus.InvalidCredentials or PasswordSignInStatus.UserNotFound =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid credentials.")),
            PasswordSignInStatus.LockedOut =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.LockedOut, "Account is locked out.")),
            PasswordSignInStatus.NotAllowed =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Sign in is not allowed.")),
            PasswordSignInStatus.RequiresTwoFactor =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.AdditionalStepRequired, "Additional authentication required.")),
            PasswordSignInStatus.ProviderFailure =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Authentication provider failure.")),
            PasswordSignInStatus.TooManyAttempts =>
                new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.TooManyAttempts, "Too many authentication attempts.")),
            _ => new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid credentials."))
        };

        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateAuditEvent(
                result is AuthenticationResult<LoginResult>.Success ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                completedKey,
                signInResult.UserId,
                result is AuthenticationResult<LoginResult>.Failure ? AuthenticationSecurityAuditEvents.AuthenticationFailed : null), cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the authentication result and any issued challenge.
        }
        catch (OperationCanceledException)
        {
            await CancelIssuedChallengeAsync(result);
            await signInService.SignOutAsync(CancellationToken.None);
            throw;
        }

        return result;
    }

    private async Task CancelIssuedChallengeAsync(AuthenticationResult<LoginResult> result)
    {
        if (result is not AuthenticationResult<LoginResult>.Failure { Error.ChallengeId: { Length: > 0 } challengeId })
            return;

        AuthenticationChallengeId id = AuthenticationChallengeId.Create(challengeId);
        AuthenticationChallenge? challenge = await challengeStore.GetAsync(id, CancellationToken.None);
        if (challenge is not null)
            await challengeStore.TryConsumeAsync(challenge, CancellationToken.None);
    }

    private async Task<AuthenticationResult<LoginResult>> IssuePhoneOtpChallengeAsync(
        PasswordSignInResult signInResult,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        AuthenticationUserDescriptor? user = await userDirectory.FindByIdAsync(signInResult.UserId!, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PhoneNumber))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Additional authentication is not configured."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        if (!securityPolicy.Allows(AuthenticationMethod.Phone, PreAuthenticationAction.PasswordPlusPhoneOtp))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.NotAllowed, "Additional authentication is not enabled."));
        IReadOnlyList<string> attemptKeys = attemptKeyFactory.SubjectCandidates(AuthenticationMethod.Phone, user.UserId);
        foreach (string attemptKey in attemptKeys)
        {
            if (!await attemptTracker.IsDeniedAsync(attemptKey, cancellationToken))
                continue;
            return new AuthenticationResult<LoginResult>.Failure(AttemptDenied(securityPolicy));
        }
        AuthenticationChallengePolicy effectivePolicy = challengePolicy.WithMaximumAttempts(securityPolicy.FailedAttemptThreshold);
        AuthenticationChallenge challenge = effectivePolicy.CreatePasswordLoginOtpChallenge(
            user.UserId,
            user.PhoneNumber,
            now,
            rememberMe,
            signInResult.RequiresProfileCompletion);
        string code = await codeGenerator.GenerateAsync(user.UserId, challenge.TokenPurpose, cancellationToken);
        await challengeStore.SaveAsync(challenge, cancellationToken);

        try
        {
            await phoneSender.SendAsync(user.PhoneNumber, code, AuthenticationChallengePurpose.PasswordLoginOtp, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            challenge.Cancel();
            await challengeStore.ReplaceAsync(challenge, CancellationToken.None);
            throw;
        }
        catch
        {
            challenge.Cancel();
            await challengeStore.ReplaceAsync(challenge, CancellationToken.None);
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Additional authentication is temporarily unavailable."));
        }

        return new AuthenticationResult<LoginResult>.Failure(new(
            AuthenticationFailureReason.AdditionalStepRequired,
            "Additional authentication required.",
            challenge.Id.Value));
    }

    private static AuthenticationFailure AttemptDenied(SecurityAccessPolicy policy) =>
        new(policy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? AuthenticationFailureReason.LockedOut : AuthenticationFailureReason.TooManyAttempts,
            policy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? "Authentication is temporarily locked out." : "Too many authentication attempts.");

    private async Task<AuthenticationResult<LoginResult>> CompletePasswordOnlyLoginAsync(
        PasswordSignInResult signInResult,
        LoginWithPasswordCommand command,
        CancellationToken cancellationToken)
    {
        AuthenticationSessionSignInResult session = await signInService.SignInAsync(signInResult.UserId!, command.RememberMe, cancellationToken);
        if (session is not null && !session.Succeeded)
            return new AuthenticationResult<LoginResult>.Failure(session.Failure!);
        return new AuthenticationResult<LoginResult>.Success(new(
            signInResult.UserId!,
            await redirects.ResolveAsync(command.ReturnUrl, signInResult.RequiresProfileCompletion, signInResult.UserId, cancellationToken),
            signInResult.RequiresProfileCompletion));
    }

    private SecurityAuditEvent CreateAuditEvent(string outcome, string idempotencyKey, string? subjectId, string? reason) => new()
    {
        EventName = AuthenticationSecurityAuditEvents.PasswordLogin,
        Outcome = outcome,
        ActorId = requestContext.UserId,
        SubjectId = subjectId,
        TenantId = requestContext.TenantId!,
        Resource = AuthenticationSecurityAuditEvents.Resource,
        Operation = AuthenticationSecurityAuditEvents.PasswordLoginOperation,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
        SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
        RedactedReason = reason,
        IdempotencyKey = idempotencyKey
    };
}
