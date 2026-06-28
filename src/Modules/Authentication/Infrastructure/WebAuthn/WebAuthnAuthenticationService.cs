using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class WebAuthnAuthenticationService : IWebAuthnAuthenticationService { public Task<string> BeginAsync(string? login, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid().ToString("N")); public Task<string?> CompleteAsync(string challengeId, string credentialJson, CancellationToken cancellationToken) => Task.FromResult<string?>(null); }
