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
}
