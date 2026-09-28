using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompletePasswordRecoveryHandler(
    IAuthenticationChallengeStore challengeStore,
    IAuthenticationCodeVerifier codeVerifier,
    IUserAccountUpdater accountUpdater,
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    IAuthenticationSecurityPolicyProvider securityPolicyProvider,
    TimeProvider timeProvider)
{
    public async Task<AuthenticationResult<bool>> Handle(CompletePasswordRecoveryCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.ChallengeId) ||
            !AuthenticationInputValidator.HasText(command.Code) ||
            !AuthenticationInputValidator.HasText(command.NewPassword))
            return Failure(AuthenticationFailureReason.InvalidInput, "Challenge, code, and new password are required.");

        AuthenticationChallenge? challenge = await challengeStore.GetAsync(AuthenticationChallengeId.Create(command.ChallengeId), cancellationToken);
        if (challenge is null || !IsRecoveryPurpose(challenge.Purpose))
            return Failure(AuthenticationFailureReason.InvalidCredentials, "Invalid verification code.");

        SecurityAccessPolicy securityPolicy = await securityPolicyProvider.GetAsync(cancellationToken);
        AuthenticationMethod method = AuthenticationAttemptKeyMethods.MethodFor(challenge.Purpose);
        IReadOnlyList<string> attemptKeys = attemptKeyFactory.ChallengeCandidates(method, challenge.Id.Value)
            .Concat(attemptKeyFactory.SubjectCandidates(method, challenge.UserId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (await IsDeniedAsync(attemptKeys, cancellationToken))
            return AttemptDenied(securityPolicy);

        AuthenticationFailureReason? validationError = challenge.ValidateAttempt(timeProvider.GetUtcNow(), challenge.Purpose);
        if (validationError is not null)
            return Failure(validationError.Value, GetChallengeErrorMessage(validationError.Value));

        if (!await challengeStore.TryReserveAttemptAsync(challenge, cancellationToken))
            return Failure(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts.");

        bool verified;
        try
        {
            verified = await codeVerifier.VerifyAsync(challenge.UserId, challenge.TokenPurpose,
                AuthenticationInputValidator.NormalizeVerificationCode(command.Code), cancellationToken);
        }
        catch
        {
            await challengeStore.ReleaseAttemptAsync(challenge, CancellationToken.None);
            throw;
        }

        if (!verified)
        {
            int failedAttempts;
            try
            {
                failedAttempts = await challengeStore.TryRegisterFailureAsync(challenge, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await challengeStore.ReleaseAttemptAsync(challenge, CancellationToken.None);
                throw;
            }

            if (failedAttempts <= 0)
                return Failure(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts.");

            bool denied = false;
            foreach (string key in attemptKeys)
                denied |= await attemptTracker.RegisterFailureAsync(key, securityPolicy.FailedAttemptThreshold,
                    securityPolicy.FailedAttemptDenialDuration, cancellationToken) >= securityPolicy.FailedAttemptThreshold;

            return denied || failedAttempts >= challenge.MaximumAttempts
                ? AttemptDenied(securityPolicy)
                : Failure(AuthenticationFailureReason.InvalidVerificationCode, "Invalid verification code.");
        }

        IReadOnlyList<PasswordPolicyViolation> violations = securityPolicy.PasswordPolicy.Validate(command.NewPassword);
        if (violations.Count > 0)
        {
            await challengeStore.ReleaseAttemptAsync(challenge, CancellationToken.None);
            return Failure(AuthenticationFailureReason.InvalidInput, "New password does not satisfy the active password policy.");
        }

        if (!await challengeStore.TryConsumeAsync(challenge, cancellationToken))
            return Failure(AuthenticationFailureReason.ChallengeAlreadyUsed, "Challenge has already been used.");

        PasswordChangeOutcome update = await accountUpdater.ResetPasswordAsync(challenge.UserId, command.NewPassword, cancellationToken);
        if (!update.Succeeded)
        {
            AuthenticationFailureReason reason = update.Status is PasswordChangeStatus.PolicyRejected
                ? AuthenticationFailureReason.InvalidInput
                : AuthenticationFailureReason.ProviderFailure;
            return Failure(reason, reason is AuthenticationFailureReason.InvalidInput
                ? "New password does not satisfy the active password policy."
                : "Password recovery could not be completed.");
        }

        foreach (string key in attemptKeys)
            await attemptTracker.ResetAsync(key, cancellationToken);

        return new AuthenticationResult<bool>.Success(true);
    }

    private async Task<bool> IsDeniedAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        foreach (string key in keys)
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return true;
        return false;
    }

    private static bool IsRecoveryPurpose(AuthenticationChallengePurpose purpose) =>
        purpose is AuthenticationChallengePurpose.EmailPasswordRecovery or AuthenticationChallengePurpose.PhonePasswordRecovery;

    private static AuthenticationResult<bool> AttemptDenied(SecurityAccessPolicy policy) =>
        Failure(policy.FailedAttemptResponse is FailedAttemptResponse.Lockout
            ? AuthenticationFailureReason.LockedOut
            : AuthenticationFailureReason.TooManyAttempts,
            policy.FailedAttemptResponse is FailedAttemptResponse.Lockout
                ? "Authentication is temporarily locked out."
                : "Too many authentication attempts.");

    private static AuthenticationResult<bool> Failure(AuthenticationFailureReason reason, string message) =>
        new AuthenticationResult<bool>.Failure(new(reason, message));

    private static string GetChallengeErrorMessage(AuthenticationFailureReason reason) => reason switch
    {
        AuthenticationFailureReason.ChallengeExpired => "Challenge has expired.",
        AuthenticationFailureReason.ChallengeAlreadyUsed => "Challenge has already been used.",
        AuthenticationFailureReason.ChallengeAttemptsExhausted => "Too many failed attempts.",
        _ => "Invalid verification code."
    };
}
