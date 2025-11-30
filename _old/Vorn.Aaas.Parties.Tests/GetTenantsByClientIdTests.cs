namespace Vorn.Aaas.Parties.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;
using Vorn.Aaas.Parties.Application.Queries;

public class GetTenantsByClientIdTests
{
    [Fact]
    public async Task GetTenantsByClientId_ReturnsTenantsForSpecificClient()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());

        var ownerId = Guid.NewGuid();
        var client1Tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 1", ClientId = "client-1" },
            new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 2", ClientId = "client-1" }
        };

        var client2Tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 3", ClientId = "client-2" }
        };

        db.Tenants.AddRange(client1Tenants);
        db.Tenants.AddRange(client2Tenants);
        await db.SaveChangesAsync();

        var handler = new GetTenantsByClientIdHandler(db);

        // Act
        var result = await handler.Handle(new GetTenantsByClientId("client-1"), default);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, t => Assert.Equal("client-1", t.ClientId));
    }

    [Fact]
    public async Task GetTenantsByClientId_ReturnsEmptyForNonExistentClient()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());

        var ownerId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 1", ClientId = "client-1" });
        await db.SaveChangesAsync();

        var handler = new GetTenantsByClientIdHandler(db);

        // Act
        var result = await handler.Handle(new GetTenantsByClientId("non-existent-client"), default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTenantsByClientId_OnlyReturnsTenantsForSpecifiedClient()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());

        var ownerId = Guid.NewGuid();
        db.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 1", ClientId = "client-1" },
            new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 2", ClientId = "client-2" },
            new Tenant { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Tenant 3", ClientId = "client-3" }
        );
        await db.SaveChangesAsync();

        var handler = new GetTenantsByClientIdHandler(db);

        // Act
        var result = await handler.Handle(new GetTenantsByClientId("client-2"), default);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("client-2", resultList[0].ClientId);
        Assert.Equal("Tenant 2", resultList[0].Name);
    }
}
