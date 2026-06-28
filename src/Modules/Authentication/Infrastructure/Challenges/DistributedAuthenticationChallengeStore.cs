using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class DistributedAuthenticationChallengeStore : IAuthenticationChallengeStore { private readonly InMemoryAuthenticationChallengeStore inner = new(); public Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken) => inner.SaveAsync(challenge, cancellationToken); public Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken) => inner.GetAsync(id, cancellationToken); }
