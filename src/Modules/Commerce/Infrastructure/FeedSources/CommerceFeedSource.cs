using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Domain.Models;
using Vitarish.Commerce.Infrastructure.Data;

namespace Vitarish.Commerce.Infrastructure.FeedSources;

/// <summary>
/// Feed data source that surfaces recent orders and new products from the Commerce module.
/// </summary>
public class CommerceFeedSource(
    CommerceDbContext dbContext) : IFeedDataSource
{
    public string SourceId => "commerce";
    public string SourceName => "Commerce";
    public string Description => "Recent orders and new products from the Commerce module";
    public int Priority => 15;
    public bool EnabledByDefault => true;

    public async Task<IReadOnlyList<FeedItem>> GetFeedItemsAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<FeedItem> items = [];

            var recentOrders = await dbContext.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new { o.Id, o.MedusaId, o.CustomerId, o.Status, o.TotalAmount, o.CurrencyCode, o.CreatedAt })
                .ToListAsync(cancellationToken);

            items.AddRange(recentOrders.Select(o => new FeedItem
            {
                Id = o.Id,
                SourceId = SourceId,
                SourceName = SourceName,
                Title = $"Order #{o.MedusaId}",
                Description = $"{o.Status} — {o.TotalAmount} {o.CurrencyCode.ToUpperInvariant()}",
                Timestamp = o.CreatedAt,
                ItemType = "order",
                NavigationUrl = $"/shop/orders/{o.Id}",
                CreatedBy = null,
                Metadata = new Dictionary<string, object?>
                {
                    ["customerId"] = o.CustomerId,
                    ["status"] = o.Status.ToString(),
                    ["totalAmount"] = o.TotalAmount,
                    ["currencyCode"] = o.CurrencyCode
                }
            }));

            var newProducts = await dbContext.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new { p.Id, p.Title, p.Handle, p.CreatedAt })
                .ToListAsync(cancellationToken);

            items.AddRange(newProducts.Select(p => new FeedItem
            {
                Id = p.Id,
                SourceId = SourceId,
                SourceName = SourceName,
                Title = p.Title,
                Description = "New product published",
                Timestamp = p.CreatedAt,
                ItemType = "product",
                NavigationUrl = $"/shop/products/{p.Handle}",
                CreatedBy = null
            }));

            return [.. items.OrderByDescending(i => i.Timestamp).Take(options.Limit)];
        }
        catch
        {
            return [];
        }
    }
}
