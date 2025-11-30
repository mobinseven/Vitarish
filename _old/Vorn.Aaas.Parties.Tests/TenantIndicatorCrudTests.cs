namespace Vorn.Aaas.Parties.Tests;

using Vorn.Aaas.Parties.Application.Commands;
using Vorn.Aaas.Parties.Application.Queries;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Tests.TestHelpers;

/// <summary>
/// Tests for tenant indicator CRUD operations.
/// </summary>
public class TenantIndicatorCrudTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;
    private readonly NoOpPublisher _publisher;

    public TenantIndicatorCrudTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _publisher = new NoOpPublisher();
        _dbContext = new PartiesDbContext(options, _publisher);
    }

    [Fact]
    public async Task GetTenantIndicators_ReturnsAllIndicatorsForTenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        var indicator1 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "test1.example.com",
            Status = IndicatorStatus.Active
        };
        var indicator2 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Subdomain,
            Value = "test2",
            Status = IndicatorStatus.Verified
        };
        tenant.Indicators.Add(indicator1);
        tenant.Indicators.Add(indicator2);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new GetTenantIndicatorsHandler(_dbContext);
        var query = new GetTenantIndicators(tenant.Id);

        // Act
        IEnumerable<TenantIndicator> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, i => i.Id == indicator1.Id);
        Assert.Contains(result, i => i.Id == indicator2.Id);
    }

    [Fact]
    public async Task GetTenantIndicators_ReturnsEmptyForNonExistentTenant()
    {
        // Arrange
        var handler = new GetTenantIndicatorsHandler(_dbContext);
        var query = new GetTenantIndicators(Guid.NewGuid());

        // Act
        IEnumerable<TenantIndicator> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteTenantIndicator_RemovesIndicatorSuccessfully()
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
            Method = IndicatorMethod.Hostname,
            Value = "test.example.com",
            Status = IndicatorStatus.Active
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteTenantIndicatorHandler(_dbContext, _publisher);
        var command = new DeleteTenantIndicator(tenant.Id, indicator.Id);

        // Act
        bool result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        TenantIndicator? deleted = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteTenantIndicator_ReturnsFalseForNonExistentIndicator()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteTenantIndicatorHandler(_dbContext, _publisher);
        var command = new DeleteTenantIndicator(tenant.Id, Guid.NewGuid());

        // Act
        bool result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTenantIndicator_ReturnsFalseForWrongTenant()
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
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant1.Id,
            Method = IndicatorMethod.Hostname,
            Value = "test.example.com",
            Status = IndicatorStatus.Active
        };
        tenant1.Indicators.Add(indicator);
        _dbContext.Tenants.AddRange(tenant1, tenant2);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteTenantIndicatorHandler(_dbContext, _publisher);
        var command = new DeleteTenantIndicator(tenant2.Id, indicator.Id);

        // Act
        bool result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
        TenantIndicator? stillExists = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(stillExists);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
