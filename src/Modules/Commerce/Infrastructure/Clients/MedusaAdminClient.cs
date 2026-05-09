using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Vitarish.Commerce.Infrastructure.Clients.Models;

namespace Vitarish.Commerce.Infrastructure.Clients;

/// <summary>
/// Typed HTTP client wrapping MedusaJS Admin API endpoints.
/// </summary>
internal class MedusaAdminClient(HttpClient httpClient, ILogger<MedusaAdminClient> logger)
{
    private const int PageSize = 100;

    /// <summary>
    /// Fetches all products from the MedusaJS Admin API, handling pagination automatically.
    /// </summary>
    public async Task<IReadOnlyList<MedusaProduct>> ListAllProductsAsync(
        CancellationToken cancellationToken = default)
    {
        List<MedusaProduct> all = [];
        int offset = 0;

        while (true)
        {
            IReadOnlyList<MedusaProduct> page = await ListProductsAsync(offset, PageSize, cancellationToken);
            all.AddRange(page);
            if (page.Count < PageSize)
            {
                break;
            }
            offset += PageSize;
        }

        return all;
    }

    /// <summary>
    /// Fetches all orders from the MedusaJS Admin API, handling pagination automatically.
    /// </summary>
    public async Task<IReadOnlyList<MedusaOrder>> ListAllOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        List<MedusaOrder> all = [];
        int offset = 0;

        while (true)
        {
            IReadOnlyList<MedusaOrder> page = await ListOrdersAsync(offset, PageSize, cancellationToken);
            all.AddRange(page);
            if (page.Count < PageSize)
            {
                break;
            }
            offset += PageSize;
        }

        return all;
    }

    /// <summary>
    /// Lists products from the MedusaJS Admin API (single page).
    /// </summary>
    public async Task<IReadOnlyList<MedusaProduct>> ListProductsAsync(
        int offset = 0,
        int limit = PageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            MedusaListResponse<MedusaProduct>? response = await httpClient
                .GetFromJsonAsync<MedusaListResponse<MedusaProduct>>(
                    $"/admin/products?offset={offset}&limit={limit}",
                    cancellationToken);
            return response?.Products ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to list products from MedusaJS Admin API (offset={Offset}).", offset);
            return [];
        }
    }

    /// <summary>
    /// Lists orders from the MedusaJS Admin API (single page).
    /// </summary>
    public async Task<IReadOnlyList<MedusaOrder>> ListOrdersAsync(
        int offset = 0,
        int limit = PageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            MedusaListResponse<MedusaOrder>? response = await httpClient
                .GetFromJsonAsync<MedusaListResponse<MedusaOrder>>(
                    $"/admin/orders?offset={offset}&limit={limit}",
                    cancellationToken);
            return response?.Orders ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to list orders from MedusaJS Admin API (offset={Offset}).", offset);
            return [];
        }
    }

    /// <summary>
    /// Lists product categories from the MedusaJS Admin API.
    /// </summary>
    public async Task<IReadOnlyList<MedusaCategory>> ListCategoriesAsync(
        int offset = 0,
        int limit = PageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            MedusaListResponse<MedusaCategory>? response = await httpClient
                .GetFromJsonAsync<MedusaListResponse<MedusaCategory>>(
                    $"/admin/product-categories?offset={offset}&limit={limit}",
                    cancellationToken);
            return response?.ProductCategories ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to list categories from MedusaJS Admin API.");
            return [];
        }
    }
}
