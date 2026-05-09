using Microsoft.Extensions.Logging;
using Vitarish.Commerce.Infrastructure.Clients;
using Vitarish.Commerce.Infrastructure.Data;

namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal sealed class CommerceSyncService(
    IStoreService storeService,
    CommerceDbContext dbContext,
    MedusaAdminClient medusaClient,
    ILogger<CommerceSyncService> logger) : ICommerceSyncService
{
    public async Task SyncCatalogAsync(CancellationToken cancellationToken)
    {
        StoreReadDto? store = await storeService.GetStoreByTenantAsync(cancellationToken);
        if (store is null)
        {
            logger.LogInformation("Commerce: no store configured for tenant — skipping catalog sync.");
            return;
        }

        try
        {
            var products = await medusaClient.ListProductsAsync(cancellationToken: cancellationToken);
            int upserted = 0;

            foreach (var mp in products)
            {
                Product? existing = await dbContext.Products
                    .FirstOrDefaultAsync(p => p.MedusaId == mp.Id, cancellationToken);

                if (existing is null)
                {
                    existing = new Product { Id = Guid.NewGuid(), MedusaId = mp.Id };
                    _ = await dbContext.Products.AddAsync(existing, cancellationToken);
                }

                existing.Handle = mp.Handle;
                existing.Title = mp.Title;
                existing.Description = mp.Description ?? string.Empty;
                existing.Status = mp.Status switch
                {
                    "published" => ProductStatus.Published,
                    "proposed" => ProductStatus.Proposed,
                    "rejected" => ProductStatus.Rejected,
                    _ => ProductStatus.Draft
                };
                upserted++;
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Commerce catalog sync: upserted {Count} products.", upserted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Commerce catalog sync failed for store {StoreId}.", store.Id);
        }
    }

    public async Task SyncOrdersAsync(CancellationToken cancellationToken)
    {
        StoreReadDto? store = await storeService.GetStoreByTenantAsync(cancellationToken);
        if (store is null)
        {
            logger.LogInformation("Commerce: no store configured for tenant — skipping order sync.");
            return;
        }

        try
        {
            var orders = await medusaClient.ListOrdersAsync(cancellationToken: cancellationToken);
            int upserted = 0;

            foreach (var mo in orders)
            {
                Order? existing = await dbContext.Orders
                    .FirstOrDefaultAsync(o => o.MedusaId == mo.Id, cancellationToken);

                if (existing is null)
                {
                    existing = new Order { Id = Guid.NewGuid(), MedusaId = mo.Id };
                    _ = await dbContext.Orders.AddAsync(existing, cancellationToken);
                }

                existing.CustomerId = mo.CustomerId ?? string.Empty;
                existing.TotalAmount = mo.Total / 100m;
                existing.CurrencyCode = mo.CurrencyCode;
                existing.Status = mo.Status switch
                {
                    "confirmed" => OrderStatus.Confirmed,
                    "processing" => OrderStatus.Processing,
                    "shipped" => OrderStatus.Shipped,
                    "delivered" => OrderStatus.Delivered,
                    "canceled" => OrderStatus.Canceled,
                    "refunded" => OrderStatus.Refunded,
                    _ => OrderStatus.Pending
                };
                existing.FulfillmentStatus = mo.FulfillmentStatus switch
                {
                    "partially_fulfilled" => FulfillmentStatus.PartiallyFulfilled,
                    "fulfilled" => FulfillmentStatus.Fulfilled,
                    "partially_shipped" => FulfillmentStatus.PartiallyShipped,
                    "shipped" => FulfillmentStatus.Shipped,
                    "partially_returned" => FulfillmentStatus.PartiallyReturned,
                    "returned" => FulfillmentStatus.Returned,
                    "canceled" => FulfillmentStatus.Canceled,
                    "requires_action" => FulfillmentStatus.Requires_Action,
                    _ => FulfillmentStatus.NotFulfilled
                };
                upserted++;
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Commerce order sync: upserted {Count} orders.", upserted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Commerce order sync failed for store {StoreId}.", store.Id);
        }
    }
}
