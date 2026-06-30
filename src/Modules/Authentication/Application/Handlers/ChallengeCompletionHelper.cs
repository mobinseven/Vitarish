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

        string normalizedCode = NormalizeVerificationCode(code);
        bool verified = await verifier.VerifyAsync(challenge.UserId, challenge.TokenPurpose, normalizedCode, cancellationToken);

        if (!verified)
        {
            challenge.RegisterFailure(now);
            await store.ReplaceAsync(challenge, cancellationToken);

            if (challenge.Status == AuthenticationChallengeStatus.Exhausted)
                return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts."));

            return new AuthenticationResult<LoginResult>.Failure(new(AuthenticationFailureReason.InvalidVerificationCode, "Invalid verification code."));
        }

        challenge.Complete(now);
        await store.ReplaceAsync(challenge, cancellationToken);

        await signInService.SignInAsync(challenge.UserId, false, cancellationToken);

        string resolvedReturnUrl = await redirects.ResolveAsync(returnUrl, false, cancellationToken);
        return new AuthenticationResult<LoginResult>.Success(new(challenge.UserId, resolvedReturnUrl, false));
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
