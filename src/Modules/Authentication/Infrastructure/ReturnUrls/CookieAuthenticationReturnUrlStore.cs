using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class CookieAuthenticationReturnUrlStore : IAuthenticationReturnUrlStore, IAuthenticationRedirectResolver { private string? value; public Task StoreAsync(string returnUrl, CancellationToken cancellationToken) { value = ReturnUrlPolicy.NormalizeOrDefault(returnUrl).Value; return Task.CompletedTask; } public Task<string?> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(value); public string Resolve(string? requestedReturnUrl, bool requiresProfileCompletion) => requiresProfileCompletion ? "/Account/Settings" : ReturnUrlPolicy.NormalizeOrDefault(requestedReturnUrl).Value; }
