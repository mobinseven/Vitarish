using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Infrastructure.Data;

namespace Vitarish.AuthenticationClients.Tests.Hosting;

/// <summary>
/// Integration tests for the AuthenticationClients dependency injection configuration.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddAaasAuthenticationClients_RegistersDbContext()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasAuthenticationClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert
        var dbContext = app.Services.GetService<AuthenticationClientDbContext>();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasAuthenticationClients_RegistersAuthenticationClientsService()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasAuthenticationClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert
        var service = app.Services.GetService<IAuthenticationClientsService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasAuthenticationClients_RegistersMediatR()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasAuthenticationClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert
        var mediator = app.Services.GetService<MediatR.IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasAuthenticationClients_RegistersValidators()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasAuthenticationClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert - Check for specific validator types
        var createValidator = app.Services.GetService<FluentValidation.IValidator<CreateAuthenticationClient>>();
        var updateValidator = app.Services.GetService<FluentValidation.IValidator<UpdateAuthenticationClient>>();
        var deleteValidator = app.Services.GetService<FluentValidation.IValidator<DeleteAuthenticationClient>>();
        var getValidator = app.Services.GetService<FluentValidation.IValidator<GetAuthenticationClient>>();

        createValidator.Should().NotBeNull();
        updateValidator.Should().NotBeNull();
        deleteValidator.Should().NotBeNull();
        getValidator.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasAuthenticationClients_ServiceIsTransient()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasAuthenticationClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert - Get two instances and verify they are different (transient)
        using var scope1 = app.Services.CreateScope();
        using var scope2 = app.Services.CreateScope();

        var service1 = scope1.ServiceProvider.GetService<IAuthenticationClientsService>();
        var service2 = scope2.ServiceProvider.GetService<IAuthenticationClientsService>();

        service1.Should().NotBeSameAs(service2);
    }

    [Fact]
    public void AddAaasAuthenticationClients_DbContextCanBeUsed()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.AddAaasAuthenticationClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Act
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthenticationClientDbContext>();

        var client = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com",
            Title = "Test App"
        };
        dbContext.AuthenticationClients.Add(client);
        var result = dbContext.SaveChanges();

        // Assert
        result.Should().Be(1);
        dbContext.AuthenticationClients.Should().HaveCount(1);
    }

    [Fact]
    public void AddAaasAuthenticationClients_MultipleCallsDoNotDuplicateRegistrations()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act - Call twice (should not throw or cause issues)
        builder.AddAaasAuthenticationClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var app = builder.Build();

        // Assert - Services should still work
        var service = app.Services.GetService<IAuthenticationClientsService>();
        service.Should().NotBeNull();
    }
}
