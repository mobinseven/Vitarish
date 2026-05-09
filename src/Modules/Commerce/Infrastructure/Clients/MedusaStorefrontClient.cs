using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Vitarish.Commerce.Infrastructure.Clients.Models;

namespace Vitarish.Commerce.Infrastructure.Clients;

/// <summary>
/// Typed HTTP client wrapping MedusaJS Storefront API endpoints.
/// </summary>
internal class MedusaStorefrontClient(HttpClient httpClient, ILogger<MedusaStorefrontClient> logger)
{
    /// <summary>
    /// Creates a new cart in MedusaJS.
    /// </summary>
    public async Task<MedusaCart?> CreateCartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/store/carts", new { }, cancellationToken);
            MedusaCartResponse? result = await response.Content.ReadFromJsonAsync<MedusaCartResponse>(cancellationToken: cancellationToken);
            return result?.Cart;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create cart in MedusaJS Storefront API.");
            return null;
        }
    }

    /// <summary>
    /// Adds a line item to a MedusaJS cart.
    /// </summary>
    public async Task<MedusaCart?> AddLineItemAsync(
        string cartId,
        string variantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                $"/store/carts/{cartId}/line-items",
                new { variant_id = variantId, quantity },
                cancellationToken);
            MedusaCartResponse? result = await response.Content.ReadFromJsonAsync<MedusaCartResponse>(cancellationToken: cancellationToken);
            return result?.Cart;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to add line item to cart {CartId} in MedusaJS Storefront API.", cartId);
            return null;
        }
    }

    /// <summary>
    /// Initiates checkout for a MedusaJS cart, returning the payment session URL.
    /// </summary>
    public async Task<string?> InitiateCheckoutAsync(string cartId, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                $"/store/carts/{cartId}/payment-sessions",
                new { },
                cancellationToken);
            MedusaCartResponse? result = await response.Content.ReadFromJsonAsync<MedusaCartResponse>(cancellationToken: cancellationToken);
            return result?.Cart?.Id;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initiate checkout for cart {CartId}.", cartId);
            return null;
        }
    }
}
