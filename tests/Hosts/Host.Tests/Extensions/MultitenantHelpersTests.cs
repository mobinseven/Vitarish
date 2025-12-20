using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Main.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Hosting.Middleware;
using Vitarish.Requests.Infrastructure.Services;

namespace Main.Tests.Extensions;

public class MultitenantHelpersTests
{
    private static IServiceProvider CreateServiceProvider()
    {
        ServiceCollection services = new();
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ResolveTenantAsync_WithNullContext_ReturnsNull()
    {
        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveTenantAsync_WithInitializedTenantId_ReturnsTenantId()
    {
        // Arrange
        const string expectedTenantId = "test-tenant-123";

        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        // Simulate what RequestsContextMiddleware does
        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: "test-client",
            ownerId: "test-owner",
            tenantId: expectedTenantId);

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        result.Should().Be(expectedTenantId);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithInitializedOwnerId_ReturnsOwnerId()
    {
        // Arrange
        const string expectedOwnerId = "test-owner-456";

        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        // Simulate what RequestsContextMiddleware does (without tenantId)
        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: "test-client",
            ownerId: expectedOwnerId,
            tenantId: null);

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        result.Should().Be(expectedOwnerId);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithInitializedClientId_ReturnsClientId()
    {
        // Arrange
        const string expectedClientId = "test-client-789";

        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        // Simulate what RequestsContextMiddleware does (without tenantId and ownerId)
        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: expectedClientId,
            ownerId: "",
            tenantId: null);

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        result.Should().Be(expectedClientId);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithUninitializedContext_ReturnsEmptyString()
    {
        // Arrange
        // This test simulates the issue where the context is NOT initialized
        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        // DO NOT call SetContext - this simulates the bug scenario

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        // With an uninitialized context, TenantId, OwnerId, and ClientId are all empty/null
        // So the result should be empty string (the default value for ClientId)
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveTenantAsync_UsesCorrectServiceScope()
    {
        // Arrange
        // This test verifies that the IRequestContext comes from the correct scope
        const string expectedTenantId = "correct-scope-tenant";

        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        // Initialize the context in the same scope
        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: "test-client",
            ownerId: "test-owner",
            tenantId: expectedTenantId);

        // Verify that resolving from the same scope gets the initialized context
        IRequestContext requestContext = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        requestContext.TenantId.Should().Be(expectedTenantId, "Context should be initialized in the same scope");

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        result.Should().Be(expectedTenantId);
    }

    [Fact]
    public async Task ResolveTenantAsync_PrefersTenantIdOverOwnerIdAndClientId()
    {
        // Arrange
        const string tenantId = "tenant-priority";
        const string ownerId = "owner-fallback";
        const string clientId = "client-fallback";

        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: clientId,
            ownerId: ownerId,
            tenantId: tenantId);

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        result.Should().Be(tenantId, "TenantId should be preferred over OwnerId and ClientId");
    }

    [Fact]
    public async Task ResolveTenantAsync_PrefersOwnerIdOverClientId()
    {
        // Arrange
        const string ownerId = "owner-priority";
        const string clientId = "client-fallback";

        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: clientId,
            ownerId: ownerId,
            tenantId: null);

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        result.Should().Be(ownerId, "OwnerId should be preferred over ClientId when TenantId is null");
    }

