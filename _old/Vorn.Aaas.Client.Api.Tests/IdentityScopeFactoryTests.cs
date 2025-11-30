using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Internal;
using Vorn.Aaas.Client.Api.Models;
using Vorn.Aaas.Client.Api.Options;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Vorn.Aaas.Client.Api.Tests;

public class IdentityScopeFactoryTests
{
    [Fact]
    public void CreateScope_CreatesNewScope_WithInitializedIdentityContext()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Configure options
        var vornOptions = new VornOptions
        {
            Aaas = new AaasOptions
            {
                Authority = "https://authority.example.com",
                ClientId = "test-client-id",
                Secret = "test-secret"
            }
        };
        services.AddSingleton<IOptions<VornOptions>>(MsOptions.Create(vornOptions));
        
        // Register identity context services
        services.AddScoped<IRequestIdentityForwarder, MockRequestIdentityForwarder>();
        services.AddScoped<RequestIdentityContext>();
        services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextGenerator, RequestIdentityContextGenerator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new IdentityScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>());
        
        var scopeIdentity = new ScopeRequestIdentity(
            OwnerId: "test-owner",
            TenantId: "test-tenant",
            ResourceId: "test-resource",
            RequestId: "test-request-123",
            AdditionalInfo: "test-info"
        );

        // Act
        using var scope = factory.CreateScope(scopeIdentity);
        var context = scope.ServiceProvider.GetRequiredService<IRequestIdentityContext>();

        // Assert
        Assert.NotNull(context);
        Assert.Equal("https://authority.example.com", context.Authority);
        Assert.Equal("test-client-id", context.ClientId);
        Assert.Equal("test-owner", context.OwnerId);
        Assert.Equal("test-tenant", context.TenantId);
        Assert.Equal("test-resource", context.ResourceId);
        Assert.Equal("test-request-123", context.RequestId);
        Assert.Equal("test-info", context.AdditionalInfo);
    }

    [Fact]
    public void CreateScope_UsesDefaultValues_WhenIdentityIsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var vornOptions = new VornOptions
        {
            Aaas = new AaasOptions
            {
                Authority = "https://authority.example.com",
                ClientId = "test-client-id",
                Secret = "test-secret"
            }
        };
        services.AddSingleton<IOptions<VornOptions>>(MsOptions.Create(vornOptions));
        
        services.AddScoped<IRequestIdentityForwarder, MockRequestIdentityForwarder>();
        services.AddScoped<RequestIdentityContext>();
        services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextGenerator, RequestIdentityContextGenerator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new IdentityScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>());
        
        var scopeIdentity = new ScopeRequestIdentity();

        // Act
        using var scope = factory.CreateScope(scopeIdentity);
        var context = scope.ServiceProvider.GetRequiredService<IRequestIdentityContext>();

        // Assert
        Assert.NotNull(context);
        Assert.Equal("https://authority.example.com", context.Authority);
        Assert.Equal("test-client-id", context.ClientId);
        Assert.Equal("test-client-id", context.OwnerId); // Falls back to ClientId
        Assert.Equal("test-client-id.test-client-id", context.TenantId); // Defaults to clientId.ownerId
        Assert.Null(context.ResourceId);
        Assert.NotNull(context.RequestId); // Should be generated
        Assert.Null(context.AdditionalInfo);
    }

    [Fact]
    public void CreateScope_CreatesIndependentScopes_WithDifferentIdentities()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var vornOptions = new VornOptions
        {
            Aaas = new AaasOptions
            {
                Authority = "https://authority.example.com",
                ClientId = "test-client-id",
                Secret = "test-secret"
            }
        };
        services.AddSingleton<IOptions<VornOptions>>(MsOptions.Create(vornOptions));
        
        services.AddScoped<IRequestIdentityForwarder, MockRequestIdentityForwarder>();
        services.AddScoped<RequestIdentityContext>();
        services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextGenerator, RequestIdentityContextGenerator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new IdentityScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>());
        
        var scopeIdentity1 = new ScopeRequestIdentity(OwnerId: "owner-1", TenantId: "tenant-1");
        var scopeIdentity2 = new ScopeRequestIdentity(OwnerId: "owner-2", TenantId: "tenant-2");

        // Act
        using var scope1 = factory.CreateScope(scopeIdentity1);
        using var scope2 = factory.CreateScope(scopeIdentity2);
        
        var context1 = scope1.ServiceProvider.GetRequiredService<IRequestIdentityContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<IRequestIdentityContext>();

        // Assert
        Assert.NotSame(context1, context2);
        Assert.Equal("owner-1", context1.OwnerId);
        Assert.Equal("tenant-1", context1.TenantId);
        Assert.Equal("owner-2", context2.OwnerId);
        Assert.Equal("tenant-2", context2.TenantId);
    }

    [Fact]
    public async Task CreateScope_SupportsAsyncDisposal()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var vornOptions = new VornOptions
        {
            Aaas = new AaasOptions
            {
                Authority = "https://authority.example.com",
                ClientId = "test-client-id",
                Secret = "test-secret"
            }
        };
        services.AddSingleton<IOptions<VornOptions>>(MsOptions.Create(vornOptions));
        
        services.AddScoped<IRequestIdentityForwarder, MockRequestIdentityForwarder>();
        services.AddScoped<RequestIdentityContext>();
        services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextGenerator, RequestIdentityContextGenerator>();
        services.AddScoped<AsyncDisposableService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new IdentityScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>());
        
        var scopeIdentity = new ScopeRequestIdentity(OwnerId: "test-owner");
        AsyncDisposableService? asyncService;

        // Act
        await using (var scope = factory.CreateScope(scopeIdentity))
        {
            asyncService = scope.ServiceProvider.GetRequiredService<AsyncDisposableService>();
            Assert.False(asyncService.IsDisposed);
        }

        // Assert
        Assert.True(asyncService.IsDisposed);
    }

    [Fact]
    public void CreateScope_ThrowsInvalidOperationException_WhenAuthorityIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var vornOptions = new VornOptions
        {
            Aaas = new AaasOptions
            {
                Authority = null!, // Missing authority
                ClientId = "test-client-id",
                Secret = "test-secret"
            }
        };
        services.AddSingleton<IOptions<VornOptions>>(MsOptions.Create(vornOptions));
        
        services.AddScoped<IRequestIdentityForwarder, MockRequestIdentityForwarder>();
        services.AddScoped<RequestIdentityContext>();
        services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextGenerator, RequestIdentityContextGenerator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new IdentityScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>());
        
        var scopeIdentity = new ScopeRequestIdentity(OwnerId: "test-owner");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.CreateScope(scopeIdentity));
    }

    [Fact]
    public void CreateScope_ThrowsInvalidOperationException_WhenClientIdIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var vornOptions = new VornOptions
        {
            Aaas = new AaasOptions
            {
                Authority = "https://authority.example.com",
                ClientId = null!, // Missing client ID
                Secret = "test-secret"
            }
        };
        services.AddSingleton<IOptions<VornOptions>>(MsOptions.Create(vornOptions));
        
        services.AddScoped<IRequestIdentityForwarder, MockRequestIdentityForwarder>();
        services.AddScoped<RequestIdentityContext>();
        services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
        services.AddScoped<IRequestIdentityContextGenerator, RequestIdentityContextGenerator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new IdentityScopeFactory(serviceProvider.GetRequiredService<IServiceScopeFactory>());
        
        var scopeIdentity = new ScopeRequestIdentity(OwnerId: "test-owner");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.CreateScope(scopeIdentity));
    }

    // Mock implementation of IRequestIdentityForwarder that doesn't make HTTP calls
    private sealed class MockRequestIdentityForwarder : IRequestIdentityForwarder
    {
        public ValueTask ForwardAsync(RequestIdentityDto envelope, IRequestIdentityContextWriter? writer = null, CancellationToken cancellationToken = default)
        {
            // Do nothing - just a mock for testing
            return ValueTask.CompletedTask;
        }
    }

    // Test helper class for async disposal testing
    private sealed class AsyncDisposableService : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
