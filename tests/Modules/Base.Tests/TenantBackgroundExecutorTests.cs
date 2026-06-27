using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base.BackgroundJobs;

namespace Modules.Base.Tests;

public class TenantBackgroundExecutorTests
{
    [Fact]
    public async Task ExecutePerTenantAsync_UsesBaseContractsForTenantScopedWork()
    {
        Guid tenantId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        BackgroundTenant tenant = new(tenantId, ownerId, "app-id");
        CapturingTenantSource tenantSource = new([tenant]);
        CapturingBackgroundRequestContextWriter contextWriter = new();
        ServiceCollection services = new();
        _ = services.AddScoped<IBackgroundRequestContextWriter>(_ => contextWriter);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        TenantBackgroundExecutor executor = new(tenantSource, serviceProvider.GetRequiredService<IServiceScopeFactory>());
        List<IServiceProvider> scopedProviders = [];

        await executor.ExecutePerTenantAsync(
            "resource-id",
            (scopeProvider, cancellationToken) =>
            {
                cancellationToken.IsCancellationRequested.Should().BeFalse();
                scopedProviders.Add(scopeProvider);
                return Task.CompletedTask;
            });

        tenantSource.RequestedCancellationToken.Should().Be(CancellationToken.None);
        scopedProviders.Should().ContainSingle();
        contextWriter.Contexts.Should().ContainSingle();
        BackgroundRequestContext context = contextWriter.Contexts.Single();
        context.Authority.Should().Be("system");
        context.ClientId.Should().Be("app-id");
        context.OwnerId.Should().Be(ownerId.ToString());
        context.TenantId.Should().Be(tenantId.ToString());
        context.ResourceId.Should().Be("resource-id");
        context.RequestId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecutePerTenantAsync_WhenTenantSourceReturnsNull_CompletesWithoutExecutingWork()
    {
        NullTenantSource tenantSource = new();
        CapturingBackgroundRequestContextWriter contextWriter = new();
        ServiceCollection services = new();
        _ = services.AddScoped<IBackgroundRequestContextWriter>(_ => contextWriter);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        TenantBackgroundExecutor executor = new(tenantSource, serviceProvider.GetRequiredService<IServiceScopeFactory>());
        bool workExecuted = false;

        await executor.ExecutePerTenantAsync(
            "resource-id",
            (_, _) =>
            {
                workExecuted = true;
                return Task.CompletedTask;
            });

        workExecuted.Should().BeFalse();
        contextWriter.Contexts.Should().BeEmpty();
    }

    private sealed class CapturingTenantSource(IReadOnlyList<BackgroundTenant> tenants) : IBackgroundTenantSource
    {
        public CancellationToken RequestedCancellationToken { get; private set; }

        public Task<IEnumerable<BackgroundTenant>> GetTenantsAsync(CancellationToken cancellationToken = default)
        {
            RequestedCancellationToken = cancellationToken;
            return Task.FromResult<IEnumerable<BackgroundTenant>>(tenants);
        }
    }

    private sealed class NullTenantSource : IBackgroundTenantSource
    {
        public Task<IEnumerable<BackgroundTenant>> GetTenantsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<BackgroundTenant>>(null!);
        }
    }

    private sealed class CapturingBackgroundRequestContextWriter : IBackgroundRequestContextWriter
    {
        public List<BackgroundRequestContext> Contexts { get; } = [];

        public void SetContext(BackgroundRequestContext context) => Contexts.Add(context);
    }
}
