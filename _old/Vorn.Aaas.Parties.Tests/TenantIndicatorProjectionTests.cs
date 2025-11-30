namespace Vorn.Aaas.Parties.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.Parties.Application.EventHandlers;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Domain.Events;
using Vorn.Aaas.Parties.Infrastructure.Extensions;
using Vorn.Aaas.Parties.Tests.TestHelpers;

/// <summary>
/// Tests for tenant indicator projection to authentication clients.
/// </summary>
public class TenantIndicatorProjectionTests : IDisposable
{
    private readonly PartiesDbContext _partiesDb;
    private readonly AuthenticationClientDbContext _authDb;
    private readonly NoOpPublisher _publisher;

    public TenantIndicatorProjectionTests()
    {
        var partiesOptions = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var authOptions = new DbContextOptionsBuilder<AuthenticationClientDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _publisher = new NoOpPublisher();
        _partiesDb = new PartiesDbContext(partiesOptions, _publisher);
        _authDb = new AuthenticationClientDbContext(authOptions);
    }

    [Fact]
    public async Task CreatedEventHandler_CreatesAuthenticationClient()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
            LogoUrl = "https://example.com/logo.png"
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            Method = IndicatorMethod.Hostname,
            Value = "app.example.com",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        var handler = new TenantIndicatorCreatedEventHandler(
            _partiesDb, _authDb, NullLogger<TenantIndicatorCreatedEventHandler>.Instance);
        var ev = new TenantIndicatorCreatedEvent(tenant.Id, indicator.Id);

        // Act
        await handler.Handle(ev, CancellationToken.None);

        // Assert
        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = await _authDb.AuthenticationClients.FindAsync(clientId);
        
        Assert.NotNull(client);
        Assert.Equal("app.example.com", client.Url);
        Assert.Equal("Test Tenant (primary)", client.Title);
        Assert.Contains("Tenant=", client.Description);
        Assert.Equal("https://example.com/logo.png", client.LogoUrl);
        Assert.True(client.AutoAccess);
    }

    [Fact]
    public async Task CreatedEventHandler_SetsCorrectTitle_ForNonPrimary()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            Method = IndicatorMethod.Subdomain,
            Value = "test.myapp.com",
            Status = IndicatorStatus.Verified,
            IsPrimary = false
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        var handler = new TenantIndicatorCreatedEventHandler(
            _partiesDb, _authDb, NullLogger<TenantIndicatorCreatedEventHandler>.Instance);
        var ev = new TenantIndicatorCreatedEvent(tenant.Id, indicator.Id);

        // Act
        await handler.Handle(ev, CancellationToken.None);

        // Assert
        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = await _authDb.AuthenticationClients.FindAsync(clientId);
        
        Assert.NotNull(client);
        Assert.Equal("Test Tenant (subdomain)", client.Title);
    }

    [Fact]
    public async Task CreatedEventHandler_SetsAutoAccessFalse_ForNonActiveIndicator()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            Method = IndicatorMethod.Hostname,
            Value = "pending.example.com",
            Status = IndicatorStatus.PendingVerification,
            IsPrimary = false
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        var handler = new TenantIndicatorCreatedEventHandler(
            _partiesDb, _authDb, NullLogger<TenantIndicatorCreatedEventHandler>.Instance);
        var ev = new TenantIndicatorCreatedEvent(tenant.Id, indicator.Id);

        // Act
        await handler.Handle(ev, CancellationToken.None);

        // Assert
        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = await _authDb.AuthenticationClients.FindAsync(clientId);
        
        Assert.NotNull(client);
        Assert.False(client.AutoAccess);
    }

    [Fact]
    public async Task UpdatedEventHandler_UpdatesExistingClient()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            Method = IndicatorMethod.Hostname,
            Value = "app.example.com",
            Status = IndicatorStatus.Verified,
            IsPrimary = false
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        // Create initial client
        var createHandler = new TenantIndicatorCreatedEventHandler(
            _partiesDb, _authDb, NullLogger<TenantIndicatorCreatedEventHandler>.Instance);
        await createHandler.Handle(new TenantIndicatorCreatedEvent(tenant.Id, indicator.Id), CancellationToken.None);

        // Update indicator
        indicator.Status = IndicatorStatus.Active;
        await _partiesDb.SaveChangesAsync();

        var updateHandler = new TenantIndicatorUpdatedEventHandler(
            _partiesDb, _authDb, NullLogger<TenantIndicatorUpdatedEventHandler>.Instance);
        var ev = new TenantIndicatorUpdatedEvent(tenant.Id, indicator.Id);

        // Act
        await updateHandler.Handle(ev, CancellationToken.None);

        // Assert
        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = await _authDb.AuthenticationClients.FindAsync(clientId);
        
        Assert.NotNull(client);
        Assert.True(client.AutoAccess); // Should be true now that status is Active
    }

    [Fact]
    public async Task DeletedEventHandler_RemovesAuthenticationClient()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            Method = IndicatorMethod.Hostname,
            Value = "app.example.com",
            Status = IndicatorStatus.Active,
            IsPrimary = false
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        // Create client
        var createHandler = new TenantIndicatorCreatedEventHandler(
            _partiesDb, _authDb, NullLogger<TenantIndicatorCreatedEventHandler>.Instance);
        await createHandler.Handle(new TenantIndicatorCreatedEvent(tenant.Id, indicator.Id), CancellationToken.None);

        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        Assert.NotNull(await _authDb.AuthenticationClients.FindAsync(clientId));

        // Delete
        var deleteHandler = new TenantIndicatorDeletedEventHandler(
            _authDb, NullLogger<TenantIndicatorDeletedEventHandler>.Instance);
        var ev = new TenantIndicatorDeletedEvent(tenant.Id, indicator.Id, indicator.Method, indicator.Value);

        // Act
        await deleteHandler.Handle(ev, CancellationToken.None);

        // Assert
        var client = await _authDb.AuthenticationClients.FindAsync(clientId);
        Assert.Null(client);
    }

    public void Dispose()
    {
        _partiesDb?.Dispose();
        _authDb?.Dispose();
    }
}
