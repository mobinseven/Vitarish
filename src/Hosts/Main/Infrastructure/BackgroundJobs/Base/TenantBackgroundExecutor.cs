using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Base;

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
    /// <param name="cancellationToken">The Hangfire job cancellation token.</param>
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

        IEnumerable<TenantReadDto> tenants = await partiesService.GetTenantsAsync(cancellationToken);

        foreach (TenantReadDto tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

                IRequestContextWriter contextWriter = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
                InitializeRequestContext(contextWriter, resourceId, tenant);

                await perTenantAsync(scope.ServiceProvider, cancellationToken);
            }
            catch
            {
            }
        }
    }

    private static void InitializeRequestContext(IRequestContextWriter contextWriter, string resourceId, TenantReadDto tenant) => contextWriter.SetContext(
            authority: BackgroundAuthority,
            clientId: tenant.ClientId,
            ownerId: tenant.OwnerId.ToString(),
            tenantId: tenant.Id.ToString(),
            resourceId: resourceId,
            requestId: Guid.NewGuid().ToString());
}
