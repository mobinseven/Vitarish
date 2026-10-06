using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Duende.IdentityServer.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Users.Domain.Entities;
using Xunit;

namespace Main.Tests;

public sealed class IdentityServerSessionCookieTests
{
    [Fact]
    public void Oidc_session_management_is_off_so_no_script_readable_session_cookie_is_issued()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration["Vitarish:Identity:Authority"] = "https://identity.example.test";
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new("CN=identity-signing-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(10));

        // The host registers ASP.NET Identity before IdentityServer's Identity integration.
        _ = builder.Services.AddIdentity<User, IdentityRole>();
        builder.AddHostIdentityServer(options => options.UseInMemoryDatabase(nameof(IdentityServerSessionCookieTests)), certificate);
        using WebApplication app = builder.Build();
        IdentityServerOptions options = app.Services.GetRequiredService<IOptions<IdentityServerOptions>>().Value;

        options.Endpoints.EnableCheckSessionEndpoint.Should().BeFalse();
        options.Authentication.CheckSessionCookieSameSiteMode.Should().Be(SameSiteMode.Lax);
    }
}
