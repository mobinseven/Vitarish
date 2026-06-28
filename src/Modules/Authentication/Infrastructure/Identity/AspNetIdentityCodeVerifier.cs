using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentityCodeVerifier : IAuthenticationCodeVerifier { public Task<bool> VerifyAsync(string challengeId, string code, CancellationToken cancellationToken) => Task.FromResult(false); }
