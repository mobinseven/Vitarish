namespace Vorn.Aaas.Parties.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.Parties.Application.Commands;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Extensions;
using Vorn.Aaas.Parties.Tests.TestHelpers;

/// <summary>
/// Tests for ResyncAuthenticationClients command.
/// </summary>
public class ResyncAuthenticationClientsTests : IDisposable
{
    private readonly PartiesDbContext _partiesDb;
    private readonly AuthenticationClientDbContext _authDb;
    private readonly NoOpPublisher _publisher;

    public ResyncAuthenticationClientsTests()
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
    public async Task Resync_CreatesNewClients()
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
            IsPrimary = true
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        var handler = new ResyncAuthenticationClientsHandler(
            _partiesDb, _authDb, NullLogger<ResyncAuthenticationClientsHandler>.Instance);
        var command = new ResyncAuthenticationClients(null, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Created);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Deleted);
        Assert.Equal(0, result.Unchanged);

        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = await _authDb.AuthenticationClients.FindAsync(clientId);
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Resync_UpdatesExistingClients()
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
            IsPrimary = true
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        // Create client with outdated info
        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = new AuthenticationClient
        {
            Id = clientId,
            Url = "old.example.com",
            Title = "Old Title",
            Description = "Old Description",
            AutoAccess = false
        };
        _authDb.AuthenticationClients.Add(client);
        await _authDb.SaveChangesAsync();

        var handler = new ResyncAuthenticationClientsHandler(
            _partiesDb, _authDb, NullLogger<ResyncAuthenticationClientsHandler>.Instance);
        var command = new ResyncAuthenticationClients(null, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Deleted);

        var updated = await _authDb.AuthenticationClients.FindAsync(clientId);
        Assert.NotNull(updated);
        Assert.Equal("app.example.com", updated.Url);
        Assert.True(updated.AutoAccess);
    }

    [Fact]
    public async Task Resync_DeletesOrphanedClients()
    {
        // Arrange - create orphaned client
        var orphanClient = new AuthenticationClient
        {
            Id = "ac:orphanedclient123456789012345",
            Url = "orphan.example.com",
            Title = "Orphaned",
            Description = "Should be deleted"
        };
        _authDb.AuthenticationClients.Add(orphanClient);
        await _authDb.SaveChangesAsync();

        var handler = new ResyncAuthenticationClientsHandler(
            _partiesDb, _authDb, NullLogger<ResyncAuthenticationClientsHandler>.Instance);
        var command = new ResyncAuthenticationClients(null, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Deleted);

        var deleted = await _authDb.AuthenticationClients.FindAsync(orphanClient.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Resync_DryRun_DoesNotModifyDatabase()
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

        var handler = new ResyncAuthenticationClientsHandler(
            _partiesDb, _authDb, NullLogger<ResyncAuthenticationClientsHandler>.Instance);
        var command = new ResyncAuthenticationClients(null, true); // DryRun = true

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Created);

        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = await _authDb.AuthenticationClients.FindAsync(clientId);
        Assert.Null(client); // Should not be created in dry run
    }

    [Fact]
    public async Task Resync_FiltersByTenantId()
    {
        // Arrange
        var tenant1 = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant 1",
            ClientId = "test-client",
        };
        var tenant2 = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant 2",
            ClientId = "test-client",
        };
        var indicator1 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant1.Id,
            Tenant = tenant1,
            Method = IndicatorMethod.Hostname,
            Value = "tenant1.example.com",
            Status = IndicatorStatus.Active,
            IsPrimary = false
        };
        var indicator2 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant2.Id,
            Tenant = tenant2,
            Method = IndicatorMethod.Hostname,
            Value = "tenant2.example.com",
            Status = IndicatorStatus.Active,
            IsPrimary = false
        };
        _partiesDb.Tenants.Add(tenant1);
        _partiesDb.Tenants.Add(tenant2);
        _partiesDb.TenantIndicators.Add(indicator1);
        _partiesDb.TenantIndicators.Add(indicator2);
        await _partiesDb.SaveChangesAsync();

        var handler = new ResyncAuthenticationClientsHandler(
            _partiesDb, _authDb, NullLogger<ResyncAuthenticationClientsHandler>.Instance);
        var command = new ResyncAuthenticationClients(tenant1.Id, false); // Only tenant1

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Created);

        var clientId1 = AuthClientIdGenerator.Generate(tenant1.Id, indicator1.Method, indicator1.Value);
        var clientId2 = AuthClientIdGenerator.Generate(tenant2.Id, indicator2.Method, indicator2.Value);
        
        Assert.NotNull(await _authDb.AuthenticationClients.FindAsync(clientId1));
        Assert.Null(await _authDb.AuthenticationClients.FindAsync(clientId2));
    }

    [Fact]
    public async Task Resync_ReportsUnchanged()
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
            IsPrimary = true
        };
        _partiesDb.Tenants.Add(tenant);
        _partiesDb.TenantIndicators.Add(indicator);
        await _partiesDb.SaveChangesAsync();

        // Create client that matches perfectly
        var clientId = AuthClientIdGenerator.Generate(tenant.Id, indicator.Method, indicator.Value);
        var client = new AuthenticationClient
        {
            Id = clientId,
            Url = indicator.Value,
            Title = $"{tenant.Name} (primary)",
            Description = $"Tenant={tenant.Id} Indicator={indicator.Method}:{indicator.Value} Status={indicator.Status}",
            LogoUrl = tenant.LogoUrl,
            AutoAccess = true
        };
        _authDb.AuthenticationClients.Add(client);
        await _authDb.SaveChangesAsync();

        var handler = new ResyncAuthenticationClientsHandler(
            _partiesDb, _authDb, NullLogger<ResyncAuthenticationClientsHandler>.Instance);
        var command = new ResyncAuthenticationClients(null, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Created);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.Unchanged);
    }

    public void Dispose()
    {
        _partiesDb?.Dispose();
        _authDb?.Dispose();
    }
}
