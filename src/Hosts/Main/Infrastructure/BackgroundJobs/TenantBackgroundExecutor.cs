using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Executes background job work for each tenant, ensuring the request context is populated per tenant scope.
/// </summary>
public sealed class TenantBackgroundExecutor(
    IPartiesService partiesService,
    IServiceScopeFactory serviceScopeFactory)
{
    private const string BackgroundAuthority = "system";

    /// <summary>
    /// Iterates all tenants and runs the provided action in a scoped context per tenant.
    /// </summary>
    /// <param name="resourceId">The resource identifier to place in the request context.</param>
    /// <param name="jobCancellationToken">The Hangfire job cancellation token.</param>
    /// <param name="perTenantAsync">The action to execute for each tenant.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task ExecutePerTenantAsync(
        string resourceId,
        IJobCancellationToken jobCancellationToken,
        Func<IServiceProvider, Tenant, IJobCancellationToken, CancellationToken, Task> perTenantAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobCancellationToken);
        ArgumentNullException.ThrowIfNull(perTenantAsync);

        jobCancellationToken.ThrowIfCancellationRequested();

        IEnumerable<Tenant> tenants = await partiesService.GetTenantsAsync(cancellationToken);

        foreach (Tenant tenant in tenants)
        {
            jobCancellationToken.ThrowIfCancellationRequested();

            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

            IRequestContextWriter contextWriter = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
            InitializeRequestContext(contextWriter, resourceId, tenant);

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                jobCancellationToken.ShutdownToken,
                cancellationToken);

            await perTenantAsync(scope.ServiceProvider, tenant, jobCancellationToken, linkedCts.Token);
        }
    }

    private static void InitializeRequestContext(IRequestContextWriter contextWriter, string resourceId, Tenant tenant)
    {
        contextWriter.SetContext(
            authority: BackgroundAuthority,
            clientId: tenant.ClientId,
            ownerId: tenant.OwnerId.ToString(),
            tenantId: tenant.Id.ToString(),
            resourceId: resourceId,
            requestId: $"hangfire-{resourceId}-{Guid.NewGuid()}");
    }
}
