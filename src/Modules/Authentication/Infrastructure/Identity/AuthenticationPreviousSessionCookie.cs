using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure.Identity;

public sealed class AuthenticationPreviousSessionCookie(IHttpContextAccessor accessor,
    IOptionsMonitor<CookieAuthenticationOptions> options, TimeProvider clock) : IAuthenticationPreviousSessionCookie
{
    public Guid? Read(string userId)
    {
        HttpContext? context = accessor.HttpContext;
        if (context is null) return null;
        CookieAuthenticationOptions cookie = options.Get(IdentityConstants.ApplicationScheme);
        string? value = cookie.CookieManager.GetRequestCookie(context, cookie.Cookie.Name!);
        if (value is null) return null;
        var ticket = cookie.TicketDataFormat.Unprotect(value);
        if (ticket?.Properties.ExpiresUtc is not { } expires || expires <= clock.GetUtcNow() ||
            AuthenticationSessionClaimTypes.FindSubject(ticket.Principal) != userId)
            return null;
        return Guid.TryParse(ticket.Principal.FindFirst(AuthenticationSessionClaimTypes.SessionId)?.Value, out Guid id) ? id : null;
    }
}
