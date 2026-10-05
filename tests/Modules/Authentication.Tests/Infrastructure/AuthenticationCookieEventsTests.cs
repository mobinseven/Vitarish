using FluentAssertions;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Security.Claims;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Infrastructure.Identity;
using Vitarish.Requests.Application.Abstractions;
using Xunit;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class AuthenticationCookieEventsTests
{
    [Fact]
    public async Task Signing_in_never_issues_a_persistent_application_cookie()
    {
        AuthenticationCookieEvents events = new(
            Mock.Of<IAuthenticationSessionRegistry>(),
            Mock.Of<IRequestContext>(),
            Mock.Of<IMultiTenantContextAccessor<TenantInfo>>(),
            TimeProvider.System);
        CookieSigningInContext context = new(
            new DefaultHttpContext(),
            new AuthenticationScheme(IdentityConstants.ApplicationScheme, IdentityConstants.ApplicationScheme, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            new ClaimsPrincipal(),
            new AuthenticationProperties { IsPersistent = true },
            new CookieOptions());

        await events.SigningIn(context);

        context.Properties.IsPersistent.Should().BeFalse();
    }

    [Fact]
    public async Task A_valid_persistent_ticket_is_reissued_as_a_browser_session_cookie()
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(registry => registry.ValidateAndTouchAsync("session-1", "user-1", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationSessionValidationResult(true));
        AuthenticationCookieEvents events = new(
            sessions.Object,
            Mock.Of<IRequestContext>(),
            Mock.Of<IMultiTenantContextAccessor<TenantInfo>>(),
            TimeProvider.System);
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim(AuthenticationSessionClaimTypes.SessionId, "session-1"),
                new Claim(ClaimTypes.NameIdentifier, "user-1")
            ],
            IdentityConstants.ApplicationScheme));
        CookieValidatePrincipalContext context = new(
            new DefaultHttpContext(),
            new AuthenticationScheme(IdentityConstants.ApplicationScheme, IdentityConstants.ApplicationScheme, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            new AuthenticationTicket(principal, new AuthenticationProperties { IsPersistent = true }, IdentityConstants.ApplicationScheme));

        await events.ValidatePrincipal(context);

        context.Principal.Should().NotBeNull();
        context.Properties.IsPersistent.Should().BeFalse();
        context.ShouldRenew.Should().BeTrue();
    }
}
