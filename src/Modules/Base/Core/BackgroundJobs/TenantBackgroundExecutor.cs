using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base.BackgroundJobs;

/// <summary>
/// Executes background job work for each tenant, ensuring the request context is populated per tenant scope.
/// </summary>
public sealed class TenantBackgroundExecutor(
    IBackgroundTenantSource tenantSource,
    IServiceScopeFactory serviceScopeFactory)
{
    private const string BackgroundAuthority = "system";

    /// <summary>
    /// Iterates all tenants and runs the provided action in a scoped context per tenant.
    /// </summary>
    /// <param name="resourceId">The resource identifier to place in the request context.</param>
    /// <param name="perTenantAsync">The action to execute for each tenant.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task ExecutePerTenantAsync(
        string resourceId,
        Func<IServiceProvider, CancellationToken, Task> perTenantAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(perTenantAsync);

        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<BackgroundTenant> tenants = await tenantSource.GetTenantsAsync(cancellationToken)
            ?? Array.Empty<BackgroundTenant>();

        foreach (BackgroundTenant tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

            IBackgroundRequestContextWriter contextWriter = scope.ServiceProvider.GetRequiredService<IBackgroundRequestContextWriter>();
            InitializeRequestContext(contextWriter, resourceId, tenant);

            await perTenantAsync(scope.ServiceProvider, cancellationToken);

        }
    }

    private static void InitializeRequestContext(IBackgroundRequestContextWriter contextWriter, string resourceId, BackgroundTenant tenant) => contextWriter.SetContext(
        new BackgroundRequestContext(
            Authority: BackgroundAuthority,
            ClientId: tenant.AppId,
            OwnerId: tenant.OwnerId.ToString(),
            TenantId: tenant.Id.ToString(),
            ResourceId: resourceId,
            RequestId: Guid.NewGuid().ToString()));
}
