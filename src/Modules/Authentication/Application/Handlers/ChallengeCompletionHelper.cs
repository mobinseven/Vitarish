using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

internal static class ChallengeCompletionHelper
{
    public static async Task<AuthenticationResult<LoginResult>> CompleteChallengeAsync(
        string challengeId,
        string code,
        string? returnUrl,
        AuthenticationChallengePurpose expectedPurpose,
        IAuthenticationChallengeStore store,
        IAuthenticationCodeVerifier verifier,
        IAuthenticationSignInService signInService,
        IAuthenticationRedirectResolver redirects,
        CancellationToken cancellationToken,
        IAuthenticationAttemptTracker attemptTracker,
        SecurityAccessPolicy securityPolicy,
        IAuthenticationAttemptKeyFactory attemptKeyFactory,
        IInitialPlatformAdministratorGrant? initialAdministratorGrant = null,
        IUserAccountUpdater? contactConfirmer = null)
    {
        if (!AuthenticationInputValidator.HasText(challengeId) || !AuthenticationInputValidator.HasText(code))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Challenge ID and code are required."));

        AuthenticationChallengeId id = AuthenticationChallengeId.Create(challengeId);
        AuthenticationChallenge? challenge = await store.GetAsync(id, cancellationToken);

        if (challenge is null)
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid verification code."));

        IReadOnlyList<string> attemptKeys = attemptKeyFactory.ChallengeCandidates(expectedPurpose, challenge.UserId);
        if (await IsDeniedAsync(attemptKeys, attemptTracker, cancellationToken))
            return new AuthenticationResult<LoginResult>.Failure(new(
                securityPolicy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? AuthenticationFailureReason.LockedOut : AuthenticationFailureReason.TooManyAttempts,
                securityPolicy.FailedAttemptResponse is FailedAttemptResponse.Lockout ? "Authentication is temporarily locked out." : "Too many authentication attempts."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureReason? validationError = challenge.ValidateAttempt(now, expectedPurpose);
        if (validationError is not null)
        {
            AuthenticationFailureReason reason = ChallengeFailureReason(challenge, validationError.Value, securityPolicy.FailedAttemptResponse);
            return new AuthenticationResult<LoginResult>.Failure(new(reason, GetErrorMessage(reason)));
        }

        if (!await store.TryReserveAttemptAsync(challenge, cancellationToken))
            return new AuthenticationResult<LoginResult>.Failure(new(
                ChallengeFailureReason(challenge, AuthenticationFailureReason.ChallengeAttemptsExhausted, securityPolicy.FailedAttemptResponse),
                "Too many failed attempts."));

        string normalizedCode = NormalizeVerificationCode(code);
        bool verified;
        try
        {
            verified = await verifier.VerifyAsync(challenge.UserId, challenge.TokenPurpose, normalizedCode, cancellationToken);
        }
        catch
        {
            await store.ReleaseAttemptAsync(challenge, CancellationToken.None);
            throw;
        }

        if (!verified)
        {
            int failedAttempts;
            try
            {
                failedAttempts = await store.TryRegisterFailureAsync(challenge, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await store.ReleaseAttemptAsync(challenge, CancellationToken.None);
                throw;
            }
            int globalFailures = 0;
            foreach (string attemptKey in attemptKeys)
            {
                globalFailures = Math.Max(globalFailures, await attemptTracker.RegisterFailureAsync(
                    attemptKey,
                    securityPolicy.FailedAttemptThreshold,
                    securityPolicy.FailedAttemptDenialDuration,
                    cancellationToken));
            }
            if (globalFailures >= securityPolicy.FailedAttemptThreshold)
                return new AuthenticationResult<LoginResult>.Failure(new(
                    MapFailureReason(AuthenticationFailureReason.ChallengeAttemptsExhausted, securityPolicy.FailedAttemptResponse),
                    GetErrorMessage(MapFailureReason(AuthenticationFailureReason.ChallengeAttemptsExhausted, securityPolicy.FailedAttemptResponse))));
            if (failedAttempts <= 0 || failedAttempts >= challenge.MaximumAttempts)
                return new AuthenticationResult<LoginResult>.Failure(new(
                    ChallengeFailureReason(challenge, AuthenticationFailureReason.ChallengeAttemptsExhausted, securityPolicy.FailedAttemptResponse),
                    "Too many failed attempts."));

            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidVerificationCode, "Invalid verification code."));
        }

        bool consumed;
        try
        {
            consumed = await store.TryConsumeAsync(challenge, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await store.ReleaseAttemptAsync(challenge, CancellationToken.None);
            throw;
        }
        if (!consumed)
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ChallengeAlreadyUsed, "Challenge has already been used."));

        if (challenge.Status == AuthenticationChallengeStatus.Pending)
            challenge.Complete(now);

        // Contact-code sign-ins prove control of the contact that names the account, so
        // they confirm it and may claim the initial administrator role. Both happen
        // before the session starts so the session reflects them.
        if (contactConfirmer is not null)
            await contactConfirmer.ConfirmContactAsync(challenge.UserId, challenge.Destination, cancellationToken);
        if (initialAdministratorGrant is not null)
            await initialAdministratorGrant.GrantIfUnclaimedAsync(challenge.UserId, cancellationToken);

        AuthenticationSessionSignInResult session = await signInService.SignInAsync(challenge.UserId, cancellationToken);
        if (session is not null && !session.Succeeded)
            return new AuthenticationResult<LoginResult>.Failure(session.Failure!);

        foreach (string attemptKey in attemptKeys)
            await attemptTracker.ResetAsync(attemptKey, cancellationToken);

        string resolvedReturnUrl = await redirects.ResolveAsync(returnUrl, challenge.RequiresProfileCompletion, challenge.UserId, cancellationToken);
        return new AuthenticationResult<LoginResult>.Success(new(challenge.UserId, resolvedReturnUrl, challenge.RequiresProfileCompletion));
    }

