using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.ApiClients.Hosting.DependencyInjection;
using Vitarish.ApiClients.Infrastructure.Data;

namespace Vitarish.ApiClients.Tests.Hosting;

/// <summary>
/// Integration tests for the <see cref="ApiClientsWebApplicationBuilderExtensions"/>.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddAaasApiClients_RegistersDbContext()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasApiClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert
        var dbContext = app.Services.GetService<ApiClientDbContext>();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasApiClients_RegistersApiClientsService()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasApiClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert
        var service = app.Services.GetService<IApiClientsService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasApiClients_RegistersMediatR()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasApiClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert
        var mediator = app.Services.GetService<MediatR.IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasApiClients_RegistersValidators()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasApiClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert - Check for specific validator types
        var createValidator = app.Services.GetService<FluentValidation.IValidator<CreateApiClient>>();
        var updateValidator = app.Services.GetService<FluentValidation.IValidator<UpdateApiClient>>();
        var deleteValidator = app.Services.GetService<FluentValidation.IValidator<DeleteApiClient>>();
        var getValidator = app.Services.GetService<FluentValidation.IValidator<GetApiClient>>();

        createValidator.Should().NotBeNull();
        updateValidator.Should().NotBeNull();
        deleteValidator.Should().NotBeNull();
        getValidator.Should().NotBeNull();
    }

    [Fact]
    public void AddAaasApiClients_ServiceIsTransient()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddAaasApiClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Assert - Get two instances and verify they are different (transient)
        using var scope1 = app.Services.CreateScope();
        using var scope2 = app.Services.CreateScope();

        var service1 = scope1.ServiceProvider.GetService<IApiClientsService>();
        var service2 = scope2.ServiceProvider.GetService<IApiClientsService>();

        service1.Should().NotBeSameAs(service2);
    }

    [Fact]
    public void AddAaasApiClients_DbContextCanBeUsed()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.AddAaasApiClients(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var app = builder.Build();

        // Act
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiClientDbContext>();

        var client = new ApiClient
        {
            Id = "test-client",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        dbContext.ApiClients.Add(client);
        var result = dbContext.SaveChanges();

        // Assert
        result.Should().Be(1);
        dbContext.ApiClients.Should().HaveCount(1);
    }
}
