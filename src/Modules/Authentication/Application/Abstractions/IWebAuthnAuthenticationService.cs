using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IWebAuthnAuthenticationService
{
    Task<WebAuthnBeginResult> BeginAsync(string? userNameOrEmail, CancellationToken cancellationToken);
    Task<string?> GetAttemptKeyAsync(string challengeId, CancellationToken cancellationToken);
    Task<string?> GetAttemptKeyAsync(string challengeId, string credentialJson, CancellationToken cancellationToken) =>
        GetAttemptKeyAsync(challengeId, cancellationToken);
    Task<WebAuthnCompleteResult> CompleteAsync(string challengeId, string credentialJson, CancellationToken cancellationToken);
}

public sealed record WebAuthnBeginResult(string ChallengeId, string OptionsJson, DateTimeOffset ExpiresAt, int RemainingAttempts);

public sealed record WebAuthnCompleteResult(bool Succeeded, string? UserId, AuthenticationFailure? Failure);
