using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Authentication.Infrastructure.Identity;
using Vitarish.Hosting.Infrastructure.Options;
using Xunit;

namespace Main.Tests;

public sealed class AuthenticationCookieSecurityTests
{
    [Fact]
    public void Application_cookie_is_host_only_lax_secure_and_http_only()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddHostAuthentication(new SecurityConfigurationOptions());
        using WebApplication app = builder.Build();
        CookieAuthenticationOptions options = app.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        options.Cookie.SameSite.Should().Be(SameSiteMode.Lax);
        options.Cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
        options.Cookie.HttpOnly.Should().BeTrue();
        options.Cookie.Domain.Should().BeNull();
        options.ExpireTimeSpan.Should().Be(TimeSpan.FromMinutes(30));
        options.SlidingExpiration.Should().BeTrue();
        options.EventsType.Should().Be(typeof(AuthenticationCookieEvents));
    }
}
