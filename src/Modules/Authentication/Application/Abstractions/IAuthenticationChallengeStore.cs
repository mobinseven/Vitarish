using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationChallengeStore
{
    Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken);
    Task ReplaceAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task<bool> TryReserveAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task ReleaseAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task<bool> TryReserveCompletionAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task ReleaseCompletionAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task<bool> TryConsumeAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task<int> TryRegisterFailureAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
}
