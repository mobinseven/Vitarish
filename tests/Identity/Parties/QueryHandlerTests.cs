namespace Vitarish.Parties.Tests;

using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Handlers;

public class QueryHandlerTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;

    public QueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
    }

    [Fact]
    public async Task GetTenant_ReturnsTenantWhenFound()
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

        var handler = new GetTenantHandler(_dbContext);

        var result = await handler.Handle(new GetTenant(tenant.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task GetTenant_ReturnsNullWhenMissing()
    {
        var handler = new GetTenantHandler(_dbContext);

        var result = await handler.Handle(new GetTenant(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTenants_ReturnsAllTenants()
    {
        _dbContext.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = "Tenant 1", ClientId = "client-1" },
            new Tenant { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = "Tenant 2", ClientId = "client-2" });
        await _dbContext.SaveChangesAsync();

        var handler = new GetTenantsHandler(_dbContext);

        var result = await handler.Handle(new GetTenants(), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetTenantsByOwner_ReturnsTenantSubset()
    {
        var ownerId = Guid.NewGuid();
        _dbContext.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 1", ClientId = "client-1" },
            new Tenant { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = "Tenant 2", ClientId = "client-2" });
        await _dbContext.SaveChangesAsync();

        var handler = new GetTenantsByOwnerHandler(_dbContext);

        var result = await handler.Handle(new GetTenantsByOwner(ownerId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(ownerId, result.First().OwnerId);
    }

    [Fact]
    public async Task GetTenantsByOwner_ReturnsEmptyWhenNoMatches()
    {
        var handler = new GetTenantsByOwnerHandler(_dbContext);

        var result = await handler.Handle(new GetTenantsByOwner(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOwner_ReturnsOwnerWhenFound()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };
        _dbContext.Owners.Add(owner);
        await _dbContext.SaveChangesAsync();

        var handler = new GetOwnerHandler(_dbContext);

        var result = await handler.Handle(new GetOwner(owner.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(owner.Id, result.Id);
    }

    [Fact]
    public async Task GetOwner_ReturnsNullWhenMissing()
    {
        var handler = new GetOwnerHandler(_dbContext);

        var result = await handler.Handle(new GetOwner(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOwners_ReturnsAllOwners()
    {
        _dbContext.Owners.AddRange(
            new Owner { Id = Guid.NewGuid(), Name = "Owner 1" },
            new Owner { Id = Guid.NewGuid(), Name = "Owner 2" });
        await _dbContext.SaveChangesAsync();

        var handler = new GetOwnersHandler(_dbContext);

        var result = await handler.Handle(new GetOwners(), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
