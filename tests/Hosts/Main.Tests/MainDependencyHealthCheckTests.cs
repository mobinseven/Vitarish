using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using StackExchange.Redis;
using Vitarish.Audit.Server;
using Vitarish.Main.Infrastructure.Health;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Main.Tests;

public sealed class MainDependencyHealthCheckTests
{
    [Fact]
    public async Task Readiness_is_healthy_when_database_and_redis_are_reachable()
    {
        Mock<IDbContextFactory<AuditDbContext>> dbFactory = DatabaseFactory();
        Mock<IConnectionMultiplexer> redis = Redis(isConnected: true);

        HealthCheckResult result = await new MainDependencyHealthCheck(dbFactory.Object, redis.Object)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public async Task Readiness_is_unhealthy_when_database_probe_fails()
    {
        Mock<IDbContextFactory<AuditDbContext>> dbFactory = new();
        _ = dbFactory.Setup(factory => factory.CreateDbContext()).Throws<InvalidOperationException>();
        Mock<IConnectionMultiplexer> redis = Redis(isConnected: true);

        HealthCheckResult result = await new MainDependencyHealthCheck(dbFactory.Object, redis.Object)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeNull();
        result.Description.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Readiness_is_unhealthy_when_redis_is_not_connected()
    {
        Mock<IDbContextFactory<AuditDbContext>> dbFactory = DatabaseFactory();
        Mock<IConnectionMultiplexer> redis = Redis(isConnected: false);

        HealthCheckResult result = await new MainDependencyHealthCheck(dbFactory.Object, redis.Object)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeNull();
    }

    private static Mock<IDbContextFactory<AuditDbContext>> DatabaseFactory()
    {
        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        Mock<IRequestContext> requestContext = new();
        Mock<IDbContextFactory<AuditDbContext>> factory = new();
        _ = factory.Setup(value => value.CreateDbContext())
            .Returns(() => new AuditDbContext(options, requestContext.Object));
        return factory;
    }

    private static Mock<IConnectionMultiplexer> Redis(bool isConnected)
    {
        Mock<IDatabase> database = new();
        _ = database.Setup(value => value.PingAsync(CommandFlags.None)).ReturnsAsync(TimeSpan.Zero);

        Mock<IConnectionMultiplexer> redis = new();
        _ = redis.SetupGet(value => value.IsConnected).Returns(isConnected);
        _ = redis.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(database.Object);
        return redis;
    }
}
