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
            // Paginate: fetch all products across all pages
            var products = await medusaClient.ListAllProductsAsync(cancellationToken);

            // Batch-load all existing products matching incoming MedusaIds (avoids N+1)
            List<string> incomingIds = [.. products.Select(p => p.Id)];
            Dictionary<string, Product> existing = await dbContext.Products
                .Where(p => incomingIds.Contains(p.MedusaId))
                .ToDictionaryAsync(p => p.MedusaId, cancellationToken);

            int upserted = 0;
            foreach (var mp in products)
            {
                if (!existing.TryGetValue(mp.Id, out Product? product))
                {
                    product = new Product { Id = Guid.NewGuid(), MedusaId = mp.Id };
                    _ = await dbContext.Products.AddAsync(product, cancellationToken);
                }

                product.Handle = mp.Handle;
                product.Title = mp.Title;
                product.Description = mp.Description ?? string.Empty;
                product.Status = mp.Status switch
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
            // Paginate: fetch all orders across all pages
            var orders = await medusaClient.ListAllOrdersAsync(cancellationToken);

            // Batch-load all existing orders matching incoming MedusaIds (avoids N+1)
            List<string> incomingIds = [.. orders.Select(o => o.Id)];
            Dictionary<string, Order> existing = await dbContext.Orders
                .Where(o => incomingIds.Contains(o.MedusaId))
                .ToDictionaryAsync(o => o.MedusaId, cancellationToken);

            int upserted = 0;
            foreach (var mo in orders)
            {
                if (!existing.TryGetValue(mo.Id, out Order? order))
                {
                    order = new Order { Id = Guid.NewGuid(), MedusaId = mo.Id };
                    _ = await dbContext.Orders.AddAsync(order, cancellationToken);
                }

                order.CustomerId = mo.CustomerId ?? string.Empty;
                order.TotalAmount = mo.Total / 100m;
                order.CurrencyCode = mo.CurrencyCode;
                order.Status = mo.Status switch
                {
                    "confirmed" => OrderStatus.Confirmed,
                    "processing" => OrderStatus.Processing,
                    "shipped" => OrderStatus.Shipped,
                    "delivered" => OrderStatus.Delivered,
                    "canceled" => OrderStatus.Canceled,
                    "refunded" => OrderStatus.Refunded,
                    _ => OrderStatus.Pending
                };
                order.FulfillmentStatus = mo.FulfillmentStatus switch
                {
                    "partially_fulfilled" => FulfillmentStatus.PartiallyFulfilled,
                    "fulfilled" => FulfillmentStatus.Fulfilled,
                    "partially_shipped" => FulfillmentStatus.PartiallyShipped,
                    "shipped" => FulfillmentStatus.Shipped,
                    "partially_returned" => FulfillmentStatus.PartiallyReturned,
                    "returned" => FulfillmentStatus.Returned,
                    "canceled" => FulfillmentStatus.Canceled,
                    "requires_action" => FulfillmentStatus.RequiresAction,
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
