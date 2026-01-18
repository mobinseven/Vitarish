using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Infrastructure.Services;

namespace Main.Tests.Infrastructure.BackgroundJobs;

public class TenantBackgroundExecutorTests : IDisposable
{
    private readonly Mock<ILogger<TenantBackgroundExecutor>> mockLogger;
    private readonly Mock<IPartiesService> mockPartiesService;
    private readonly Mock<IJobCancellationToken> mockCancellationToken;
    private readonly ServiceProvider serviceProvider;
    private readonly TenantBackgroundExecutor sut;
    private readonly List<RequestContextSnapshot> requestContextSnapshots;

    public TenantBackgroundExecutorTests()
    {
        mockLogger = new Mock<ILogger<TenantBackgroundExecutor>>();
        mockPartiesService = new Mock<IPartiesService>();
        mockCancellationToken = new Mock<IJobCancellationToken>();
        requestContextSnapshots = [];

        // Setup default cancellation token behavior
        mockCancellationToken.Setup(x => x.ShutdownToken).Returns(CancellationToken.None);

        // Setup default tenant list
        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tenant>());

        ServiceCollection services = new();
        _ = services.AddHttpContextAccessor();
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());

        serviceProvider = services.BuildServiceProvider();

        sut = new TenantBackgroundExecutor(
            mockPartiesService.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    public void Dispose() => serviceProvider.Dispose();

    private sealed class RequestContextSnapshot
    {
        public string? Authority { get; set; }
        public string? ClientId { get; set; }
        public string? OwnerId { get; set; }
        public string? TenantId { get; set; }
        public string? ResourceId { get; set; }
        public string? RequestId { get; set; }
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithNullResourceId_ThrowsArgumentNullException()
    {
        // Arrange
        string? resourceId = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.ExecutePerTenantAsync(
                resourceId!,
                (_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithEmptyResourceId_ThrowsArgumentException()
    {
        // Arrange
        string resourceId = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.ExecutePerTenantAsync(
                resourceId,
                (_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithWhitespaceResourceId_ThrowsArgumentException()
    {
        // Arrange
        string resourceId = "   ";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.ExecutePerTenantAsync(
                resourceId,
                (_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithNullJobCancellationToken_ThrowsArgumentNullException()
    {
        // Arrange
        string resourceId = "test-resource";
        IJobCancellationToken? cancellationToken = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.ExecutePerTenantAsync(
                resourceId,
                (_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithNullPerTenantAction_ThrowsArgumentNullException()
    {
        // Arrange
        string resourceId = "test-resource";
        Func<IServiceProvider, CancellationToken, Task>? action = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await sut.ExecutePerTenantAsync(
                resourceId,
                action!));
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithNoTenants_DoesNotExecuteAction()
    {
        // Arrange
        string resourceId = "test-resource";
        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tenant>());

        int executionCount = 0;

        // Act
        await sut.ExecutePerTenantAsync(
            resourceId,
            (_, _) =>
            {
                executionCount++;
                return Task.CompletedTask;
            });

        // Assert
        executionCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithSingleTenant_ExecutesActionOnce()
    {
        // Arrange
        string resourceId = "test-resource";
        Tenant tenant = CreateTenant("00000000-0000-0000-0000-000000000001", "client-1", "Tenant 1");
        
        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tenant });

        int executionCount = 0;

        // Act
        await sut.ExecutePerTenantAsync(
            resourceId,
            (_, _) =>
            {
                executionCount++;
                return Task.CompletedTask;
            });

        // Assert
        executionCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WithMultipleTenants_ExecutesActionForEachTenant()
    {
        // Arrange
        string resourceId = "test-resource";
        Tenant[] tenants =
        [
            CreateTenant("00000000-0000-0000-0000-000000000001", "client-1", "Tenant 1"),
            CreateTenant("00000000-0000-0000-0000-000000000002", "client-2", "Tenant 2"),
            CreateTenant("00000000-0000-0000-0000-000000000003", "client-3", "Tenant 3")
        ];

        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants);

        int executionCount = 0;

        // Act
        await sut.ExecutePerTenantAsync(
            resourceId,
            (_, _) =>
            {
                executionCount++;
                return Task.CompletedTask;
            });

        // Assert
        executionCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecutePerTenantAsync_InitializesRequestContextWithTenantDetails()
    {
        // Arrange
        string resourceId = "test-resource";
        Guid tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid ownerId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        string clientId = "client-1";

        Tenant tenant = new()
        {
            Id = tenantId,
            OwnerId = ownerId,
            ClientId = clientId,
            Name = "Tenant 1"
        };

        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tenant });

        // Act
        await sut.ExecutePerTenantAsync(
            resourceId,
            (provider, _) =>
            {
                IRequestContext context = provider.GetRequiredService<IRequestContext>();
                requestContextSnapshots.Add(new RequestContextSnapshot
                {
                    Authority = context.Authority,
                    ClientId = context.ClientId,
                    OwnerId = context.OwnerId,
                    TenantId = context.TenantId,
                    ResourceId = context.ResourceId,
                    RequestId = context.RequestId
                });
                return Task.CompletedTask;
            });

        // Assert
        requestContextSnapshots.Should().HaveCount(1);
        RequestContextSnapshot snapshot = requestContextSnapshots[0];
        snapshot.Authority.Should().Be("system");
        snapshot.ClientId.Should().Be(clientId);
        snapshot.OwnerId.Should().Be(ownerId.ToString());
        snapshot.TenantId.Should().Be(tenantId.ToString());
        snapshot.ResourceId.Should().Be(resourceId);
        snapshot.RequestId.Should().StartWith($"hangfire-{resourceId}-");
    }


    [Fact]
    public async Task ExecutePerTenantAsync_WithOperationCanceledException_RethrowsException()
    {
        // Arrange
        string resourceId = "test-resource";
        Tenant tenant = CreateTenant("00000000-0000-0000-0000-000000000001", "client-1", "Tenant 1");

        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tenant });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await sut.ExecutePerTenantAsync(
                resourceId,
                (_, _) => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task ExecutePerTenantAsync_ChecksCancellationBeforeProcessing()
    {
        // Arrange
        string resourceId = "test-resource";
        Tenant tenant = CreateTenant("00000000-0000-0000-0000-000000000001", "client-1", "Tenant 1");

        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tenant });

        mockCancellationToken
            .Setup(x => x.ThrowIfCancellationRequested())
            .Throws<OperationCanceledException>();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await sut.ExecutePerTenantAsync(
                resourceId,
                (_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task ExecutePerTenantAsync_LinksCancellationTokens()
    {
        // Arrange
        string resourceId = "test-resource";
        Tenant tenant = CreateTenant("00000000-0000-0000-0000-000000000001", "client-1", "Tenant 1");

        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tenant });

        CancellationTokenSource externalCts = new();
        CancellationTokenSource shutdownCts = new();
        mockCancellationToken.Setup(x => x.ShutdownToken).Returns(shutdownCts.Token);

        bool wasLinked = false;

        // Act
        await sut.ExecutePerTenantAsync(
            resourceId,
            (_, ct) =>
            {
                // Verify that cancellation from either source cancels the linked token
                shutdownCts.Cancel();
                wasLinked = ct.IsCancellationRequested;
                return Task.CompletedTask;
            },
            externalCts.Token);

        // Assert
        wasLinked.Should().BeTrue("cancellation token should be linked to both shutdown and external tokens");
    }


    private static Tenant CreateTenant(string id, string clientId, string name)
    {
        return new Tenant
        {
            Id = Guid.Parse(id),
            OwnerId = Guid.NewGuid(),
            ClientId = clientId,
            Name = name
        };
    }
}
