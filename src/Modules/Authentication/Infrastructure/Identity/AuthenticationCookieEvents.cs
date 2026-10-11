using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure.Identity;

public sealed class AuthenticationCookieEvents(
    IAuthenticationSessionRegistry sessionRegistry,
    IRequestContext requestContext,
    IMultiTenantContextAccessor<TenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider) : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    // The cookie persists across browser restarts; the server session registry's idle
    // expiry, sign-out, and revocation end the session.
    public override Task SigningIn(CookieSigningInContext context)
    {
        context.Properties.IsPersistent = true;
        return Task.CompletedTask;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        string? sessionId = context.Principal?.FindFirst(AuthenticationSessionClaimTypes.SessionId)?.Value;
        string? userId = AuthenticationSessionClaimTypes.FindSubject(context.Principal);
        string? tenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id ?? requestContext.TenantId;
        AuthenticationSessionValidationResult validation = string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(userId)
            ? new(false)
            : await sessionRegistry.ValidateAndTouchAsync(sessionId, userId, tenantId, context.HttpContext.RequestAborted);
        if (!validation.IsValid)
        {
            context.RejectPrincipal();
            return;
        }

        // Sliding renewal reuses the ticket's properties without calling SigningIn, so a
        // ticket issued as a browser-session cookie (before this policy) is reissued here
        // as a persistent cookie.
        if (!context.Properties.IsPersistent)
        {
            context.Properties.IsPersistent = true;
            context.ShouldRenew = true;
        }

        // Renewal keeps the ticket's issued-to-expiry window, so the window must equal the
        // effective idle expiry: never outliving it, and growing when an administrator raises it.
        if (validation.InactivityTimeoutMinutes is int inactivityTimeoutMinutes)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            TimeSpan window = TimeSpan.FromMinutes(inactivityTimeoutMinutes);
            if (context.Properties.IssuedUtc is not DateTimeOffset issued ||
                context.Properties.ExpiresUtc is not DateTimeOffset expires ||
                expires - issued != window ||
                expires > now + window)
            {
                context.Properties.IssuedUtc = now;
                context.Properties.ExpiresUtc = now + window;
                context.ShouldRenew = true;
            }
        }
    }
}
