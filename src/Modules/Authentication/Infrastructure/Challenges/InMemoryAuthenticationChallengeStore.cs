using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class InMemoryAuthenticationChallengeStore : IAuthenticationChallengeStore { private readonly ConcurrentDictionary<string, AuthenticationChallenge> challenges = new(); public Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken) { challenges[challenge.Id.Value] = challenge; return Task.CompletedTask; } public Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken) => Task.FromResult(challenges.TryGetValue(id.Value, out AuthenticationChallenge? c) ? c : null); }
