using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentityAuthenticationUserDirectory : IAuthenticationUserDirectory { public Task<AuthenticationUserDescriptor?> FindByLoginAsync(string login, CancellationToken cancellationToken) => Task.FromResult<AuthenticationUserDescriptor?>(null); public Task<AuthenticationUserDescriptor?> GetCurrentAsync(CancellationToken cancellationToken) => Task.FromResult<AuthenticationUserDescriptor?>(null); }
