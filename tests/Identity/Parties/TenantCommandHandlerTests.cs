namespace Vitarish.Parties.Tests;

using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Handlers;
using Vitarish.Parties.Tests.TestHelpers;

public class TenantCommandHandlerTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;
    private readonly RecordingPublisher _publisher;

    public TenantCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _publisher = new RecordingPublisher();
        _dbContext = new PartiesDbContext(options, _publisher);
    }

    [Fact]
    public async Task CreateTenant_WithExistingOwner_CreatesTenantAndPublishesEvent()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };
        _dbContext.Owners.Add(owner);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateTenantHandler(_dbContext, _publisher);
        var command = new CreateTenant(owner.Id, "Tenant", "client-id", CreatedBy: "creator");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(owner.Id, result.OwnerId);
        Assert.Equal("Tenant", result.Name);
        Assert.Equal("client-id", result.ClientId);
        Assert.Equal("creator", result.CreatedBy);
        Assert.Contains(_publisher.Notifications, notification =>
            notification is TenantCreatedEvent createdEvent && createdEvent.TenantId == result.Id && createdEvent.OwnerId == owner.Id);
    }

    [Fact]
    public async Task CreateTenant_WithMissingOwner_ReturnsNull()
    {
        var handler = new CreateTenantHandler(_dbContext, _publisher);
        var command = new CreateTenant(Guid.NewGuid(), "Tenant", "client-id");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(_publisher.Notifications);
    }

    [Fact]
    public async Task UpdateTenant_UpdatesTenantAndOwner()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };
        var newOwner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "New Owner"
        };
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            Name = "Tenant",
            ClientId = "client-id"
        };
        _dbContext.Owners.AddRange(owner, newOwner);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateTenantHandler(_dbContext);
        var command = new UpdateTenant(tenant.Id, newOwner.Id, "Updated", "updated-client", "logo", "desc", "modifier");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newOwner.Id, result.OwnerId);
        Assert.Equal("Updated", result.Name);
        Assert.Equal("updated-client", result.ClientId);
        Assert.Equal("logo", result.LogoUrl);
        Assert.Equal("desc", result.Description);
        Assert.Equal("modifier", result.ModifiedBy);
        Assert.NotNull(result.ModifiedAt);
    }

    [Fact]
    public async Task UpdateTenant_ReturnsNullWhenOwnerMissing()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            Name = "Tenant",
            ClientId = "client-id"
        };
        _dbContext.Owners.Add(owner);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateTenantHandler(_dbContext);
        var command = new UpdateTenant(tenant.Id, Guid.NewGuid(), "Updated", "client-id");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteTenant_RemovesTenant()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant",
            ClientId = "client-id"
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteTenantHandler(_dbContext);
        var command = new DeleteTenant(tenant.Id);

        await handler.Handle(command, CancellationToken.None);

        Tenant? deleted = await _dbContext.Tenants.FindAsync(tenant.Id);
        Assert.Null(deleted);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
