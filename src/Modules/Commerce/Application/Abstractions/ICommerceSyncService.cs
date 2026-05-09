namespace Vitarish.Commerce.Application.Abstractions;

public interface ICommerceSyncService
{
    Task SyncCatalogAsync(CancellationToken cancellationToken);
    Task SyncOrdersAsync(CancellationToken cancellationToken);
}
