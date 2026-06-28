using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class DuendeLogoutContextService : ILogoutContextService { public Task<LogoutContext?> GetAsync(string? logoutId, CancellationToken cancellationToken) => Task.FromResult<LogoutContext?>(new(logoutId, null, null, true)); }
