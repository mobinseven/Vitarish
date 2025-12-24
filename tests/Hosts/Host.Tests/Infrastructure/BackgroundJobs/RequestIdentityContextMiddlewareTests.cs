using System.Security.Claims;
using FluentAssertions;
using Main.Tests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Main.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Hosting.Middleware;
using Vitarish.Requests.Infrastructure.Services;

namespace Vitarish.Requests.Tests;

public class RequestIdentityContextMiddlewareTests
{
    private static HttpContext CreateHttpContext(
        IServiceProvider services,
        ClaimsPrincipal? user = null,
        string? requestIdHeader = null)
    {
        DefaultHttpContext httpContext = new()
        {
            RequestServices = services
        };

        if (user is not null)
        {
            httpContext.User = user;
        }

        if (requestIdHeader is not null)
        {
            httpContext.Request.Headers[RequestIdentityConstants.RequestIdHeader] = requestIdHeader;
        }

        return httpContext;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        ServiceCollection services = new();
        _ = AddTestAuthentication(services);
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());
        return services.BuildServiceProvider();
    }

    private static IServiceScope CreateScope(IServiceProvider provider) => provider.CreateScope();

    private static AuthenticationBuilder AddTestAuthentication(ServiceCollection services)
    {
        services.AddLogging();
        return services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                JwtBearerDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                IdentityConstants.ApplicationScheme,
                _ => { });
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUser_PopulatesContextFromClaims()
    {
        // Arrange
        const string expectedTenantId = "tenant-from-claim";
        const string expectedOwnerId = "owner-from-claim";
        const string expectedUserId = "user-123";
        const string expectedClientId = "client-456";

        ClaimsPrincipal user = new(new ClaimsIdentity([
            new Claim(RequestIdentityConstants.TenantIdClaim, expectedTenantId),
            new Claim(RequestIdentityConstants.OwnerIdClaim, expectedOwnerId),
            new Claim("sub", expectedUserId),
            new Claim("client_id", expectedClientId)
        ], "test"));

        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider, user);

        bool nextCalled = false;
        RequestsContextMiddleware middleware = new((ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        nextCalled.Should().BeTrue();

        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        context.TenantId.Should().Be(expectedTenantId);
        context.OwnerId.Should().Be(expectedOwnerId);
        context.UserId.Should().Be(expectedUserId);
        context.ClientId.Should().Be(expectedClientId);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthenticatedUser_HandlesGracefully()
    {
        // Arrange
        ClaimsPrincipal user = new(new ClaimsIdentity());

        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider, user);

        bool nextCalled = false;
        RequestsContextMiddleware middleware = new((ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        nextCalled.Should().BeTrue();

        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        // When there are no claims and no configuration, TenantId should be null
        context.TenantId.Should().BeNull("TenantId should be null when there are no claims or configuration");
        context.UserId.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_WithoutRequestIdHeader_GeneratesNewRequestId()
    {
        // Arrange
        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider);

        RequestsContextMiddleware middleware = new((ctx) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        context.RequestId.Should().NotBeNullOrEmpty();
        Guid.TryParse(context.RequestId, out _).Should().BeTrue("Generated RequestId should be a valid GUID");
    }

    [Fact]
    public async Task InvokeAsync_WithExistingRequestIdHeader_UsesProvidedValue()
    {
        // Arrange
        const string expectedRequestId = "existing-request-id-123";

        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider, requestIdHeader: expectedRequestId);

        RequestsContextMiddleware middleware = new((ctx) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        context.RequestId.Should().Be(expectedRequestId);
    }

    [Fact]
    public async Task InvokeAsync_SetsRequestIdInResponseHeader()
    {
        // Arrange
        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider);

        RequestsContextMiddleware middleware = new((ctx) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers[RequestIdentityConstants.RequestIdHeader].ToString()
            .Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithOnlyTenantIdClaim_UsesClientIdForOwnerId()
    {
        // Arrange
        const string expectedTenantId = "tenant-only";
        const string expectedClientId = "fallback-client";

        ClaimsPrincipal user = new(new ClaimsIdentity([
            new Claim(RequestIdentityConstants.TenantIdClaim, expectedTenantId),
            new Claim("client_id", expectedClientId)
        ], "test"));

        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider, user);

        RequestsContextMiddleware middleware = new((ctx) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        context.TenantId.Should().Be(expectedTenantId);
        context.OwnerId.Should().Be(expectedClientId, "OwnerId should fallback to ClientId when owner_id claim is not present");
    }

    [Fact]
    public async Task InvokeAsync_WithOnlyOwnerIdClaim_SynthesizesTenantId()
    {
        // Arrange
        const string expectedOwnerId = "owner-only";

        ClaimsPrincipal user = new(new ClaimsIdentity([
            new Claim(RequestIdentityConstants.OwnerIdClaim, expectedOwnerId)
        ], "test"));

        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider, user);

        RequestsContextMiddleware middleware = new((ctx) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        context.OwnerId.Should().Be(expectedOwnerId);
        context.TenantId.Should().NotBeNullOrEmpty("TenantId should be synthesized");
        context.TenantId.Should().EndWith(expectedOwnerId, "TenantId should include the ownerId");
    }

    [Fact]
    public async Task InvokeAsync_WithIssuerClaim_PopulatesAuthority()
    {
        // Arrange
        const string expectedIssuer = "https://issuer.example.com";

        ClaimsPrincipal user = new(new ClaimsIdentity([
            new Claim("iss", expectedIssuer)
        ], "test"));

        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider, user);

        RequestsContextMiddleware middleware = new((ctx) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        context.Authority.Should().Be(expectedIssuer);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        IServiceProvider rootProvider = CreateServiceProvider();
        using IServiceScope scope = CreateScope(rootProvider);
        HttpContext httpContext = CreateHttpContext(scope.ServiceProvider);

        int callOrder = 0;
        int middlewareCallOrder = 0;

        RequestsContextMiddleware middleware = new((ctx) =>
        {
            callOrder++;
            middlewareCallOrder = callOrder;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        middlewareCallOrder.Should().Be(1, "next middleware should be called exactly once");
    }

    [Fact]
    public async Task InvokeAsync_WhenWriterNotRegistered_StillCallsNextMiddleware()
    {
        // Arrange
        // Create a service provider without IRequestContextWriter registered
        ServiceCollection services = new();
        _ = AddTestAuthentication(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpContext httpContext = CreateHttpContext(provider);

        bool nextCalled = false;
        RequestsContextMiddleware middleware = new((ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        nextCalled.Should().BeTrue("next middleware should still be called even when writer is not registered");
    }
}