    private static string NormalizeVerificationCode(string code)
    {
        if (code.Length > 128)
            return code;

        Span<char> normalized = stackalloc char[code.Length];
        for (int index = 0; index < code.Length; index++)
        {
            normalized[index] = code[index] switch
            {
                >= '۰' and <= '۹' => (char)('0' + code[index] - '۰'),
                >= '٠' and <= '٩' => (char)('0' + code[index] - '٠'),
                _ => code[index]
            };
        }

        return new string(normalized);
    }

    // A used-up second-factor challenge only means "sign in again"; reporting it
    // as a lockout would be false, and the account-wide threshold is checked above.
    private static AuthenticationFailureReason ChallengeFailureReason(AuthenticationChallenge challenge, AuthenticationFailureReason reason, FailedAttemptResponse? response) =>
        challenge.IsSecondFactor && reason is AuthenticationFailureReason.ChallengeAttemptsExhausted
            ? reason
            : MapFailureReason(reason, response);

    private static AuthenticationFailureReason MapFailureReason(AuthenticationFailureReason reason, FailedAttemptResponse? response) =>
        reason is AuthenticationFailureReason.ChallengeAttemptsExhausted && response is not null
            ? response is FailedAttemptResponse.Lockout
                ? AuthenticationFailureReason.LockedOut
                : AuthenticationFailureReason.TooManyAttempts
            : reason;

    private static async Task<bool> IsDeniedAsync(
        IReadOnlyList<string> keys,
        IAuthenticationAttemptTracker attemptTracker,
        CancellationToken cancellationToken)
    {
        foreach (string key in keys)
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return true;
        return false;
    }

    private static string GetErrorMessage(AuthenticationFailureReason reason) => reason switch
    {
        AuthenticationFailureReason.ChallengeNotFound => "Challenge not found.",
        AuthenticationFailureReason.ChallengeExpired => "Challenge has expired.",
        AuthenticationFailureReason.ChallengeAlreadyUsed => "Challenge has already been used.",
        AuthenticationFailureReason.ChallengeAttemptsExhausted => "Too many failed attempts.",
        AuthenticationFailureReason.LockedOut => "Authentication is temporarily locked out.",
        AuthenticationFailureReason.TooManyAttempts => "Too many authentication attempts.",
        AuthenticationFailureReason.InvalidVerificationCode => "Invalid verification code.",
        AuthenticationFailureReason.InvalidInput => "Invalid input.",
        _ => "Verification failed."
    };
}
