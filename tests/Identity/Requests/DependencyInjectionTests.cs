using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Hosting.DependencyInjection;
using Vitarish.Requests.Infrastructure.Services;

namespace Vitarish.Requests.Tests;

public class DependencyInjectionTests
{
    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
    }

    [Fact]
    public void AddRequestIdentityContext_RegistersIRequestIdentityContext()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        // Act
        services.AddRequestsContext(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IRequestContext? context = provider.GetService<IRequestContext>();
        context.Should().NotBeNull();
    }

    [Fact]
    public void AddRequestIdentityContext_RegistersIRequestIdentityContextWriter()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        // Act
        services.AddRequestsContext(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IRequestContextWriter? writer = provider.GetService<IRequestContextWriter>();
        writer.Should().NotBeNull();
    }

    [Fact]
    public void AddRequestIdentityContext_RegistersRequestIdentityContext()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        // Act
        services.AddRequestsContext(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        RequestContext? context = provider.GetService<RequestContext>();
        context.Should().NotBeNull();
    }

    [Fact]
    public void AddRequestIdentityContext_ServicesAreScoped_SameInstanceWithinScope()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();
        services.AddRequestsContext(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        using IServiceScope scope = provider.CreateScope();
        IRequestContext context1 = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        IRequestContext context2 = scope.ServiceProvider.GetRequiredService<IRequestContext>();

        // Assert
        context1.Should().BeSameAs(context2, "Same instance should be returned within the same scope");
    }

    [Fact]
    public void AddRequestIdentityContext_ServicesAreScoped_DifferentInstancesAcrossScopes()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();
        services.AddRequestsContext(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IRequestContext context1;
        IRequestContext context2;

        using (IServiceScope scope1 = provider.CreateScope())
        {
            context1 = scope1.ServiceProvider.GetRequiredService<IRequestContext>();
        }

        using (IServiceScope scope2 = provider.CreateScope())
        {
            context2 = scope2.ServiceProvider.GetRequiredService<IRequestContext>();
        }

        // Assert
        context1.Should().NotBeSameAs(context2, "Different instances should be returned across different scopes");
    }

    [Fact]
    public void AddRequestIdentityContext_ContextAndWriterAreSameInstance()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();
        services.AddRequestsContext(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        using IServiceScope scope = provider.CreateScope();
        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();

        // Assert
        context.Should().BeSameAs(writer, "IRequestContext and IRequestContextWriter should resolve to the same instance");
    }

    [Fact]
    public void AddRequestIdentityContext_WriterModifiesContextValues()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();
        services.AddRequestsContext(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        using IServiceScope scope = provider.CreateScope();
        IRequestContext context = scope.ServiceProvider.GetRequiredService<IRequestContext>();
        IRequestContextWriter writer = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();

        writer.SetContext(
            authority: "https://auth.test.com",
            clientId: "test-client",
            ownerId: "test-owner",
            tenantId: "test-tenant",
            userId: "test-user");

        // Assert
        context.Authority.Should().Be("https://auth.test.com");
        context.ClientId.Should().Be("test-client");
        context.OwnerId.Should().Be("test-owner");
        context.TenantId.Should().Be("test-tenant");
        context.UserId.Should().Be("test-user");
    }
}
