using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentitySignInService : IAuthenticationSignInService { public Task<bool> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken) => Task.FromResult(false); public Task SignInAsync(string userId, bool rememberMe, CancellationToken cancellationToken) => Task.CompletedTask; public Task SignOutAsync(CancellationToken cancellationToken) => Task.CompletedTask; }
