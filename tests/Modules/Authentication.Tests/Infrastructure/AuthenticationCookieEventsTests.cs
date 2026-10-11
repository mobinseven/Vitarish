using FluentAssertions;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Time.Testing;
using Moq;
using System.Security.Claims;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Infrastructure.Identity;
using Vitarish.Requests.Application.Abstractions;
using Xunit;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class AuthenticationCookieEventsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Signing_in_always_issues_a_persistent_application_cookie()
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
            new AuthenticationProperties { IsPersistent = false },
            new CookieOptions());

        await events.SigningIn(context);

        context.Properties.IsPersistent.Should().BeTrue();
    }

    [Fact]
    public async Task A_valid_browser_session_ticket_is_reissued_as_a_persistent_cookie()
    {
        CookieValidatePrincipalContext context = ValidationContext(new AuthenticationProperties { IsPersistent = false });

        await Events(new AuthenticationSessionValidationResult(true)).ValidatePrincipal(context);

        context.Principal.Should().NotBeNull();
        context.Properties.IsPersistent.Should().BeTrue();
        context.ShouldRenew.Should().BeTrue();
    }

    [Fact]
    public async Task A_ticket_matching_the_effective_idle_expiry_is_not_renewed()
    {
        CookieValidatePrincipalContext context = ValidationContext(new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = Now.AddMinutes(-5),
            ExpiresUtc = Now.AddMinutes(-5).AddMinutes(20160)
        });

        await Events(new AuthenticationSessionValidationResult(true, 20160)).ValidatePrincipal(context);

        context.ShouldRenew.Should().BeFalse();
        context.Properties.ExpiresUtc.Should().Be(Now.AddMinutes(-5).AddMinutes(20160));
    }

    [Fact]
    public async Task A_ticket_window_longer_than_the_effective_idle_expiry_is_shortened()
    {
        CookieValidatePrincipalContext context = ValidationContext(new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = Now.AddMinutes(-1),
            ExpiresUtc = Now.AddMinutes(-1).AddMinutes(20160)
        });

        await Events(new AuthenticationSessionValidationResult(true, 30)).ValidatePrincipal(context);

        context.ShouldRenew.Should().BeTrue();
        context.Properties.IssuedUtc.Should().Be(Now);
        context.Properties.ExpiresUtc.Should().Be(Now.AddMinutes(30));
    }

    [Fact]
    public async Task A_ticket_window_shorter_than_the_effective_idle_expiry_is_extended()
    {
        CookieValidatePrincipalContext context = ValidationContext(new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = Now.AddMinutes(-10),
            ExpiresUtc = Now.AddMinutes(20)
        });

        await Events(new AuthenticationSessionValidationResult(true, 20160)).ValidatePrincipal(context);

        context.ShouldRenew.Should().BeTrue();
        context.Properties.IssuedUtc.Should().Be(Now);
        context.Properties.ExpiresUtc.Should().Be(Now.AddMinutes(20160));
    }

    [Fact]
    public async Task An_invalid_server_session_rejects_the_cookie()
    {
        CookieValidatePrincipalContext context = ValidationContext(new AuthenticationProperties { IsPersistent = true });

        await Events(new AuthenticationSessionValidationResult(false)).ValidatePrincipal(context);

        context.Principal.Should().BeNull();
    }

    private static AuthenticationCookieEvents Events(AuthenticationSessionValidationResult validation)
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(registry => registry.ValidateAndTouchAsync("session-1", "user-1", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);
        return new(
            sessions.Object,
            Mock.Of<IRequestContext>(),
            Mock.Of<IMultiTenantContextAccessor<TenantInfo>>(),
            new FakeTimeProvider(Now));
    }

    private static CookieValidatePrincipalContext ValidationContext(AuthenticationProperties properties)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim(AuthenticationSessionClaimTypes.SessionId, "session-1"),
                new Claim(ClaimTypes.NameIdentifier, "user-1")
            ],
            IdentityConstants.ApplicationScheme));
        return new(
            new DefaultHttpContext(),
            new AuthenticationScheme(IdentityConstants.ApplicationScheme, IdentityConstants.ApplicationScheme, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            new AuthenticationTicket(principal, properties, IdentityConstants.ApplicationScheme));
    }
}
