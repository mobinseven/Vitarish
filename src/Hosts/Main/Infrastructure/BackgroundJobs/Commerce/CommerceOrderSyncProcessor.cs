using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Commerce.Application.Abstractions;
using Vitarish.Commerce.Domain.Dto;
using Vitarish.Commerce.Domain.Enums;
using Vitarish.Commerce.Infrastructure.Clients;
using Vitarish.Commerce.Infrastructure.Clients.Models;
using Vitarish.Commerce.Infrastructure.Data;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Commerce;

/// <summary>
/// Pulls orders from MedusaJS and upserts them in the local CommerceDbContext.
/// </summary>
public class CommerceOrderSyncProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<CommerceOrderSyncProcessor> logger)
{
    private const string BackgroundResource = "hangfire:commerce:orders:sync";

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        IStoreService storeService = provider.GetRequiredService<IStoreService>();
        CommerceDbContext dbContext = provider.GetRequiredService<CommerceDbContext>();
        MedusaAdminClient medusaClient = provider.GetRequiredService<MedusaAdminClient>();

        StoreReadDto? store = await storeService.GetStoreByTenantAsync(shutdownToken);
        if (store is null)
        {
            logger.LogInformation("Commerce: no store configured for tenant — skipping order sync.");
            return;
        }

        try
        {
            IReadOnlyList<MedusaOrder> orders = await medusaClient.ListOrdersAsync(cancellationToken: shutdownToken);
            int upserted = 0;

            foreach (MedusaOrder mo in orders)
            {
                Vitarish.Commerce.Domain.Entities.Order? existing = await dbContext.Orders
                    .FirstOrDefaultAsync(o => o.MedusaId == mo.Id, shutdownToken);

                if (existing is null)
                {
                    existing = new Vitarish.Commerce.Domain.Entities.Order
                    {
                        Id = Guid.NewGuid(),
                        MedusaId = mo.Id
                    };
                    _ = await dbContext.Orders.AddAsync(existing, shutdownToken);
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
                    "requires_action" => FulfillmentStatus.RequiresAction,
                    _ => FulfillmentStatus.NotFulfilled
                };
                upserted++;
            }

            _ = await dbContext.SaveChangesAsync(shutdownToken);
            logger.LogInformation("Commerce order sync: upserted {Count} orders.", upserted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Commerce order sync failed for store {StoreId}.", store.Id);
        }
    }
}
