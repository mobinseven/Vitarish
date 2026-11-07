using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Handlers;

public sealed record PendingAuthenticationChallenge(ChallengeResult Challenge, string Destination, string? ReturnUrl);

public sealed class GetPendingAuthenticationChallengeHandler(
    IPendingAuthenticationChallenge pending, IAuthenticationChallengeStore store, IAuthenticationUtcClock clock)
{
    public async Task<PendingAuthenticationChallenge?> Handle(CancellationToken cancellationToken)
    {
        if (pending.Read() is not { } reference) return null;
        if (reference.Challenge.ExpiresAt <= clock.UtcNow) return null;
        // Recovery deliberately gives identical presentation for eligible, unknown, blocked,
        // cancelled-delivery and completed challenges. Never query eligibility or disclose contact.
        if (reference.Challenge.Purpose is AuthenticationChallengePurpose.EmailPasswordRecovery or AuthenticationChallengePurpose.PhonePasswordRecovery)
            return new(reference.Challenge, string.Empty, reference.ReturnUrl);
        AuthenticationChallenge? challenge = await store.GetAsync(new AuthenticationChallengeId(reference.Challenge.ChallengeId), cancellationToken);
        if (challenge is null || challenge.Status != AuthenticationChallengeStatus.Pending ||
            challenge.ExpiresAt <= clock.UtcNow || challenge.RemainingAttempts <= 0)
            return null;
        return new(new ChallengeResult(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts),
            challenge.Purpose == AuthenticationChallengePurpose.PasswordLoginOtp ? string.Empty : challenge.Destination, reference.ReturnUrl);
    }
}
