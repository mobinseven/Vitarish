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
/// Pulls products and categories from MedusaJS and upserts them in the local CommerceDbContext.
/// </summary>
public class CommerceCatalogSyncProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<CommerceCatalogSyncProcessor> logger)
{
    private const string BackgroundResource = "hangfire:commerce:catalog:sync";

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
            logger.LogInformation("Commerce: no store configured for tenant — skipping catalog sync.");
            return;
        }

        try
        {
            IReadOnlyList<MedusaProduct> products = await medusaClient.ListProductsAsync(cancellationToken: shutdownToken);
            int upserted = 0;

            foreach (MedusaProduct mp in products)
            {
                Vitarish.Commerce.Domain.Entities.Product? existing = await dbContext.Products
                    .FirstOrDefaultAsync(p => p.MedusaId == mp.Id, shutdownToken);

                if (existing is null)
                {
                    existing = new Vitarish.Commerce.Domain.Entities.Product
                    {
                        Id = Guid.NewGuid(),
                        MedusaId = mp.Id
                    };
                    _ = await dbContext.Products.AddAsync(existing, shutdownToken);
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

            _ = await dbContext.SaveChangesAsync(shutdownToken);
            logger.LogInformation("Commerce catalog sync: upserted {Count} products.", upserted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Commerce catalog sync failed for store {StoreId}.", store.Id);
        }
    }
}
