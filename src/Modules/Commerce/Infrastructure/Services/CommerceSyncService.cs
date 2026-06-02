using Microsoft.Extensions.Logging;
using Vitarish.Commerce.Infrastructure.Clients;
using Vitarish.Commerce.Infrastructure.Clients.Models;

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
            IReadOnlyList<MedusaProduct> products = await medusaClient.ListAllProductsAsync(cancellationToken);

            // Batch-load all existing products matching incoming MedusaIds (avoids N+1)
            List<string> incomingIds = [.. products.Select(p => p.Id)];
            Dictionary<string, Product> existing = await dbContext.Products
                .Where(p => incomingIds.Contains(p.MedusaId))
                .ToDictionaryAsync(p => p.MedusaId, cancellationToken);

            int upserted = 0;
            foreach (MedusaProduct mp in products)
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
            IReadOnlyList<MedusaOrder> orders = await medusaClient.ListAllOrdersAsync(cancellationToken);

            // Batch-load all existing orders matching incoming MedusaIds (avoids N+1)
            List<string> incomingIds = [.. orders.Select(o => o.Id)];
            Dictionary<string, Order> existing = await dbContext.Orders
                .Where(o => incomingIds.Contains(o.MedusaId))
                .ToDictionaryAsync(o => o.MedusaId, cancellationToken);

            int nextOrderNumber = await dbContext.Orders
                .Where(o => o.OrderNumber > 0)
                .Select(o => (int?)o.OrderNumber)
                .MaxAsync(cancellationToken) ?? 0;

            int upserted = 0;
            foreach (MedusaOrder mo in orders)
            {
                bool isNew = false;
                if (!existing.TryGetValue(mo.Id, out Order? order))
                {
                    order = new Order { Id = Guid.NewGuid(), MedusaId = mo.Id };
                    _ = await dbContext.Orders.AddAsync(order, cancellationToken);
                    isNew = true;
                }

                order.CustomerId = mo.CustomerId ?? string.Empty;
                order.TotalAmount = mo.Total / 100m;
                order.CurrencyCode = mo.CurrencyCode;
                if (isNew || order.OrderNumber <= 0)
                {
                    nextOrderNumber++;
                    order.OrderNumber = nextOrderNumber;
                }
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
                order.RefundStatus = mo.Status switch
                {
                    "refunded" => RefundStatus.Refunded,
                    _ when order.RefundStatus == RefundStatus.Refunded => RefundStatus.Refunded,
                    _ => order.RefundStatus
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

            // Deterministic backfill for any historical rows that still do not have an order number (batched).
            const int BackfillBatchSize = 500;
            while (true)
            {
                List<Order> missingOrderNumbers = await dbContext.Orders
                    .Where(o => o.OrderNumber <= 0)
                    .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.Id)
                    .Take(BackfillBatchSize)
                    .ToListAsync(cancellationToken);

                if (missingOrderNumbers.Count == 0)
                {
                    break;
                }

                foreach (Order order in missingOrderNumbers)
                {
                    nextOrderNumber++;
                    order.OrderNumber = nextOrderNumber;
                }
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
