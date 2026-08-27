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
        CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(challengeId) || !AuthenticationInputValidator.HasText(code))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidInput, "Challenge ID and code are required."));

        AuthenticationChallengeId id = AuthenticationChallengeId.Create(challengeId);
        AuthenticationChallenge? challenge = await store.GetAsync(id, cancellationToken);

        if (challenge is null)
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid verification code."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureReason? validationError = challenge.ValidateAttempt(now, expectedPurpose);
        if (validationError is not null)
            return new AuthenticationResult<LoginResult>.Failure(new(validationError.Value, GetErrorMessage(validationError.Value)));

        if (!await store.TryReserveAttemptAsync(challenge, cancellationToken))
            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts."));

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
            if (failedAttempts <= 0 || failedAttempts >= challenge.MaximumAttempts)
                return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts."));

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

        await signInService.SignInAsync(challenge.UserId, challenge.RememberMe, cancellationToken);

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

    private static string GetErrorMessage(AuthenticationFailureReason reason) => reason switch
    {
        AuthenticationFailureReason.ChallengeNotFound => "Challenge not found.",
        AuthenticationFailureReason.ChallengeExpired => "Challenge has expired.",
        AuthenticationFailureReason.ChallengeAlreadyUsed => "Challenge has already been used.",
        AuthenticationFailureReason.ChallengeAttemptsExhausted => "Too many failed attempts.",
        AuthenticationFailureReason.InvalidVerificationCode => "Invalid verification code.",
        AuthenticationFailureReason.InvalidInput => "Invalid input.",
        _ => "Verification failed."
    };
}
