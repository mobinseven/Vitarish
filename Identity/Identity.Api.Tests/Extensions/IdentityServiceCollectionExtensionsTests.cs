using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Identity.Api.Extensions;
using Vitarish.Identity.Core;
using Vitarish.Identity.Core.Abstractions;

namespace Identity.Api.Tests.Extensions;

public class IdentityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddIdentityContext_RegistersHttpContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdentityContext();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHttpContextAccessor));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddIdentityContext_RegistersIIdentityContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdentityContext();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IIdentityContext));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddIdentityContext_RegistersIAdminContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdentityContext();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAdminContext));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddIdentityContext_CanResolveIdentityContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdentityContext();

        // Create a mock HttpContext with user
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Subject, "test-user") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var httpContext = new DefaultHttpContext { User = principal };

        // Add the HttpContext to the accessor
        services.AddSingleton<IHttpContextAccessor>(new MockHttpContextAccessor(httpContext));

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var identityContext = serviceProvider.GetService<IIdentityContext>();

        // Assert
        identityContext.Should().NotBeNull();
        identityContext!.UserId.Should().Be("test-user");
    }

    [Fact]
    public void AddIdentityContext_CanResolveAdminContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdentityContext();

        // Create a mock HttpContext with admin user
        var claims = new[]
        {
            new Claim(IdentityConstants.ClaimTypes.Subject, "admin-user"),
            new Claim(ClaimTypes.Role, IdentityConstants.Admin)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var httpContext = new DefaultHttpContext { User = principal };

        // Add the HttpContext to the accessor
        services.AddSingleton<IHttpContextAccessor>(new MockHttpContextAccessor(httpContext));

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var adminContext = serviceProvider.GetService<IAdminContext>();

        // Assert
        adminContext.Should().NotBeNull();
        adminContext!.UserId.Should().Be("admin-user");
        adminContext.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void AddIdentityContext_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddIdentityContext();

        // Assert
        result.Should().BeSameAs(services);
    }

    private sealed class MockHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }

        public MockHttpContextAccessor(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }
    }
}
