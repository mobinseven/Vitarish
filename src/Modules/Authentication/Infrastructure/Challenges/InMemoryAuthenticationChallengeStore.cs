using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Infrastructure;

public sealed class InMemoryAuthenticationChallengeStore : IAuthenticationChallengeStore
{
    private readonly ConcurrentDictionary<string, AuthenticationChallenge> challenges = new();

    public Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        challenges[challenge.Id.Value] = challenge;
        return Task.CompletedTask;
    }

    public Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken)
    {
        challenges.TryGetValue(id.Value, out AuthenticationChallenge? c);
        return Task.FromResult(c);
    }

    public Task ReplaceAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        challenges[challenge.Id.Value] = challenge;
        return Task.CompletedTask;
    }
}
