using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationChallengeStore
{
    Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
    Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken);
    Task ReplaceAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
}
