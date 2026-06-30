using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class CookieAuthenticationReturnUrlStore(IHttpContextAccessor httpContextAccessor) : IAuthenticationReturnUrlStore, IAuthenticationRedirectResolver
{
    private const string CookieName = "Authentication.ReturnUrl";

    public Task StoreAsync(string returnUrl, CancellationToken cancellationToken)
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        if (context is not null)
        {
            context.Response.Cookies.Append(
                CookieName,
                ReturnUrlPolicy.NormalizeOrDefault(returnUrl).Value,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });
        }

        return Task.CompletedTask;
    }

    public Task<string?> ReadAsync(CancellationToken cancellationToken)
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        string? value = context?.Request.Cookies[CookieName];
        return Task.FromResult(value);
    }

    public string Resolve(string? requestedReturnUrl, bool requiresProfileCompletion) => requiresProfileCompletion ? "/account/profile/complete" : ReturnUrlPolicy.NormalizeOrDefault(requestedReturnUrl).Value;

    public Task<string> ResolveAsync(string? requestedReturnUrl, bool requiresProfileCompletion, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Resolve(requestedReturnUrl, requiresProfileCompletion));
    }

    public Task<string> ResolveAsync(string? requestedReturnUrl, bool requiresProfileCompletion, string? userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Resolve(requestedReturnUrl, requiresProfileCompletion));
    }
}
