using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class CompleteChangePhoneHandler(
    IAuthenticationChallengeStore store,
    IAuthenticationCodeVerifier verifier,
    IUserAccountUpdater accountUpdater)
{
    public async Task<AuthenticationResult<bool>> Handle(CompleteChangePhoneCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.ChallengeId) || !AuthenticationInputValidator.HasText(command.Code))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "Challenge ID and code are required."));

        AuthenticationChallengeId id = AuthenticationChallengeId.Create(command.ChallengeId);
        AuthenticationChallenge? challenge = await store.GetAsync(id, cancellationToken);

        if (challenge is null)
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Invalid verification code."));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureReason? validationError = challenge.ValidateAttempt(now, AuthenticationChallengePurpose.PhoneChange);
        if (validationError is not null)
            return new AuthenticationResult<bool>.Failure(new(validationError.Value, GetErrorMessage(validationError.Value)));

        if (!await store.TryReserveAttemptAsync(challenge, cancellationToken))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts."));

        string normalizedCode = AuthenticationInputValidator.NormalizeVerificationCode(command.Code);
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
                return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ChallengeAttemptsExhausted, "Too many failed attempts."));

            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidVerificationCode, "Invalid verification code."));
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
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ChallengeAlreadyUsed, "Challenge has already been used."));

        if (challenge.Status == AuthenticationChallengeStatus.Pending)
            challenge.Complete(now);

        bool updated = await accountUpdater.SetPhoneNumberAsync(command.UserId, challenge.Destination, cancellationToken);
        return updated
            ? new AuthenticationResult<bool>.Success(true)
            : new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Failed to update phone number."));
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
