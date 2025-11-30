namespace Vorn.Aaas.Parties.Tests;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vorn.Aaas.Parties.Application.Services;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Data;
using Xunit;

public class TenantsServiceTests
{
    [Fact]
    public async Task ResolveFromHostAsync_WithActiveHostname_ReturnsTenant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
        
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            LogoUrl = "https://example.com/logo.png",
            ClientId = "test-client-id"
        };

        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.example.test",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };

        tenant.Indicators.Add(indicator);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("app.example.test");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act
        var result = await service.ResolveFromHostAsync(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal("Test Tenant", result.Name);
    }

    [Fact]
    public async Task ResolveFromHostAsync_WithActiveSubdomain_ReturnsTenant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
        
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
            Method = IndicatorMethod.Subdomain,
            Value = "customer.app.example.test",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };

        tenant.Indicators.Add(indicator);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("customer.app.example.test");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act
        var result = await service.ResolveFromHostAsync(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task ResolveFromHostAsync_WithNonActiveIndicator_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
        
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
            Method = IndicatorMethod.Hostname,
            Value = "app.example.test",
            Status = IndicatorStatus.PendingVerification,
            IsPrimary = true
        };

        tenant.Indicators.Add(indicator);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("app.example.test");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act
        var result = await service.ResolveFromHostAsync(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_WithUnknownHost_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("unknown.example.test");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act
        var result = await service.ResolveFromHostAsync(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_NormalizesHostname()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
        
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };

        // Store normalized value
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.example.test",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };

        tenant.Indicators.Add(indicator);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        // Request with uppercase and port
        httpContext.Request.Host = new HostString("APP.EXAMPLE.TEST:5001");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act
        var result = await service.ResolveFromHostAsync(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task ResolveFromHostAsync_WithEmptyHost_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act
        var result = await service.ResolveFromHostAsync(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_WithMultipleMatchingTenants_ThrowsException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
        
        // This shouldn't happen with DB constraints, but test the guard
        var tenant1 = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant 1",
            ClientId = "test-client",
        };

        var indicator1 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant1.Id,
            Method = IndicatorMethod.Hostname,
            Value = "shared.example.test",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };

        tenant1.Indicators.Add(indicator1);
        db.Tenants.Add(tenant1);
        
        // Simulate constraint violation by adding second tenant with same hostname
        // (In real DB this would be prevented by unique constraint)
        var tenant2 = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant 2",
            ClientId = "test-client",
        };

        var indicator2 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant2.Id,
            Method = IndicatorMethod.Hostname,
            Value = "shared.example.test",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };

        tenant2.Indicators.Add(indicator2);
        db.Tenants.Add(tenant2);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("shared.example.test");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveFromHostAsync(httpContext));
    }

    [Fact]
    public async Task ResolveFromHostAsync_WithPunycodeHost_ResolvesProperly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
        
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };

        // Store as punycode
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "xn--80akhbyknj4f.test", // Punycode for испытание.test
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };

        tenant.Indicators.Add(indicator);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        // Request with punycode
        httpContext.Request.Host = new HostString("xn--80akhbyknj4f.test");

        var service = new TenantsService(db, NullLogger<TenantsService>.Instance);

        // Act
        var result = await service.ResolveFromHostAsync(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }
}