    /// <summary>
    /// Tests that the full middleware pipeline correctly initializes the request context
    /// before the tenant resolver middleware reads from it.
    /// </summary>
    [Fact]
    public async Task ResolveTenantAsync_AfterMiddleware_ReturnsInitializedValues()
    {
        // Arrange
        const string expectedTenantId = "middleware-tenant";
        const string expectedOwnerId = "middleware-owner";
        const string expectedClientId = "middleware-client";

        using ServiceProvider rootProvider = (ServiceProvider)CreateServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        // Add test claims
        ClaimsPrincipal user = new(new ClaimsIdentity(
        [
            new Claim(RequestIdentityConstants.TenantIdClaim, expectedTenantId),
            new Claim(RequestIdentityConstants.OwnerIdClaim, expectedOwnerId),
            new Claim("client_id", expectedClientId)
        ], "test"));
        httpContext.User = user;

        string? resolvedTenant = null;

        // Create middleware that simulates RequestsContextMiddleware followed by tenant resolution
        RequestsContextMiddleware middleware = new(async ctx =>
        {
            // This simulates what the tenant resolver (in UseMultiTenant middleware) would do
            // after RequestsContextMiddleware has run
            resolvedTenant = await MultitenantHelpers.ResolveTenantAsync(ctx);
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        resolvedTenant.Should().Be(expectedTenantId, "Tenant resolver should get the initialized context");
    }

    /// <summary>
    /// Tests that the IHttpContextAccessor fallback helps when contexts might differ.
    /// </summary>
    [Fact]
    public async Task ResolveTenantAsync_WithHttpContextAccessor_UsesCorrectContext()
    {
        // Arrange
        const string expectedTenantId = "accessor-tenant";

        ServiceCollection services = new();
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());

        // Create HttpContextAccessor and set it up
        HttpContextAccessor httpContextAccessor = new();
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        using ServiceProvider rootProvider = services.BuildServiceProvider();
        using IServiceScope scope = rootProvider.CreateScope();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = scope.ServiceProvider
        };

        // Set the HttpContext in the accessor (simulates ASP.NET Core behavior)
        httpContextAccessor.HttpContext = httpContext;

        // Initialize the context
        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: "test-client",
            ownerId: "test-owner",
            tenantId: expectedTenantId);

        // Act
        string? result = await MultitenantHelpers.ResolveTenantAsync(httpContext);

        // Assert
        result.Should().Be(expectedTenantId);
    }

    /// <summary>
    /// Tests that when the passed context has a different scope than the IHttpContextAccessor's context,
    /// the helper correctly uses the IHttpContextAccessor's context (which is properly initialized).
    /// This simulates the scenario where Finbuckle might pass a context with a different scope.
    /// </summary>
    [Fact]
    public async Task ResolveTenantAsync_WithDifferentScopes_UsesHttpContextAccessorScope()
    {
        // Arrange
        const string expectedTenantId = "correct-scope-tenant";

        ServiceCollection services = new();
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());

        // Create HttpContextAccessor
        HttpContextAccessor httpContextAccessor = new();
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        using ServiceProvider rootProvider = services.BuildServiceProvider();

        // Create the "correct" scope (simulates the request scope where middleware runs)
        using IServiceScope correctScope = rootProvider.CreateScope();
        DefaultHttpContext correctContext = new()
        {
            RequestServices = correctScope.ServiceProvider
        };
        httpContextAccessor.HttpContext = correctContext;

        // Initialize context in the correct scope
        IRequestContextWriter writer = correctScope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
        writer.SetContext(
            authority: "test-authority",
            clientId: "test-client",
            ownerId: "test-owner",
            tenantId: expectedTenantId);

        // Create a "wrong" scope (simulates what might happen if Finbuckle creates a new scope)
        using IServiceScope wrongScope = rootProvider.CreateScope();
        DefaultHttpContext wrongContext = new()
        {
            RequestServices = wrongScope.ServiceProvider
        };

        // Verify the wrong scope has an uninitialized context
        IRequestContext wrongRequestContext = wrongScope.ServiceProvider.GetRequiredService<IRequestContext>();
        wrongRequestContext.TenantId.Should().BeNull("Wrong scope should have uninitialized context");

        // Act - pass the "wrong" context but expect the helper to use IHttpContextAccessor
        string? result = await MultitenantHelpers.ResolveTenantAsync(wrongContext);

        // Assert - should get the value from the correct scope (via IHttpContextAccessor)
        result.Should().Be(expectedTenantId,
            "Helper should use IHttpContextAccessor's HttpContext which has the initialized IRequestContext");
    }
}
