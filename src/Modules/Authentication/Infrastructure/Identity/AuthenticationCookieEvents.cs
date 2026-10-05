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

    public override Task SigningIn(CookieSigningInContext context)
    {
        context.Properties.IsPersistent = false;
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

        if (validation.InactivityTimeoutMinutes is int inactivityTimeoutMinutes)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            DateTimeOffset maximumExpiry = now.AddMinutes(inactivityTimeoutMinutes);
            if (context.Properties.ExpiresUtc is null || context.Properties.ExpiresUtc > maximumExpiry)
            {
                context.Properties.ExpiresUtc = maximumExpiry;
                context.ShouldRenew = true;
            }
        }
    }
}
